# MTFH Reporting Data Listener

An AWS Lambda that receives tenure events through SQS, retrieves the current tenure from the Tenure API, and publishes an Avro record to Kafka. This repository deploys a service; it has no NuGet publishing workflow or package release version.

## Message processing and data sources

Terraform subscribes the listener queue to the tenure SNS topic with raw message delivery. Each SQS record body is deserialized as an `EntityEventSns`. `TenureCreatedEvent` and `TenureUpdatedEvent` are handled; other event types are logged and ignored.

For each supported event, the listener retrieves the tenure, combines it with event metadata, retrieves the latest value schema, builds an Avro record, ensures the Kafka topic exists, and publishes the record. Processing exceptions propagate to Lambda for SQS retry/dead letter handling. Records in a batch are processed sequentially.

The service exposes no HTTP API endpoints. Its Lambda entry point is:

```text
MtfhReportingDataListener::MtfhReportingDataListener.SqsFunction::FunctionHandler
```

External interfaces:

| System | Interface | Purpose |
| --- | --- | --- |
| SNS / SQS | Tenure event messages | Trigger processing |
| Tenure API | `GET {ApiRoute}/tenures/{id}` | Retrieve current tenure, using the configured API URL and token |
| Schema Registry | `GET http://{KAFKA_SCHEMA_REGISTRY_HOSTNAME}/subjects/{TENURE_SCHEMA_NAME}-value/versions/latest` | Retrieve the Avro schema |
| Kafka | Topic named by `TENURE_SCHEMA_NAME` | Publish serialized tenure events |
| AWS Systems Manager Parameter Store | References in `serverless.yml` and Terraform | Supply deployment configuration, queue and topic ARNs |

The listener does not directly query a database. Kafka topic creation uses one partition and a replication factor of two. The producer flush timeout is ten seconds.

## Setup

Install Docker with Docker Compose v2. For builds outside Docker, install the .NET 10 SDK. Private Hackney dependencies require a GitHub Packages credential with access to the `LBHackney-IT` feed.

From the repository root:

```sh
cp .env.example .env
```

Replace the placeholder in `.env` with the `LBHPACKAGESTOKEN` credential. Keep this file private. For native .NET commands, export `LBHPACKAGESTOKEN` in the shell as well; `NuGet.Config` reads it from the environment.

```sh
dotnet restore MtfhReportingDataListener.sln
dotnet build MtfhReportingDataListener.sln --no-restore
```

## Configuration

`BaseFunction` loads `appsettings.json`, optionally `appsettings.{ENVIRONMENT}.json`, then environment variables.

| Variable | Purpose |
| --- | --- |
| `ENVIRONMENT` | Select optional settings; `LocalDevelopment` also selects plaintext Kafka connections, otherwise SSL is used |
| `TenureApiUrl` | Tenure API base URL |
| `TenureApiToken` | Tenure API credential |
| `DATAPLATFORM_KAFKA_HOSTNAME` | Comma-separated Kafka bootstrap servers |
| `TENURE_SCHEMA_NAME` | Kafka topic and schema subject prefix |
| `KAFKA_SCHEMA_REGISTRY_HOSTNAME` | Schema Registry host and port; schema retrieval prepends `http://` |
| `AWS_REGION` / AWS credentials | AWS SDK configuration; deployed Lambda uses its execution role |

Deployment values are mapped from SSM in [serverless.yml](MtfhReportingDataListener/serverless.yml). Use reachable API, Kafka and schema services when invoking the handler locally.

## Tests

```sh
make test
```

This builds the test container, starts ZooKeeper, two Kafka brokers and Schema Registry, and runs the existing xUnit unit, gateway and end-to-end tests. Compose supplies local AWS placeholder credentials and Kafka/schema addresses. Coverage is collected inside the test container under `/app/coverage` in OpenCover format. The test Dockerfile does not install or run SonarScanner; CircleCI runs the SonarCloud scan separately.

The application project is a Lambda class library. The existing `make serve` command does not provide an HTTP server or a Lambda invocation host; port 3000 is not a service endpoint. Use the automated tests or a Lambda invocation tool to exercise the handler.

## Packaging and deployment

From `MtfhReportingDataListener`, `build.sh` installs Amazon.Lambda.Tools and packages a Release build under `bin/release/net10.0/mtfh-reporting-data-listener.zip`. On Windows, `build.cmd` packages the same artifact and requires Amazon.Lambda.Tools to be installed already.

The Serverless configuration selects the managed `dotnet10` runtime in `eu-west-2`. CircleCI builds and tests changes, manages Terraform, and deploys development from `master`; the `release` workflow deploys staging and uses approval steps before production. Deployment requires the configured AWS credentials, SSM parameters, VPC resources and execution permissions. The existing Serverless v3 runtime schema may warn about `dotnet10`; deployment validation is still required before release.

## Project layout

- `MtfhReportingDataListener`: Lambda handler, configuration, gateways and tenure processing.
- `MtfhReportingDataListener.Tests`: xUnit tests, mocks and integration fixtures.
- `terraform/{development,staging,production}`: queue, SNS subscription and related infrastructure.
- `.circleci/config.yml`: build, test and deployment workflows.

Maintained by the LBHackney-IT housing products team.

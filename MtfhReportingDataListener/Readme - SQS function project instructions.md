# SQS Lambda packaging and invocation

The deployed handler is `MtfhReportingDataListener::MtfhReportingDataListener.SqsFunction::FunctionHandler`. It accepts an AWS SQS event; each record body contains the JSON tenure event directly, rather than an SNS envelope. Terraform configures raw SNS delivery to the SQS queue, and `serverless.yml` configures the queue as the Lambda event source.

From the repository root, run `make test` to exercise the existing automated tests with their local Kafka and Schema Registry dependencies. See the [repository README](../README.md) for credentials and configuration.

For packaging, install the .NET 10 SDK and provide `LBHPACKAGESTOKEN` in the environment, then run:

```sh
cd MtfhReportingDataListener
./build.sh
```

The script installs Amazon.Lambda.Tools and produces `bin/release/net10.0/mtfh-reporting-data-listener.zip`. On Debian it also installs `zip`, requiring appropriate system permissions. `build.cmd` is the Windows equivalent and assumes the Lambda tool is already installed.

`aws-lambda-tools-defaults.json` selects `net10.0` and the `dotnet10` runtime. Normal deployment uses the CircleCI Serverless workflow and the SSM/VPC configuration in `serverless.yml`. An AWS console test invocation must supply an SQS event and configuration permitting access to the Tenure API, Schema Registry and Kafka. The class library cannot be started as an HTTP application with `dotnet run`.

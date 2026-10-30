# Reporting listener implementation

This .NET 10 AWS Lambda handles tenure events from SQS and sends current tenure data to Kafka. See the [repository README](../README.md) for setup, configuration, external interfaces, tests and deployment.

`SqsFunction.FunctionHandler` processes each SQS record sequentially. `BaseFunction` initializes configuration, dependency injection, Lambda logging and AWS X-Ray instrumentation. `UseCaseFactory` routes `TenureCreatedEvent` and `TenureUpdatedEvent` to `TenureUseCase`; unsupported events are ignored.

`TenureInfoApiGateway` retrieves the current tenure. `SchemaRegistry` retrieves the latest topic value schema. `TenureUseCase` converts tenure data and event metadata to an Avro record, and `KafkaGateway` creates the topic if needed and publishes it. Exceptions propagate to Lambda for retry handling. There is no direct database access or HTTP server in this project.

The `Boundary` directory defines incoming event models, `Domain` defines outgoing event data, and `Infrastructure` contains JSON configuration and exception types. Package versions and business behavior are preserved by the .NET 10 migration.

# MTFH Reporting Data Listener

This listener implements an AWS Lambda Function that recieves messages when any updates are made to the TenureInformationApi and sends the updates to a Kafka queue. 

Here is the process on how the data is saved to Kafka:

### Retrieving data from the API
1. When one of the events in [this switch statement][usecase-factory] is raised then this listener is triggered
2. The listener can then call the relevant API to retrieve the data the needs to be saved. You could potentially use [this Shared NuGet Package][nuget] to make calls to any APIs.[This ReadME][Api-Gateway] explains how the listener calls the relative API.
     - If the Tenure doesn't exist then the listener throws an Exception 

### Get Schema from AWS Glue
3. The listener will then get the schema from AWS Glue Schema registry managed in [this repository][ADD LINK HERE]

### Convert data to Avro
4. Then using this schema, an AVRO generic record is created holding the tenure details retrieved from the tenure API.

   Kafka only accepts the following data types; `byte[], Bytebuffer, Double, Integer, Long, String`. If you need to send through other data types you will first need to serialize the data into one of these types. Below are some code examples of how we have done this.

   **Nullable types (Union)**:

   Here we check whether the field value is null, assign null if it is. This needs to be done as Kafka doesn't accept empty strings.
   If not, remove the nullable part from the schema and continue as normal.

   ```csharp
        if (fieldValue == null)
        {
            record.Add(field.Name, null);
            return;
        }

        fieldSchema = GetNonNullablePartOfNullableSchema(field.Schema);
        fieldType = fieldSchema.Tag;

        ...

        private Schema GetNonNullablePartOfNullableSchema(Schema nullableSchema)
        {
            var jsonSchema = (JsonElement) JsonSerializer.Deserialize<object>(nullableSchema.ToString());
            jsonSchema.TryGetProperty("type", out var unionList);
            var notNullSchema = unionList.EnumerateArray().First(type => type.ToString() != "null").ToString();
            return Schema.Parse(notNullSchema);
        }
   ```

   **Enums**:

   ```csharp 
        new GenericEnum((EnumSchema) fieldSchema, fieldValue.ToString())
   ```

   **DateTime Objects**:

   Converts to a int holding a Unix timestamp.

    ```csharp
        private int? UnixTimestampNullable(object obj)
        {
            var date = (DateTime?) obj;
            return (int?) (date?.Subtract(new DateTime(1970, 1, 1)))?.TotalSeconds;
        }
    ```

    **Arrays**:

    ```csharp
        var fieldValueAsList = (List<ExampleList>) fieldValue;
        var itemsSchema = GetSchemaForArrayItems(fieldSchema);
        var recordsList = fieldValueAsList.Select(listItem => PopulateFields(listItem, itemsSchema)).ToArray()
        record.Add(field.Name, recordsList);
        
        private Schema GetSchemaForArrayItems(Schema arraySchema)
        {
            var jsonSchema = (JsonElement) JsonSerializer.Deserialize<object>(arraySchema.ToString());
            jsonSchema.TryGetProperty("items", out var itemsSchemaJson);
            return Schema.Parse(itemsSchemaJson.ToString());
        }
    ``` 

    Once the data types have been converted you can create the Generic Record by adding each value to the record, this can be done through the use of a for loop & reflection:

    ```csharp

        public GenericRecord PopulateFields(object item, Schema schema)
        {
            var record = new GenericRecord((RecordSchema) schema);
            ((RecordSchema) schema).Fields.ForEach(field =>
            {
                PropertyInfo propInfo = item.GetType().GetProperty(field.Name);
                if (propInfo == null)
                {
                    Console.WriteLine($"Field name: {field.Name} not found in {item}");
                    return;
                }

                var fieldValue = propInfo.GetValue(item);
                var fieldSchema = field.Schema;
                var fieldType = field.Schema.Tag;

                //Add any data type conversion required here

                record.Add(field.Name, fieldValue);
            });

            return record;
        }

    ```

5. Lastly the record created above is then sent through to Kafka.
   ### Schema Registry
   In order to send the data through to Kafka a SchemaRegistryClient is required. SchemaRegistery are seperate from your Kafka brokers. The Kafka Producers publish the data to Kafka topics and communicates with the Schema Registry to send and receive schemas that describe the data models for the messages simultaneously. Hence the SchemaRegistry is used to serialize the message and then save the serialize message to Kafka. 

   ### Send Data to Kafka
   Messages are sent to Kafka using Kafka Topics which the consumer subsequently reads from. A Kafka Topic is created for the producer from its schema name if one doesn't already exist, otherwise it uses the schema name provided as an environment variable.
   This is set in Parameter Store in the producer's AWS account.
   We then use the `Producer.Flush(TimeSpan.FromSeconds(10))` to immediately send through the message through Kafka to the consumer.
   There is a 10 seconds maximum timeout on the listener. This would mean that it will wait until either all the messages have been sent or 10 seconds have passed. If a maximum timeout is not set then it would wait until all the messages have been sent, and in the event there is an issue whilst sending messages, it will likely hang until the lambda timeout which we don't want.

   ### Kafka Images
   In order to ensure high availability and improve fault tolerance of the event streaming process, we deploy multiple brokers and set a replication factor for Kafka topics to a value that is greater than 1.
   This means that copies (replicas) of the data will be spread across the brokers so that in the event one of the brokers goes down/ fails, then another broker can take over and serve the request.

See [this diagram][diagram] for a visual representation of the process of the listener.


## Stack

- .NET Core as a web framework.
- xUnit as a test framework.

## Contributing

### Setup

1. Install [Docker][docker-download].
2. Install [AWS CLI][AWS-CLI].
3. Clone this repository.
4. Rename the initial template.
5. Open it in your IDE.



### Development

To serve the application, run it using your IDE of choice, we use Visual Studio CE and JetBrains Rider on Mac.

**Note**
When running locally the appropriate database conneciton details are still needed.



The application can also be served locally using docker:
1.  Add you security credentials to AWS CLI.
```sh
$ aws configure
```
2. Log into AWS ECR.
```sh
$ aws ecr get-login --no-include-email
```
3. Build and serve the application. It will be available in the port 3000.
```sh
$ make build && make serve
```

### Release process

We use a pull request workflow, where changes are made on a branch and approved by one or more other maintainers before the developer can merge into `master` branch.

![Circle CI Workflow Example](docs/circle_ci_workflow.png)

Then we have an automated six step deployment process, which runs in CircleCI.

1. Automated tests (xUnit) are run to ensure the release is of good quality.
2. The application is deployed to development automatically, where we check our latest changes work well.
3. We manually confirm a staging deployment in the CircleCI workflow once we're happy with our changes in development.
4. The application is deployed to staging.
5. We manually confirm a production deployment in the CircleCI workflow once we're happy with our changes in staging.
6. The application is deployed to production.

Our staging and production environments are hosted by AWS. We would deploy to production per each feature/config merged into  `release`  branch.

### Creating A PR

To help with making changes to code easier to understand when being reviewed, we've added a PR template.
When a new PR is created on a repo that uses this API template, the PR template will automatically fill in the `Open a pull request` description textbox.
The PR author can edit and change the PR description using the template as a guide.

## Static Code Analysis

### Using [FxCop Analysers](https://www.nuget.org/packages/Microsoft.CodeAnalysis.FxCopAnalyzers)

FxCop runs code analysis when the Solution is built.

Both the API and Test projects have been set up to **treat all warnings from the code analysis as errors** and therefore, fail the build.

However, we can select which errors to suppress by setting the severity of the responsible rule to none, e.g `dotnet_analyzer_diagnostic.<Category-or-RuleId>.severity = none`, within the `.editorconfig` file.
Documentation on how to do this can be found [here](https://docs.microsoft.com/en-us/visualstudio/code-quality/use-roslyn-analyzers?view=vs-2019).

## Testing

### Run the tests

```sh
$ make test
```

### Agreed Testing Approach
- Use xUnit, FluentAssertions and Moq
- Always follow a TDD approach
- Tests should be independent of each other
- Gateway tests should interact with a real test instance of the database
- Test coverage should never go down. (See the [test project readme](MtfhReportingDataListener.Tests/readme.md#Run-coverage) for how to run a coverage check.)
- All use cases should be covered by E2E tests
- Optimise when test run speed starts to hinder development
- Unit tests and E2E tests should run in CI
- Test database schemas should match up with production database schema
- Have integration tests which test from the DynamoDb database to API Gateway

## Data Migrations
### A good data migration
- Record failure logs
- Automated
- Reliable
- As close to real time as possible
- Observable monitoring in place
- Should not affect any existing databases

## Contacts

### Active Maintainers

- **Selwyn Preston**, Lead Developer at London Borough of Hackney (selwyn.preston@hackney.gov.uk)
- **Mirela Georgieva**, Lead Developer at London Borough of Hackney (mirela.georgieva@hackney.gov.uk)
- **Matt Keyworth**, Lead Developer at London Borough of Hackney (matthew.keyworth@hackney.gov.uk)

### Other Contacts

- **Rashmi Shetty**, Product Owner at London Borough of Hackney (rashmi.shetty@hackney.gov.uk)

[docker-download]: https://www.docker.com/products/docker-desktop
[AWS-CLI]: https://aws.amazon.com/cli/
[Api-Gateway]: https://github.com/LBHackney-IT/lbh-core/blob/release/Hackney.Core/Hackney.Core.Http/README.md#ApiGateway
[diagram]: https://drive.google.com/file/d/1KbF9gcmf0LOvr7w2fE_fxTd1lcccPilr/view?usp=sharing
[tenure-api-github]: https://github.com/LBHackney-IT/tenure-api
[usecase-factory]: /MtfhReportingDataListener/Factories/UseCaseFactory.cs#L16
[nuget]: https://github.com/LBHackney-IT/lbh-core/blob/release/Hackney.Core/Hackney.Core.Http/ApiGateway.cs
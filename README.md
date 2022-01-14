# MTFH Reporting Data Listener

The aim of this listener is to implement an AWS Function that would recieve messages of any updates made to the TenureInformationApi and then save the relative data to Kafka. 

Here is the process on how the data is saved to Kafka:
1. When the TenureUpdateEvent is raised this listener is triggered
2. The Listener calls the TenureInformationApi using a Shared Nuget Package to ensure that the Tenure sent through in the message exists. [Here][Api-Gateway] is a ReadME the explains how the listner calls the TenureInformationApi.
3. If the Tenure doesn't exist then the Listener throws an Exception 
4. If the Tenure exists then the Listener gets the schema that has been generated in AWS Glue using [this repository][ADD LINK HERE]
5. Then the details from the Schema are used the convert the schema into an avro generic record.
   Kafka only accepts the following data type; `byte[], Bytebuffer, Double, Integer, Long, String`, if you would like to send through other data type you will first need to convert the data type that is acceptable. Some of the common data types are below with examples of how you can convert it:

   Nullable type (Union):
   ```csharp
        private Schema GetNonNullablePartOfNullableSchema(Schema nullableSchema)
        {
            var jsonSchema = (JsonElement) JsonSerializer.Deserialize<object>(nullableSchema.ToString());
            jsonSchema.TryGetProperty("type", out var unionList);
            var notNullSchema = unionList.EnumerateArray().First(type => type.ToString() != "null").ToString();
            return Schema.Parse(notNullSchema);
        }
   ```

   Enum:

   ```csharp 
        new GenericEnum((EnumSchema) fieldSchema, fieldValue.ToString())
   ```

   DateTime:

    ```csharp
        private int? UnixTimestampNullable(object obj)
        {
            var date = (DateTime?) obj;
            return (int?) (date?.Subtract(new DateTime(1970, 1, 1)))?.TotalSeconds;
        }
    ```

    Array:

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

    Once the data types have been converted you can create the Generic Record by adding each value to the record, this can be done through the use of a For Loop:

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

6. Lastly the record created above is then sent through to Kafka.
   In order to send the data through to Kafka a SchemaRegistryClient is required. SchemaRegistery are seperate from your Kafka brokers.The Kafka Producers publish the data to Kafka topics and communicates with the Schema Registry to send and recieve schemas that describe the data models for the messages simultaneously. Hence the SchemaRegistry is used to serialize the message and then save the serialize message to Kafka. 
   We then use the Producer.Flush() to immediately send through the message to Kafka. 

[Here][ADD LINK HERE] is a diagram to give a visual representation of the process of the listener.


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

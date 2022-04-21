using MtfhReportingDataListener.Tests.Helper;
using AutoFixture;
using System;
using System.Threading.Tasks;
using System.Net.Http;

public class MockContactDetailSchemaRegistry
{
    public string SchemaDefinition { get; }
    public string Topic { get; }


    public MockContactDetailSchemaRegistry()
    {
        var fixture = new Fixture();
        SchemaDefinition = SmallContactDetailSchema();
        Topic = fixture.Create<string>();
        SetTopicEnvVariable();
    }

    public async Task GivenThereIsAMatchingSchemaInTheRegistry(HttpClient httpClient)
    {
        var schemaRegistryUrl = Environment.GetEnvironmentVariable("KAFKA_SCHEMA_REGISTRY_HOSTNAME");
        await SchemaRegistry.SaveSchemaForTopic(httpClient, schemaRegistryUrl, SchemaDefinition, Topic);
    }

    private void SetTopicEnvVariable()
    {
        Environment.SetEnvironmentVariable("CONTACT_DETAIL_SCHEMA_NAME", Topic);
    }
    private string SmallContactDetailSchema()
    {
        return @"{
            ""type"": ""record"",
            ""name"": ""ContactDetailAPIAddedEvent"",
            ""namespace"": ""MMH"",
            ""fields"": [
                {
                    ""name"": ""Id"",
                    ""type"": ""string"",
                    ""logicalType"": ""uuid""
                },
                {
                    ""name"": ""EventType"",
                    ""type"": ""string""
                },
                {
                    ""name"": ""SourceDomain"",
                    ""type"": ""string""
                },
                {
                    ""name"": ""SourceSystem"",
                    ""type"": ""string""
                },
                {
                    ""name"": ""Version"",
                    ""type"": ""string""
                },
                {
                    ""name"": ""CorrelationId"",
                    ""type"": ""string"",
                    ""logicalType"": ""uuid""
                },
                {
                    ""name"": ""DateTime"",
                    ""type"": ""int"",
                    ""logicalType"": ""date""
                },
                {
                    ""name"": ""User"",
                    ""type"": {
                        ""type"": ""record"",
                        ""name"": ""User"",
                        ""fields"": [
                            {
                                ""name"": ""Name"",
                                ""type"": ""string""
                            },
                            {
                                ""name"": ""Email"",
                                ""type"": ""string""
                            }
                        ]
                    }
                },
                {
                    ""name"": ""ContactDetails"",
                    ""type"": {
                        ""type"": ""record"",
                        ""namespace"": ""MMH"",
                        ""name"": ""ContactDetail"",
                        ""fields"": [
                            {
                                ""name"": ""Id"",
                                ""type"": ""string"",
                                ""logicalType"": ""uuid""
                            },
                            {
                                ""name"": ""TargetId"",
                                ""type"": ""string""
                                ""logicalType"": ""uuid""
                            },
                            {
                                ""name"": ""RecordValidUntil"",
                                ""type"": [""int"", ""null""],
                                ""logicalType"": ""date""
                            },
                            {
                                ""name"": ""SourceServiceArea"",
                                ""type"": {
                                    ""type"": ""record"",
                                    ""name"": ""SourceServiceArea"",
                                    ""fields"": [
                                        {
                                            ""name"": ""Area"",
                                            ""type"": ""string""
                                        },
                                        {
                                            ""name"": ""IsDefault"",
                                            ""type"": ""boolean""
                                        }
                                    ]
                                }
                            },
                            {
                                ""name"": ""IsActive"",
                                ""type"": ""boolean""
                            },
                            {
                                ""name"": ""ContactInformation"",
                                ""type"": {
                                    ""type"": ""record"",
                                    ""name"": ""ContactInformation"",
                                    ""fields"": [
                                        {
                                            ""name"": ""Value"",
                                            ""type"": ""string""
                                        },
                                        {
                                            ""name"": ""ContactType"",
                                            ""type"": ""enum"",
                                            ""symbols"": [
                                                ""Phone"",
                                                ""Email"",
                                                ""Address""
                                           ]
                                        },
                                        {
                                            ""name"": ""Description"",
                                            ""type"": ""string""
                                        },
                                        {
                                            ""name"": ""SubType"",
                                            ""type"": [{
                                                ""name"": ""ContactInformationSubType"",
                                                ""type"": ""enum"",
                                                ""symbols"": [
                                                    ""correspondenceAddress,
                                                    ""mobile"",
                                                    ""home"",
                                                    ""work"",
                                                    ""other"",
                                                    ""landline""
                                               ]
                                            }, ""null""]
                                        },
                                    ]
                                }
                            },
                        ]
                    }
                }
            ]
        }";
    }
}


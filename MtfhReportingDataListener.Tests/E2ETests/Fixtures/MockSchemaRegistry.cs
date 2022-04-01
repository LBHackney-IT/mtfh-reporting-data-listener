using MtfhReportingDataListener.Tests.Helper;
using AutoFixture;
using System;
using System.Threading.Tasks;
using System.Net.Http;

public class MockSchemaRegistry
{
    public string SchemaDefinition { get; }
    public string Topic { get; }


    public MockSchemaRegistry()
    {
        var fixture = new Fixture();
        SchemaDefinition = SmallTenureSchema();
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
        Environment.SetEnvironmentVariable("TENURE_SCHEMA_NAME", Topic);
    }
    private string SmallTenureSchema()
    {
        return @"{
            ""type"": ""record"",
            ""name"": ""TenureAPIChangeEvent"",
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
                    ""name"": ""Tenure"",
                    ""type"": {
                        ""type"": ""record"",
                        ""namespace"": ""MMH"",
                        ""name"": ""TenureInformation"",
                        ""fields"": [
                            {
                                ""name"": ""Id"",
                                ""type"": ""string"",
                                ""logicalType"": ""uuid""
                            },
                            {
                                ""name"": ""PaymentReference"",
                                ""type"": ""string""
                            },
                            {
                                ""name"": ""SuccessionDate"",
                                ""type"": [""int"", ""null""],
                                ""logicalType"": ""date""
                            },
                            {
                                ""name"": ""HouseholdMembers"",
                                ""type"": {
                                    ""type"": ""array"",
                                    ""items"": {
                                        ""name"": ""HouseholdMember"",
                                        ""type"": ""record"",
                                        ""fields"": [
                                            {
                                                ""name"": ""Id"",
                                                ""type"": ""string"",
                                                ""logicalType"": ""uuid""
                                            },
                                            {
                                                ""name"": ""Type"",
                                                ""type"": {
                                                    ""name"": ""HouseholdMembersType"",
                                                    ""type"": ""enum"",
                                                    ""symbols"": [
                                                        ""Person"",
                                                        ""Organisation""
                                                    ]
                                                }
                                            },
                                            {
                                                ""name"": ""FullName"",
                                                ""type"": ""string""
                                            },
                                            {
                                                ""name"": ""IsResponsible"",
                                                ""type"": ""boolean""
                                            },
                                            {
                                                ""name"": ""DateOfBirth"",
                                                ""type"": ""int"",
                                                ""logicalType"": ""date""
                                            },
                                            {
                                                ""name"": ""PersonTenureType"",
                                                ""type"": {
                                                    ""name"": ""PersonTenureType"",
                                                    ""type"": ""enum"",
                                                    ""symbols"": [
                                                        ""Tenant"",
                                                        ""Leaseholder"",
                                                        ""Freeholder"",
                                                        ""HouseholdMember"",
                                                        ""Occupant""
                                                    ]
                                                }
                                            }
                                        ]
                                    }
                                }
                            },
                            {
                                ""name"": ""TenuredAsset"",
                                ""type"": {
                                    ""type"": ""record"",
                                    ""name"": ""TenuredAsset"",
                                    ""fields"": [
                                        {
                                            ""name"": ""Id"",
                                            ""type"": ""string"",
                                            ""logicalType"": ""uuid""
                                        },
                                        {
                                            ""name"": ""Type"",
                                            ""type"": [{
                                                ""name"": ""TenuredAssetType"",
                                                ""type"": ""enum"",
                                                ""symbols"": [
                                                    ""Block"",
                                                    ""Concierge"",
                                                    ""Dwelling"",
                                                    ""LettableNonDwelling"",
                                                    ""MediumRiseBlock"",
                                                    ""NA"",
                                                    ""TravellerSite""
                                                ]
                                            }, ""null""]
                                        },
                                        {
                                            ""name"": ""FullAddress"",
                                            ""type"": ""string""
                                        },
                                        {
                                            ""name"": ""Uprn"",
                                            ""type"": ""string""
                                        },
                                        {
                                            ""name"": ""PropertyReference"",
                                            ""type"": ""string""
                                        }
                                    ]
                                }
                            }
                        ]
                    }
                }
            ]
        }";
    }
}

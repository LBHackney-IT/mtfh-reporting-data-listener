using MtfhReportingDataListener.Tests.Helper;
using System.Threading;
using Amazon.Glue;
using Amazon.Glue.Model;
using Moq;
using AutoFixture;
using System;

public class MockSchemaRegistry
{
    public string RegistryName { get; }
    public string SchemaArn { get; }
    public string SchemaName { get; }
    public string SchemaDefinition { get; }

    private Mock<IAmazonGlue> _mockGlue { get; set; }

    public MockSchemaRegistry(Mock<IAmazonGlue> mockGlue)
    {
        var fixture = new Fixture();
        RegistryName = fixture.Create<string>();
        SchemaName = fixture.Create<string>();
        SchemaArn = "arn:aws:glue:my-schema-registry";
        SchemaDefinition = SmallTenureSchema();
        _mockGlue = mockGlue;
        SetSchemaEnvVariables();
    }

    public void GivenThereIsAMatchingSchemaInGlueRegistry()
    {

        var getSchemaRequest = new GetSchemaRequest()
        {
            SchemaId = new SchemaId()
            {

                RegistryName = RegistryName,
                SchemaArn = SchemaArn,
                SchemaName = SchemaName
            }
        };

        var getSchemaVersion = new GetSchemaVersionRequest()
        {
            SchemaId = getSchemaRequest.SchemaId,
            SchemaVersionNumber = new SchemaVersionNumber()
            {
                LatestVersion = true,
                VersionNumber = 2
            }
        };
        var getSchemaResponse = new GetSchemaResponse()
        {
            LatestSchemaVersion = getSchemaVersion.SchemaVersionNumber.VersionNumber,
            RegistryName = RegistryName,
            SchemaArn = SchemaArn,
            SchemaName = SchemaName
        };

        _mockGlue.Setup(x => x.GetSchemaAsync(It.Is<GetSchemaRequest>(x => MockGlueHelperMethods.CheckRequestsEquivalent(getSchemaRequest, x)), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(getSchemaResponse);

        _mockGlue.Setup(x => x.GetSchemaVersionAsync(
                It.Is<GetSchemaVersionRequest>(x => MockGlueHelperMethods.CheckVersionRequestsEquivalent(getSchemaVersion, x)),
                It.IsAny<CancellationToken>()
            )).ReturnsAsync(new GetSchemaVersionResponse { SchemaDefinition = SchemaDefinition });
    }

    private void SetSchemaEnvVariables()
    {
        Environment.SetEnvironmentVariable("SCHEMA_ARN", SchemaArn);
        Environment.SetEnvironmentVariable("REGISTRY_NAME", RegistryName);
        Environment.SetEnvironmentVariable("SCHEMA_NAME", SchemaName);
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
            }
        }";
    }
}

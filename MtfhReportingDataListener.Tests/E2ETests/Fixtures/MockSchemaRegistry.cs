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

    private Mock<IAmazonGlue> _mockGlue {get; set;}

    public MockSchemaRegistry(Mock<IAmazonGlue> mockGlue)
    {
        var fixture = new Fixture();
        RegistryName = fixture.Create<string>();
        SchemaName = fixture.Create<string>();
        SchemaArn = "arn:aws:glue:my-schema-registry";
        SchemaDefinition = SmallTenureSchema();
        _mockGlue = mockGlue;
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
                                        ""Organization""
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
            }
        ]}";
    }
}

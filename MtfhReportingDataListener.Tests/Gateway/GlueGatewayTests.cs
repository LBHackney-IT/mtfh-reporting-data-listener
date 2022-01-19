using Amazon.Glue;
using Amazon.Glue.Model;
using Moq;
using MtfhReportingDataListener.Gateway;
using MtfhReportingDataListener.Gateway.Interfaces;
using MtfhReportingDataListener.Factories;
using Xunit;
using System.Threading.Tasks;
using System.Threading;
using AutoFixture;
using FluentAssertions;
using MtfhReportingDataListener.Tests.Helper;

namespace MtfhReportingDataListener.Tests.Gateway
{
    [Collection("LogCall collection")]
    public class GlueGatewayTests
    {
        public readonly GlueGateway _gateway;
        public readonly Mock<IAmazonGlue> _mockAmazonGlue;
        public readonly Fixture _fixture = new Fixture();

        public GlueGatewayTests()
        {
            _mockAmazonGlue = new Mock<IAmazonGlue>();
            var mockGlueWrapper = new Mock<IGlueFactory>();
            mockGlueWrapper.Setup(x => x.GlueClient()).ReturnsAsync(_mockAmazonGlue.Object);
            _gateway = new GlueGateway(mockGlueWrapper.Object);
        }

        [Theory]
        [InlineData("goodnight moon", 4)]
        [InlineData("hello world", 7)]
        public async Task VerifyGettingTheSchemaStringForTheLatestVersion(string schema, int versionId)
        {
            var expectedRequest = new GetSchemaVersionRequest
            {
                SchemaId = new SchemaId()
                {
                    SchemaArn = "arn:aws:glue:mmh",
                },
                SchemaVersionNumber = new SchemaVersionNumber
                {
                    LatestVersion = true,
                }
            };

            var response = new GetSchemaVersionResponse
            {
                SchemaDefinition = schema,
                VersionNumber = versionId
            };

            _mockAmazonGlue.Setup(x =>
                x.GetSchemaVersionAsync(
                    It.Is<GetSchemaVersionRequest>(x => MockGlueHelperMethods.CheckVersionRequestsEquivalent(expectedRequest, x)),
                    It.IsAny<CancellationToken>()
                )
            ).ReturnsAsync(response);

            var schemaDetails = await _gateway.GetSchema("arn:aws:glue:mmh").ConfigureAwait(false);

            var expectedSchemaResponse = new SchemaResponse
            {
                Schema = schema,
                VersionId = versionId,
            };

            schemaDetails.Should().BeEquivalentTo(expectedSchemaResponse);
        }
    }
}

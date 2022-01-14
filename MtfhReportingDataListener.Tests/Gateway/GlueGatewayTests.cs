using Amazon.Glue;
using Amazon.Glue.Model;
using Moq;
using MtfhReportingDataListener.Gateway;
using MtfhReportingDataListener.Gateway.Interfaces;
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
            _gateway = new GlueGateway(_mockAmazonGlue.Object);
        }


        [Fact]
        public async Task VerifyTheSchemaDetailsAreRetrievedFromGlue()
        {
            var getSchemaRequest = new GetSchemaRequest()
            {
                SchemaId = new SchemaId()
                {

                    RegistryName = "TenureSchema",
                    SchemaArn = "arn:aws:glue:mmh",
                    SchemaName = "MMH"
                }
            };
            var getSchemaResponse = _fixture.Create<GetSchemaResponse>();
            _mockAmazonGlue.Setup(x => x.GetSchemaAsync(It.Is<GetSchemaRequest>(x => MockGlueHelperMethods.CheckRequestsEquivalent(getSchemaRequest, x)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(getSchemaResponse).Verifiable();
            _mockAmazonGlue.Setup(x => x.GetSchemaVersionAsync(
                    It.IsAny<GetSchemaVersionRequest>(),
                    It.IsAny<CancellationToken>()
                )).ReturnsAsync(new GetSchemaVersionResponse { SchemaDefinition = "schema" });

            await _gateway.GetSchema("TenureSchema", "arn:aws:glue:mmh", "MMH").ConfigureAwait(false);
            _mockAmazonGlue.Verify();
        }

        [Theory]
        [InlineData("goodnight moon", 4)]
        [InlineData("hello world", 7)]
        public async Task VerifyGettingTheSchemaStringForTheLatestVersion(string schema, int versionId)
        {
            var getSchemaResponse = new GetSchemaResponse()
            {
                LatestSchemaVersion = versionId,
                RegistryName = "TenureSchema",
                SchemaArn = "arn:aws:glue:mmh",
                SchemaName = "MMH"
            };

            var expectedRequest = new GetSchemaVersionRequest
            {
                SchemaId = new SchemaId()
                {
                    RegistryName = "TenureSchema",
                    SchemaArn = "arn:aws:glue:mmh",
                    SchemaName = "MMH"
                },
                SchemaVersionNumber = new SchemaVersionNumber
                {
                    LatestVersion = true,
                    VersionNumber = versionId
                }
            };

            _mockAmazonGlue.Setup(x => x.GetSchemaAsync(It.IsAny<GetSchemaRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(getSchemaResponse);

            var response = new GetSchemaVersionResponse { SchemaDefinition = schema };

            _mockAmazonGlue.Setup(x =>
                x.GetSchemaVersionAsync(
                    It.Is<GetSchemaVersionRequest>(x => MockGlueHelperMethods.CheckVersionRequestsEquivalent(expectedRequest, x)),
                    It.IsAny<CancellationToken>()
                )
            ).ReturnsAsync(response);

            var schemaDetails = await _gateway.GetSchema("TenureSchema", "arn:aws:glue:mmh", "MMH").ConfigureAwait(false);

            var expectedSchemaResponse = new SchemaResponse
            {
                Schema = schema,
                VersionId = versionId,
            };

            schemaDetails.Should().BeEquivalentTo(expectedSchemaResponse);
        }
    }
}

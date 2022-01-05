using Amazon.Glue;
using Amazon.Glue.Model;
using Moq;
using MtfhReportingDataListener.Gateway;
using Xunit;
using System.Threading.Tasks;
using System.Threading;
using AutoFixture;

namespace MtfhReportingDataListener.Tests.Gateway
{
    [Collection("LogCall collection")]
    public class GlueGatewayTests
    {
        public readonly GlueGateway _gateway;
        public readonly Mock<AmazonGlueClient> _mockAmazonGlue;
        public readonly Fixture _fixture = new Fixture(); 

        public GlueGatewayTests()
        {
            _mockAmazonGlue = new Mock<AmazonGlueClient>();
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
            _mockAmazonGlue.Setup(x => x.GetSchemaAsync(It.Is<GetSchemaRequest>(x => CheckRequestsEquivalent(getSchemaRequest, x)), It.IsAny<CancellationToken>())).ReturnsAsync(getSchemaResponse).Verifiable();
            await _gateway.GetSchema("TenureSchema", "arn:aws:glue:mmh", "MMH").ConfigureAwait(false);
            _mockAmazonGlue.Verify();
        }
        [Fact]
        public async Task VerifyGettingTheSchemaStringForTheLatestVersion()
        {
            var getSchemaResponse = new GetSchemaResponse()
            {
                LatestSchemaVersion = 5,
                RegistryName = "TenureSchema",
                SchemaArn = "arn:aws:glue:mmh",
                SchemaName = "MMH"
            };

            _mockAmazonGlue.Setup(x => x.GetSchemaAsync(It.IsAny<GetSchemaRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(getSchemaResponse);

            var response = new GetSchemaVersionResponse { SchemaDefinition = "schema" };

            _mockAmazonGlue.Setup(x =>
                x.GetSchemaVersionAsync(
                    It.IsAny<GetSchemaVersionRequest>(),
                    It.IsAny<CancellationToken>()
                )
            ).ReturnsAsync(response);

            var schema = await _gateway.GetSchema("TenureSchema", "arn:aws:glue:mmh", "MMH").ConfigureAwait(false);

            var expectedSchemaResponse = new SchemaResponse
            {
                Schema = "",
                VersionId = 1,
                SchemaId = 1,
                Subject = "Tenure"
            };


        }

        private static bool CheckRequestsEquivalent(GetSchemaRequest expectedRequest, GetSchemaRequest receivedRequest)
        {
            return receivedRequest.SchemaId.RegistryName == expectedRequest.SchemaId.RegistryName
                && receivedRequest.SchemaId.SchemaArn == expectedRequest.SchemaId.SchemaArn
                && receivedRequest.SchemaId.SchemaName == expectedRequest.SchemaId.SchemaName;
        }
        private static bool CheckVersionRequestsEquivalent(GetSchemaVersionRequest expectedRequest, GetSchemaVersionRequest receivedRequest)
        {
            return receivedRequest.SchemaId.RegistryName == expectedRequest.SchemaId.RegistryName
                && receivedRequest.SchemaId.SchemaArn == expectedRequest.SchemaId.SchemaArn
                && receivedRequest.SchemaId.SchemaName == expectedRequest.SchemaId.SchemaName
                && receivedRequest.SchemaVersionNumber.LatestVersion == expectedRequest.SchemaVersionNumber.LatestVersion
                && receivedRequest.SchemaVersionNumber.VersionNumber == expectedRequest.SchemaVersionNumber.VersionNumber;
        }
    }
}

using Amazon.Glue;
using Amazon.Glue.Model;
using Moq;
using MtfhReportingDataListener.Gateway;
using Xunit;
using System.Threading.Tasks;
using System.Threading;

namespace MtfhReportingDataListener.Tests.Gateway
{
    [Collection("LogCall collection")]
    public class GlueGatewayTests
    {
        public readonly GlueGateway _gateway;
        public readonly Mock<AmazonGlueClient> _mockAmazonGlue;

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
            await _gateway.GetSchema("TenureSchema", "arn:aws:glue:mmh", "MMH").ConfigureAwait(false);
            _mockAmazonGlue.Verify(x => x.GetSchemaAsync(It.Is<GetSchemaRequest>(x => CheckRequestsEquivalent(getSchemaRequest, x)), It.IsAny<CancellationToken>()));
        }

        private bool CheckRequestsEquivalent(GetSchemaRequest expectedRequest, GetSchemaRequest receivedRequest)
        {
            return receivedRequest.SchemaId.RegistryName == expectedRequest.SchemaId.RegistryName
                && receivedRequest.SchemaId.SchemaArn == expectedRequest.SchemaId.SchemaArn
                && receivedRequest.SchemaId.SchemaName == expectedRequest.SchemaId.SchemaName;
        }
    }
}

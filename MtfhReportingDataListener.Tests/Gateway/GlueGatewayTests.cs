using Amazon.Glue;
using Amazon.Glue.Model;
using Moq;
using MtfhReportingDataListener.Gateway;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

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
        public void VerifyTheGetSchemaFromGlueIsRetrieved()
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
            _mockAmazonGlue.Verify(x => x.GetSchemaAsync(getSchemaRequest, default));
        }


    }
}

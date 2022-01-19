using System.Threading.Tasks;
using Amazon.Glue;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using System;

namespace MtfhReportingDataListener.Factories
{
    public class GlueFactory : IGlueFactory
    {
        public async Task<IAmazonGlue> GlueClient()
        {
            var stsClient = new AmazonSecurityTokenServiceClient();
            var assumeRoleReq = new AssumeRoleRequest()
            {
                DurationSeconds = 3600,
                RoleArn = Environment.GetEnvironmentVariable("ROLE_ARN_TO_ACCESS_DATAPLATFORM_GLUE_REGISTRY"),
                RoleSessionName = "mtfh-tenure-api-events-streaming",
            };

            var response = await stsClient.AssumeRoleAsync(assumeRoleReq);

            var credentials = response.Credentials;
            return new AmazonGlueClient(credentials);
        }
    }

    public interface IGlueFactory
    {
        Task<IAmazonGlue> GlueClient();
    }
}

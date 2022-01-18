using Amazon.Glue;
using MtfhReportingDataListener.Gateway.Interfaces;
using System.Threading.Tasks;
using Amazon.Glue.Model;

namespace MtfhReportingDataListener.Gateway
{
    public class GlueGateway : IGlueGateway
    {
        public IAmazonGlue _amazonGlueClient;
        public GlueGateway(IAmazonGlue amazonGlueClient)
        {
            _amazonGlueClient = amazonGlueClient;
        }

        public async Task<SchemaResponse> GetSchema(string schemaArn)
        {
            var latestVersionNumber = await GetLastestSchemaVersionNumber(schemaArn).ConfigureAwait(false);
            var SchemaDefinition = await GetSchemaDefinition(schemaArn, latestVersionNumber).ConfigureAwait(false);

            return new SchemaResponse
            {
                Schema = SchemaDefinition,
                VersionId = latestVersionNumber
            };
        }

        private async Task<string> GetSchemaDefinition(string schemaArn, long versionNumber)
        {
            var schemaVersionRequest = new GetSchemaVersionRequest()
            {
                SchemaId = new SchemaId()
                {
                    SchemaArn = schemaArn,
                },
                SchemaVersionNumber = new SchemaVersionNumber()
                {
                    LatestVersion = true,
                    VersionNumber = versionNumber
                }
            };

            var schemaVersionResponse = await _amazonGlueClient.GetSchemaVersionAsync(schemaVersionRequest).ConfigureAwait(false);
            return schemaVersionResponse.SchemaDefinition;
        }



        private async Task<long> GetLastestSchemaVersionNumber(string schemaArn)
        {
            var schemaRequest = new GetSchemaRequest()
            {
                SchemaId = new SchemaId()
                {
                    SchemaArn = schemaArn,
                }
            };

            var getSchemaResponse = await _amazonGlueClient.GetSchemaAsync(schemaRequest).ConfigureAwait(false);
            return getSchemaResponse.LatestSchemaVersion;
        }
    }
}

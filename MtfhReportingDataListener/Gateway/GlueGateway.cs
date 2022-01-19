using Amazon.Glue;
using MtfhReportingDataListener.Gateway.Interfaces;
using MtfhReportingDataListener.Factories;
using System.Threading.Tasks;
using Amazon.Glue.Model;

namespace MtfhReportingDataListener.Gateway
{
    public class GlueGateway : IGlueGateway
    {
        public IGlueFactory _amazonGlue;
        public GlueGateway(IGlueFactory amazonGlue)
        {
            _amazonGlue = amazonGlue;
        }

        public async Task<SchemaResponse> GetSchema(string schemaArn)
        {
            var glueClient = await _amazonGlue.GlueClient().ConfigureAwait(false);
            var latestVersionNumber = await GetLastestSchemaVersionNumber(glueClient, schemaArn).ConfigureAwait(false);
            var SchemaDefinition = await GetSchemaDefinition(glueClient, schemaArn, latestVersionNumber).ConfigureAwait(false);

            return new SchemaResponse
            {
                Schema = SchemaDefinition,
                VersionId = latestVersionNumber
            };
        }

        private async Task<string> GetSchemaDefinition(IAmazonGlue glueClient, string schemaArn, long versionNumber)
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

            var schemaVersionResponse = await glueClient.GetSchemaVersionAsync(schemaVersionRequest).ConfigureAwait(false);
            return schemaVersionResponse.SchemaDefinition;
        }



        private async Task<long> GetLastestSchemaVersionNumber(IAmazonGlue glueClient, string schemaArn)
        {
            var schemaRequest = new GetSchemaRequest()
            {
                SchemaId = new SchemaId()
                {
                    SchemaArn = schemaArn,
                }
            };

            var getSchemaResponse = await glueClient.GetSchemaAsync(schemaRequest).ConfigureAwait(false);
            return getSchemaResponse.LatestSchemaVersion;
        }
    }
}

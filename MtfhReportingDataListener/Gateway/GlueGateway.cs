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

        public async Task<SchemaResponse> GetSchema(string registryName, string schemaArn, string schemaName)
        {
            var latestVersionNumber = await GetLastestSchemaVersionNumber(registryName, schemaArn, schemaName).ConfigureAwait(false);
            var SchemaDefinition = await GetSchemaDefinition(registryName, schemaArn, schemaName, latestVersionNumber).ConfigureAwait(false);

            return new SchemaResponse
            {
                Schema = SchemaDefinition,
                VersionId = latestVersionNumber
            };
        }

        private async Task<string> GetSchemaDefinition(string registryName, string schemaArn, string schemaName, long versionNumber)
        {
            var schemaVersionRequest = new GetSchemaVersionRequest()
            {
                SchemaId = new SchemaId()
                {

                    RegistryName = registryName,
                    SchemaArn = schemaArn,
                    SchemaName = schemaName

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



        private async Task<long> GetLastestSchemaVersionNumber(string registryName, string schemaArn, string schemaName)
        {
            var schemaRequest = new GetSchemaRequest()
            {
                SchemaId = new SchemaId()
                {

                    RegistryName = registryName,
                    SchemaArn = schemaArn,
                    SchemaName = schemaName

                }
            };

            var getSchemaResponse = await _amazonGlueClient.GetSchemaAsync(schemaRequest).ConfigureAwait(false);
            return getSchemaResponse.LatestSchemaVersion;
        }
    }
}

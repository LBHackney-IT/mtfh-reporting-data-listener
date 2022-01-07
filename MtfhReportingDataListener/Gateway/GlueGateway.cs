using Amazon.Glue;
using MtfhReportingDataListener.Gateway.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
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
            var schemaRequest = new GetSchemaRequest()
            {
                SchemaId = new SchemaId()
                {

                    RegistryName = registryName,
                    SchemaArn = schemaArn,
                    SchemaName = schemaName

                }
            };

            var getSchema = await _amazonGlueClient.GetSchemaAsync(schemaRequest).ConfigureAwait(false);

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
                    VersionNumber = getSchema.LatestSchemaVersion
                }
            };

            var schemaVersionResponse = await _amazonGlueClient.GetSchemaVersionAsync(schemaVersionRequest).ConfigureAwait(false);
            return new SchemaResponse
            {
                Schema = schemaVersionResponse.SchemaDefinition,
                VersionId = getSchema.LatestSchemaVersion
            };
        }
    }
}

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
        public AmazonGlueClient _amazonGlueClient;
        public GlueGateway(AmazonGlueClient amazonGlueClient)
        {
            _amazonGlueClient = amazonGlueClient;
        }

        public async Task<string> GetSchema(string registryName, string schemaArn, string schemaName)
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

            var schemaVersion = new GetSchemaVersionRequest()
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

            await _amazonGlueClient.GetSchemaVersionAsync(schemaVersion).ConfigureAwait(false);
            return "";
        }
    }
}

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
            var schemaVersionRequest = new GetSchemaVersionRequest()
            {
                SchemaId = new SchemaId()
                {
                    SchemaArn = schemaArn,
                },
                SchemaVersionNumber = new SchemaVersionNumber()
                {
                    LatestVersion = true
                }
            };
            var schemaVersionResponse = await glueClient.GetSchemaVersionAsync(schemaVersionRequest).ConfigureAwait(false);

            return new SchemaResponse
            {
                Schema = schemaVersionResponse.SchemaDefinition,
                VersionId = schemaVersionResponse.VersionNumber
            };
        }
    }
}

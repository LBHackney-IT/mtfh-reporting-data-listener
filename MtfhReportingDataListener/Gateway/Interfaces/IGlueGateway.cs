using System.Threading.Tasks;

namespace MtfhReportingDataListener.Gateway.Interfaces
{
    public interface IGlueGateway
    {
        Task<SchemaResponse> GetSchema(string registryName, string schemaArn, string schemaName);
    }

    public class SchemaResponse
    {
        public string Schema { get; set; }
        public long VersionId { get; set; }
    }
}

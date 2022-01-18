using System.Threading.Tasks;

namespace MtfhReportingDataListener.Gateway.Interfaces
{
    public interface IGlueGateway
    {
        Task<SchemaResponse> GetSchema(string schemaArn);
    }

    public class SchemaResponse
    {
        public string Schema { get; set; }
        public long VersionId { get; set; }
    }
}

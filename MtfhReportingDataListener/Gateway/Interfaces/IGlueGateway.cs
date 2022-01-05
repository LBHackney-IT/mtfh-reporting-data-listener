using System.Threading.Tasks;

namespace MtfhReportingDataListener.Gateway.Interfaces
{
    public interface IGlueGateway
    {
        Task<string> GetSchema(string registryName, string schemaArn, string schemaName);
    }
}

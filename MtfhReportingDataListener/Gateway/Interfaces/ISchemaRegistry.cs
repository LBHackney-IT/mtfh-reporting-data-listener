using System.Threading.Tasks;

namespace MtfhReportingDataListener.Gateway.Interfaces
{
    public interface ISchemaRegistry
    {
        Task<string> GetSchemaForTopic(string topic);
    }
}

using Hackney.Shared.Tenure.Boundary.Response;

namespace MtfhReportingDataListener.Gateway.Interfaces
{
    public interface IKafkaGateway
    {
        IsSuccessful SendDataToKafka(TenureResponseObject message, string topic);
        string GetSchema();
    }
}

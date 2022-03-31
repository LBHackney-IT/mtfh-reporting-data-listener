using System.Threading.Tasks;
using Avro.Generic;

namespace MtfhReportingDataListener.Gateway.Interfaces
{
    public interface IKafkaGateway
    {
        IsSuccessful SendDataToKafka(string topic, GenericRecord record);
        Task CreateKafkaTopic(string tenureApiTopic);
    }

}

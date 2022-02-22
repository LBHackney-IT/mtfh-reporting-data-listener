using System.Threading.Tasks;
using Avro.Generic;
using Confluent.SchemaRegistry;

namespace MtfhReportingDataListener.Gateway.Interfaces
{
    public interface IKafkaGateway
    {
        IsSuccessful SendDataToKafka(string topic, GenericRecord record, Schema schema);
        Task CreateKafkaTopic(string tenureApiTopic);
    }

}

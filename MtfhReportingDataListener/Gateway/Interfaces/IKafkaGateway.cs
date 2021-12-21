using Avro.Generic;
using Confluent.Kafka;
using Hackney.Shared.Tenure.Boundary.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace MtfhReportingDataListener.Gateway.Interfaces
{
    public interface IKafkaGateway
    {
        IsSuccessful SendDataToKafka(TenureResponseObject message, string topic, string schemaRegistryUrl);
        string GetSchema();
    }
}

using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Text;

namespace MtfhReportingDataListener.Gateway.Interfaces
{
    public interface IKafkaGateway
    {
        IsSuccessful SendDataToKafka(string message, string topic);
    }
}

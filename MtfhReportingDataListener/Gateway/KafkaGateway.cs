using Confluent.Kafka;
using MtfhReportingDataListener.Gateway.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace MtfhReportingDataListener.Gateway
{
    public class IsSuccessful
    {
        public bool Success { get; set; }
    }
    public class KafkaGateway : IKafkaGateway
    {
        public KafkaGateway() { }
        public IsSuccessful SendDataToKafka(string message, string topic)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092",
                ClientId = "mtfh-reporting-data-listener"
            };


            DeliveryReport<string, string> deliveryReport = null;

            using (var producer = new ProducerBuilder<string, string>(config).Build())
            {
                producer.Produce(topic,
                                new Message<string, string>
                                {
                                    Key = Guid.NewGuid().ToString(),
                                    Value = message
                                },
                (report) =>
                {
                    deliveryReport = report;
                    if (deliveryReport.Error.Code != ErrorCode.NoError)
                    {
                        throw new Exception(deliveryReport.Error.Reason);
                    }
                    else
                    {
                        Console.WriteLine($"Produced message to: {deliveryReport.TopicPartitionOffset}");
                    }
                });


                producer.Flush(TimeSpan.FromSeconds(10));
            }
            return new IsSuccessful
            {
                Success = deliveryReport.Error.Code == ErrorCode.NoError
            };
        }
    }
}

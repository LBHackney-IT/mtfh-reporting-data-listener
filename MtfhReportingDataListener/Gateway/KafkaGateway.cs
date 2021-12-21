using Avro.Generic;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Hackney.Shared.Tenure.Boundary.Response;
using MtfhReportingDataListener.Gateway.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using TenureSchema;

namespace MtfhReportingDataListener.Gateway
{
    public class IsSuccessful
    {
        public bool Success { get; set; }
    }
    public class KafkaGateway : IKafkaGateway
    {
        public KafkaGateway() { }
        public IsSuccessful SendDataToKafka(TenureResponseObject message, string topic, string schemaRegistryUrl)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = Environment.GetEnvironmentVariable("DATAPLATFORM_KAFKA_HOSTNAME"),
                ClientId = "mtfh-reporting-data-listener",
            };


            DeliveryReport<string, TenureSchema.TenureInformation> deliveryReport = null;

            using (var schemaRegistry = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = schemaRegistryUrl }))
            using (var producer = new ProducerBuilder<string, TenureSchema.TenureInformation>(config)
                .SetValueSerializer(new AvroSerializer<TenureSchema.TenureInformation>(schemaRegistry).AsSyncOverAsync()).Build())
            {
                producer.Produce(topic,
                                new Message<string, TenureSchema.TenureInformation>
                                {
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
                Success = deliveryReport?.Error?.Code == ErrorCode.NoError
            };
        }

        public string GetSchema()
        {
            return "";
        }
    }
}

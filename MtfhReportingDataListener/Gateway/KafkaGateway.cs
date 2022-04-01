using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry.Serdes;
using MtfhReportingDataListener.Gateway.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avro.Generic;
using Confluent.Kafka.Admin;
using Confluent.SchemaRegistry;

namespace MtfhReportingDataListener.Gateway
{
    public class IsSuccessful
    {
        public bool Success { get; set; }
    }

    public class KafkaGateway : IKafkaGateway
    {
        public IsSuccessful SendDataToKafka(string topic, GenericRecord message)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = Environment.GetEnvironmentVariable("DATAPLATFORM_KAFKA_HOSTNAME"),
                ClientId = "mtfh-reporting-data-listener",
                SecurityProtocol = Environment.GetEnvironmentVariable("ENVIRONMENT") == "LocalDevelopment" ? SecurityProtocol.Plaintext : SecurityProtocol.Ssl
            };

            var schemaRegistryConfig = new SchemaRegistryConfig
            {
                Url = Environment.GetEnvironmentVariable("KAFKA_SCHEMA_REGISTRY_HOSTNAME")
            };

            DeliveryReport<string, GenericRecord> deliveryReport = null;
            Action<DeliveryReport<string, GenericRecord>> handleProduceErrors = (report) =>
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
            };

            using (var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig))
            using (var producer = new ProducerBuilder<string, GenericRecord>(config)
                .SetValueSerializer(new AvroSerializer<GenericRecord>(schemaRegistry).AsSyncOverAsync())
                .Build()
            )
            {
                producer.Produce(topic, new Message<string, GenericRecord> { Value = message }, handleProduceErrors);
                producer.Flush(TimeSpan.FromSeconds(10));
            }

            return new IsSuccessful
            {
                Success = deliveryReport?.Error?.Code == ErrorCode.NoError
            };
        }


        public async Task CreateKafkaTopic(string topicName)
        {
            using (var adminClient = new AdminClientBuilder(new AdminClientConfig()
            {
                BootstrapServers = Environment.GetEnvironmentVariable("DATAPLATFORM_KAFKA_HOSTNAME"),
                SecurityProtocol = Environment.GetEnvironmentVariable("ENVIRONMENT") == "LocalDevelopment" ? SecurityProtocol.Plaintext : SecurityProtocol.Ssl
            }).Build())
            {
                var topicsList = adminClient.GetMetadata(TimeSpan.FromSeconds(5)).Topics;
                bool topicExists = topicsList.Any(t => t.Topic == topicName);

                if (!topicExists)
                {
                    await adminClient.CreateTopicsAsync(new TopicSpecification[]
                    {
                        new TopicSpecification {Name = topicName, ReplicationFactor = 2, NumPartitions = 1},
                    });
                    Console.WriteLine($"Topic: {topicName} was successfully created");
                    return;
                }

                Console.WriteLine($"Topic: {topicName} already exists");
            }
        }
    }
}

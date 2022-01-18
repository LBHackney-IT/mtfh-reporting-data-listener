using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry.Serdes;
using FluentAssertions;
using MtfhReportingDataListener.Gateway;
using MtfhReportingDataListener.Gateway.Interfaces;
using System;
using Xunit;
using Avro;
using Avro.Generic;
using Schema = Avro.Schema;

namespace MtfhReportingDataListener.Tests.Gateway
{
    [Collection("LogCall collection")]
    public class KafkaGatewayTests : MockApplicationFactory
    {
        private readonly IKafkaGateway _gateway;

        public KafkaGatewayTests()
        {
            _gateway = new KafkaGateway();
        }

        [Fact]
        public void TenureUpdatedSendsDataToKafka()
        {
            var consumerconfig = new ConsumerConfig
            {
                BootstrapServers = Environment.GetEnvironmentVariable("DATAPLATFORM_KAFKA_HOSTNAME"),
                GroupId = "4c659d6b-4739-4579-9698-a27d1aaa397d",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            var topic = "mtfh-reporting-data-listener";

            var schemaString = @"{
                 ""type"": ""record"",
                 ""name"": ""Person"",
                 ""fields"": [
                   {
                     ""name"": ""firstName"",
                     ""type"": ""string""
                   }
                 ]
               }";

            var schema = (RecordSchema) Schema.Parse(schemaString);
            GenericRecord message = new GenericRecord(schema);
            message.Add("firstName", "Tom");
            var schemaWithMetadata = new Confluent.SchemaRegistry.Schema("tenure", 1, 1, schemaString);
            var result = _gateway.SendDataToKafka(topic, message, schemaWithMetadata);
            result.Success.Should().BeTrue();

            var schemaRegistryClient = new SchemaRegistryClient(schemaWithMetadata);

            using (var consumer = new ConsumerBuilder<Ignore, GenericRecord>(consumerconfig)
                .SetValueDeserializer(new AvroDeserializer<GenericRecord>(schemaRegistryClient).AsSyncOverAsync())
                .Build()
            )
            {
                consumer.Subscribe("mtfh-reporting-data-listener");
                var r = consumer.Consume(TimeSpan.FromSeconds(30));
                Assert.NotNull(r?.Message);
                Assert.Equal(message, r.Message.Value);
                consumer.Close();
            }
        }
    }
}

using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry.Serdes;
using FluentAssertions;
using MtfhReportingDataListener.Gateway;
using MtfhReportingDataListener.Gateway.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Avro;
using Avro.Generic;
using Schema = Avro.Schema;
using AutoFixture;
using Confluent.SchemaRegistry;
using SchemaRegistry = MtfhReportingDataListener.Tests.Helper.SchemaRegistry;


namespace MtfhReportingDataListener.Tests.Gateway
{
    [Collection("LogCall collection")]
    public class KafkaGatewayTests : MockApplicationFactory
    {
        private readonly IKafkaGateway _gateway;
        private readonly IFixture _fixture;
        private string _schemaRegistryUrl;

        public KafkaGatewayTests()
        {
            _gateway = new KafkaGateway();
            _fixture = new Fixture();
            _schemaRegistryUrl = Environment.GetEnvironmentVariable("KAFKA_SCHEMA_REGISTRY_HOSTNAME");
        }

        [Fact]
        public async Task TenureUpdatedandCreatedSendsDataToKafka()
        {
            var consumerconfig = new ConsumerConfig
            {
                BootstrapServers = Environment.GetEnvironmentVariable("DATAPLATFORM_KAFKA_HOSTNAME"),
                GroupId = "4c659d6b-4739-4579-9698-a27d1aaa397d",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            var topic = "tenure" + _fixture.Create<string>();

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
            await SchemaRegistry.SaveSchemaForTopic(HttpClient, _schemaRegistryUrl, schemaString, topic);

            var schema = (RecordSchema) Schema.Parse(schemaString);
            GenericRecord message = new GenericRecord(schema);
            message.Add("firstName", "Tom");
            var result = _gateway.SendDataToKafka(topic, message);
            result.Success.Should().BeTrue();

            using (var schemaRegistry = new CachedSchemaRegistryClient(new SchemaRegistryConfig
            {
                Url = _schemaRegistryUrl
            }))
            using (var consumer = new ConsumerBuilder<Ignore, GenericRecord>(consumerconfig)
                .SetValueDeserializer(new AvroDeserializer<GenericRecord>(schemaRegistry).AsSyncOverAsync())
                .Build()
            )
            {
                consumer.Subscribe(topic);
                var r = consumer.Consume(TimeSpan.FromSeconds(30));
                Assert.NotNull(r?.Message);
                Assert.Equal(message, r.Message.Value);
                consumer.Close();
            }
        }

        [Fact]
        public async Task ContactDetailAddedSendsDataToKafka()
        {
            var consumerconfig = new ConsumerConfig
            {
                BootstrapServers = Environment.GetEnvironmentVariable("DATAPLATFORM_KAFKA_HOSTNAME"),
                GroupId = "4c659d6b-4739-4579-9698-a27d1aaa397d",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            var topic = "contactDetail" + _fixture.Create<string>();

            var schemaString = @"{
                ""type"": ""record"",
                ""name"": ""ContactDetail"",
                ""fields"": [
                   {
                     ""name"": ""Id"",
                     ""type"": ""string""
                   },
                ]
                }";
            await SchemaRegistry.SaveSchemaForTopic(HttpClient, _schemaRegistryUrl, schemaString, topic);

            var schema = (RecordSchema) Schema.Parse(schemaString);
            GenericRecord message = new GenericRecord(schema);
            message.Add("TargetType", "person");
            var result = _gateway.SendDataToKafka(topic, message);
            result.Success.Should().BeTrue();

            using (var schemaRegistry = new CachedSchemaRegistryClient(new SchemaRegistryConfig
            {
                Url = _schemaRegistryUrl
            }))
            using (var consumer = new ConsumerBuilder<Ignore, GenericRecord>(consumerconfig)
                .SetValueDeserializer(new AvroDeserializer<GenericRecord>(schemaRegistry).AsSyncOverAsync())
                .Build()
            )
            {
                consumer.Subscribe(topic);
                var r = consumer.Consume(TimeSpan.FromSeconds(30));
                Assert.NotNull(r?.Message);
                Assert.Equal(message, r.Message.Value);
                consumer.Close();
            }
        }

        [Fact]
        public async Task CreatesKafkaTopicCreatesTopic()
        {
            var config = new AdminClientConfig()
            {
                BootstrapServers = Environment.GetEnvironmentVariable("DATAPLATFORM_KAFKA_HOSTNAME"),
            };

            var topicName = "mtfh-reporting-data-listener";

            await _gateway.CreateKafkaTopic(topicName).ConfigureAwait(false);

            using (var adminClient = new AdminClientBuilder(config)
                .Build())
            {
                var meta = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
                var topicsList = meta.Topics.Select(t => t.Topic);

                topicsList.Should().Contain(topicName);
            }
        }

    }
}

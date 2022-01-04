using AutoFixture;
using Confluent.Kafka;
using FluentAssertions;
using Hackney.Shared.Tenure.Boundary.Response;
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
        private readonly TenureResponseObject _message;
        private readonly Fixture _fixture = new Fixture();

        public KafkaGatewayTests()
        {
            _gateway = new KafkaGateway();
            _message = _fixture.Create<TenureResponseObject>();

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
            // var schemaRegistryUrl = Environment.GetEnvironmentVariable("SCHEMA_REGISTRY_HOST_NAME");
            // Do we need to upload to schema do the docker registry here?
            // Can we mount it directly into the container?

            var schema = (RecordSchema) Schema.Parse(@"{
                 ""type"": ""record"",
                 ""name"": ""Person"",
                 ""fields"": [
                   {
                     ""name"": ""firstName"",
                     ""type"": ""string""
                   },
                   {
                     ""name"": ""lastName"",
                     ""type"": ""string""
                   },
                   {
                     ""name"": ""id"",
                     ""type"": ""long""
                   }
                 ]
               }");
            GenericRecord message = new GenericRecord(schema);
            message.Add("firstName", "Tom");
            var result = _gateway.SendDataToKafka(topic, message);
            result.Success.Should().BeTrue();

            // var expectedReceivedMessage = _message.ToAvro();

            // using (var schemaResgistry = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = schemaRegistryUrl }))
            using (var consumer = new ConsumerBuilder<Ignore, GenericRecord>(consumerconfig).Build())
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

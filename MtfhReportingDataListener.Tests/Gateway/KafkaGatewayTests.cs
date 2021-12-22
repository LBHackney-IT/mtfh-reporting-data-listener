using AutoFixture;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using FluentAssertions;
using Hackney.Shared.Tenure.Boundary.Response;
using MtfhReportingDataListener.Gateway;
using MtfhReportingDataListener.Domain;
using MtfhReportingDataListener.Gateway.Interfaces;
using System;
using Xunit;
using MMH;

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
            var schemaRegistryUrl = Environment.GetEnvironmentVariable("SCHEMA_REGISTRY_HOST_NAME");
            // Do we need to upload to schema do the docker registry here?
            // Can we mount it directly into the container?


            var result = _gateway.SendDataToKafka(_message, topic);
            result.Success.Should().BeTrue();

            var expectedReceivedMessage = _message.ToAvro();

            using (var schemaRegistry = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = schemaRegistryUrl }))
            using (var consumer = new ConsumerBuilder<Ignore, TenureInformation>(consumerconfig).SetValueDeserializer(new AvroDeserializer<TenureInformation>(schemaRegistry).AsSyncOverAsync()).Build())
            {
                consumer.Subscribe("mtfh-reporting-data-listener");
                var r = consumer.Consume(TimeSpan.FromSeconds(30));
                Assert.NotNull(r?.Message);
                Assert.Equal(expectedReceivedMessage, r.Message.Value);
                consumer.Close();
            }
        }

    }
}

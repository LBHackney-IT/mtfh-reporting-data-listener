using AutoFixture;
using Avro;
using Avro.Generic;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using FluentAssertions;
using Hackney.Shared.Tenure.Boundary.Response;
using Moq;
using MtfhReportingDataListener.Boundary;
using MtfhReportingDataListener.Domain;
using MtfhReportingDataListener.Gateway;
using MtfhReportingDataListener.Gateway.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MtfhReportingDataListener.Tests.Gateway
{
    [Collection("LogCall collection")]
    public class KafkaGatewayTests : MockApplicationFactory
    {
        private readonly IKafkaGateway _gateway;
        private readonly TenureResponseObject _message;
        private readonly DomainEntity _domainEntity;
        private readonly Fixture _fixture = new Fixture();

        public KafkaGatewayTests()
        {
            _gateway = new KafkaGateway();
            _domainEntity = _fixture.Create<DomainEntity>();

            //var schema = (RecordSchema) Schema.Parse(@"{
            //          ""type"": ""record"",
            //          ""name"": ""Person"",
            //          ""fields"": [
            //            {
            //              ""name"": ""firstName"",
            //              ""type"": ""string""
            //            },
            //            {
            //              ""name"": ""lastName"",
            //              ""type"": ""string""
            //            },
            //            {
            //              ""name"": ""id"",
            //              ""type"": ""long""
            //            }
            //          ]
            //        }");
            _message = _fixture.Create<TenureResponseObject>();

        }

        [Fact]
        public void TenureUpdatedSendsDataToKafka()
        {
            //_message.Add("id", 5);
            //_message.Add("firstName", "Tom");
            //_message.Add("lastName", "Brown");
            var consumerconfig = new ConsumerConfig
            {
                BootstrapServers = Environment.GetEnvironmentVariable("DATAPLATFORM_KAFKA_HOSTNAME"),
                GroupId = "4c659d6b-4739-4579-9698-a27d1aaa397d",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            var topic = "mtfh-reporting-data-listener";
            var schemaRegistryUrl = "registryUrl";
            var result = _gateway.SendDataToKafka(_message, topic, schemaRegistryUrl);
            result.Success.Should().BeTrue();
            using (var schemaRegistry = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = schemaRegistryUrl }))
            using (var consumer = new ConsumerBuilder<Ignore, TenureResponseObject>(consumerconfig).SetValueDeserializer(new AvroDeserializer<TenureResponseObject>(schemaRegistry).AsSyncOverAsync()).Build())
            {
                consumer.Subscribe("mtfh-reporting-data-listener");
                var r = consumer.Consume(TimeSpan.FromSeconds(30));
                Assert.NotNull(r?.Message);
                //Assert.Equal(_message, r.Message.Value);
                consumer.Close();
            }
        }

    }
}

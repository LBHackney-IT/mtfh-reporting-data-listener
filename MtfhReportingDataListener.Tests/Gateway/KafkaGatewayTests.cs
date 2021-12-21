using AutoFixture;
using Confluent.Kafka;
using FluentAssertions;
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
        private readonly string _message;
        private readonly DomainEntity _domainEntity;
        private readonly Fixture _fixture = new Fixture();

        public KafkaGatewayTests()
        {
            _gateway = new KafkaGateway();
            _domainEntity = _fixture.Create<DomainEntity>();

            _message = _fixture.Create<string>();
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
            var result = _gateway.SendDataToKafka(_message, topic);
            result.Success.Should().BeTrue();
            using (var consumer = new ConsumerBuilder<Ignore, string>(consumerconfig).Build())
            {
                consumer.Subscribe("mtfh-reporting-data-listener");
                var r = consumer.Consume(TimeSpan.FromSeconds(30));
                Assert.NotNull(r?.Message);
                Assert.Equal(_message, r.Message.Value);
                consumer.Close();
            }
        }

    }
}

using FluentAssertions;
using SchemaRegistry = MtfhReportingDataListener.Gateway.SchemaRegistry;
using MtfhReportingDataListener.Gateway.Interfaces;
using System;
using System.Threading.Tasks;
using Xunit;
using AutoFixture;
using Moq;
using System.Net.Http;

namespace MtfhReportingDataListener.Tests.Gateway
{
    [Collection("LogCall collection")]
    public class SchemaRegistryTests : MockApplicationFactory
    {
        private readonly ISchemaRegistry _gateway;
        private readonly IFixture _fixture;
        private string _schemaRegistryUrl;
        private Mock<IHttpClientFactory> _mockHttpclientFactory;

        public SchemaRegistryTests()
        {
            _mockHttpclientFactory = new Mock<IHttpClientFactory>();
            _mockHttpclientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(HttpClient);
            _gateway = new SchemaRegistry(_mockHttpclientFactory.Object);
            _fixture = new Fixture();
            _schemaRegistryUrl = Environment.GetEnvironmentVariable("KAFKA_SCHEMA_REGISTRY_HOSTNAME");
        }

        [Fact]
        public async Task GetSchemaForTopicWillRetrieveLatestSchemaVersion()
        {
            var schema = "{\"type\":\"record\",\"name\":\"Person\",\"fields\":[{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"favouriteColour\",\"type\":\"string\"}]}";
            var topicName = _fixture.Create<string>();

            await Helper.SchemaRegistry.SaveSchemaForTopic(HttpClient, _schemaRegistryUrl, schema, topicName);

            var response = await _gateway.GetSchemaForTopic(topicName, _schemaRegistryUrl).ConfigureAwait(true);
            response.Should().BeEquivalentTo(schema);
        }
    }
}

using AutoFixture;
using MtfhReportingDataListener.Boundary;
using MtfhReportingDataListener.Gateway.Interfaces;
using MtfhReportingDataListener.Infrastructure.Exceptions;
using MtfhReportingDataListener.UseCase;
using MtfhReportingDataListener.Domain;
using FluentAssertions;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using Hackney.Shared.Tenure.Boundary.Response;
using Hackney.Shared.Tenure.Domain;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Avro.Generic;
using MtfhReportingDataListener.Helper;

namespace MtfhReportingDataListener.Tests.UseCase
{
    [Collection("LogCall collection")]
    public class TenureUseCaseTests : IDisposable
    {
        private readonly Mock<ITenureInfoApiGateway> _mockGateway;
        private readonly Mock<IKafkaGateway> _mockKafka;
        private readonly Mock<ISchemaRegistry> _mockSchemaRegistry;
        private readonly Mock<IConvertToAvroHelper> _mockConvertToAvroHelper;
        private readonly TenureUseCase _sut;
        private readonly TenureResponseObject _tenure;

        private readonly EntityEventSns _tenureUpdatedMessage;
        private readonly EntityEventSns _tenureCreatedMessage;

        private readonly Fixture _fixture;

        private readonly string _tenureSchemaName;

        public TenureUseCaseTests()
        {
            _fixture = new Fixture();

            _mockGateway = new Mock<ITenureInfoApiGateway>();
            _mockKafka = new Mock<IKafkaGateway>();
            _mockSchemaRegistry = new Mock<ISchemaRegistry>();
            _mockConvertToAvroHelper = new Mock<IConvertToAvroHelper>();
            _sut = new TenureUseCase(_mockGateway.Object, _mockKafka.Object, _mockSchemaRegistry.Object, _mockConvertToAvroHelper.Object);


            _tenure = CreateTenure();
            _tenureUpdatedMessage = CreateTenureUpdatedMessage();
            _tenureCreatedMessage = CreateTenureCreatedMessage();

            _tenureSchemaName = Environment.GetEnvironmentVariable("TENURE_SCHEMA_NAME");
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("TENURE_SCHEMA_NAME", _tenureSchemaName);
        }


        [Fact]
        public void ProcessMessageAsyncTestNullMessageThrows()
        {
            Func<Task> func = async () => await _sut.ProcessMessageAsync(null).ConfigureAwait(false);
            func.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public void ProcessMessageAsyncTestEntityIdNotFoundThrows()
        {
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_tenureCreatedMessage.EntityId, _tenureCreatedMessage.CorrelationId)).ReturnsAsync((TenureResponseObject) null);
            Func<Task> func = async () => await _sut.ProcessMessageAsync(null).ConfigureAwait(false);
            func.Should().ThrowAsync<EntityNotFoundException<TenureResponseObject>>();
        }

        [Fact]
        public void ProcessMessageAsyncTestSaveEntityThrows()
        {
            var exMsg = "This is the last error";
            var jsonTenure = JsonSerializer.Serialize(_tenureCreatedMessage);
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_tenureCreatedMessage.EntityId, _tenureCreatedMessage.CorrelationId))
                        .ThrowsAsync(new Exception(exMsg));

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_tenureCreatedMessage).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);
            _mockGateway.Verify(x => x.GetTenureInfoByIdAsync(_tenureCreatedMessage.EntityId, _tenureCreatedMessage.CorrelationId), Times.Once);
        }


        [Fact]
        public async Task ProcessMessageAsyncGetsTheSchema()
        {
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_tenureCreatedMessage.EntityId, _tenureCreatedMessage.CorrelationId))
                        .ReturnsAsync(_tenure);
            var mockSchemaResponse = @"{
                ""type"": ""record"",
                ""name"": ""Person"",
                ""fields"": [
                   {
                     ""name"": ""Id"",
                     ""type"": ""string""
                   },
                ]
                }";

            var schemaName = _fixture.Create<string>();
            Environment.SetEnvironmentVariable("TENURE_SCHEMA_NAME", schemaName);

            _mockSchemaRegistry.Setup(x => x.GetSchemaForTopic(schemaName)).ReturnsAsync(mockSchemaResponse).Verifiable();

            await _sut.ProcessMessageAsync(_tenureCreatedMessage).ConfigureAwait(false);
            _mockSchemaRegistry.Verify(x => x.GetSchemaForTopic(schemaName), Times.Once);
        }

        [Fact]
        public async Task CallsKafkaGatewayToCreateTopic()
        {
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(_tenure);

            var schemaResponse = @"{
                ""type"": ""record"",
                ""name"": ""Person"",
                ""fields"": [
                    {
                        ""name"": ""Id"",
                        ""type"": ""string""
                    },
                ]
            }";

            _mockSchemaRegistry.Setup(x => x.GetSchemaForTopic(It.IsAny<string>())).ReturnsAsync(schemaResponse);
            var schemaName = "mtfh-reporting-data-listener";

            Environment.SetEnvironmentVariable("TENURE_SCHEMA_NAME", schemaName);

            var message = new EntityEventSns();

            await _sut.ProcessMessageAsync(message);

            _mockKafka.Verify(x => x.CreateKafkaTopic(schemaName), Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsyncSendsDataToKafkaWithTenureCreatedEventType()
        {
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_tenureCreatedMessage.EntityId, _tenureCreatedMessage.CorrelationId))
                        .ReturnsAsync(_tenure);
            var mockSchemaResponse = @"{
                ""type"": ""record"",
                ""name"": ""Person"",
                ""fields"": [
                   {
                     ""name"": ""Id"",
                     ""type"": ""string""
                   },
                ]
                }";
            _mockSchemaRegistry.Setup(x => x.GetSchemaForTopic(It.IsAny<string>())).ReturnsAsync(mockSchemaResponse);
            var schemaName = "mtfh-reporting-data-listener";

            Environment.SetEnvironmentVariable("TENURE_SCHEMA_NAME", schemaName);

            await _sut.ProcessMessageAsync(_tenureCreatedMessage).ConfigureAwait(false);
            _mockKafka.Verify(x => x.SendDataToKafka(schemaName, It.IsAny<GenericRecord>()), Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsyncSendsDataToKafkaWithTenureUpdatedEventType()
        {
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_tenureUpdatedMessage.EntityId, _tenureUpdatedMessage.CorrelationId))
                        .ReturnsAsync(_tenure);
            var mockSchemaResponse = @"{
                ""type"": ""record"",
                ""name"": ""Person"",
                ""fields"": [
                   {
                     ""name"": ""Id"",
                     ""type"": ""string""
                   },
                ]
                }";
            _mockSchemaRegistry.Setup(x => x.GetSchemaForTopic(It.IsAny<string>())).ReturnsAsync(mockSchemaResponse);
            var schemaName = "mtfh-reporting-data-listener";

            Environment.SetEnvironmentVariable("TENURE_SCHEMA_NAME", schemaName);

            await _sut.ProcessMessageAsync(_tenureUpdatedMessage).ConfigureAwait(false);
            _mockKafka.Verify(x => x.SendDataToKafka(schemaName, It.IsAny<GenericRecord>()), Times.Once);
        }


        private EntityEventSns CreateTenureUpdatedMessage(string eventType = EventTypes.TenureUpdatedEvent)
        {
            return _fixture.Build<EntityEventSns>()
                           .With(x => x.EventType, eventType)
                           .Create();
        }
        private EntityEventSns CreateTenureCreatedMessage(string eventType = EventTypes.TenureCreatedEvent)
        {
            return _fixture.Build<EntityEventSns>()
                           .With(x => x.EventType, eventType)
                           .Create();
        }

        private TenureResponseObject CreateTenure()
        {
            return _fixture.Build<TenureResponseObject>()
                           .With(x => x.HouseholdMembers, _fixture.Build<HouseholdMembers>()
                                                                  .With(x => x.PersonTenureType, PersonTenureType.Tenant)
                                                                  .CreateMany(3)
                                                                  .ToList())
                           .Create();
        }

    }
}

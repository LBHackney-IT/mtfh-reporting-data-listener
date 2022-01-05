using AutoFixture;
using MtfhReportingDataListener.Boundary;
using MtfhReportingDataListener.Gateway.Interfaces;
using MtfhReportingDataListener.Infrastructure.Exceptions;
using MtfhReportingDataListener.UseCase;
using FluentAssertions;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using Hackney.Shared.Tenure.Boundary.Response;
using Hackney.Shared.Tenure.Domain;
using System.Linq;
using System.Text.Json;
using Avro.Generic;
using Confluent.SchemaRegistry;

namespace MtfhReportingDataListener.Tests.UseCase
{
    [Collection("LogCall collection")]
    public class TenureUpdatedUseCaseTests
    {
        private readonly Mock<ITenureInfoApiGateway> _mockGateway;
        private readonly Mock<IKafkaGateway> _mockKafka;
        private readonly Mock<IGlueGateway> _mockGlue;
        private readonly TenureUpdatedUseCase _sut;
        private readonly TenureResponseObject _tenure;

        private readonly EntityEventSns _message;

        private readonly Fixture _fixture;

        public TenureUpdatedUseCaseTests()
        {
            _fixture = new Fixture();

            _mockGateway = new Mock<ITenureInfoApiGateway>();
            _mockKafka = new Mock<IKafkaGateway>();
            _mockGlue = new Mock<IGlueGateway>();
            _sut = new TenureUpdatedUseCase(_mockGateway.Object, _mockKafka.Object, _mockGlue.Object);


            _tenure = CreateTenure();
            _message = CreateMessage();

        }

        private EntityEventSns CreateMessage(string eventType = EventTypes.TenureUpdatedEvent)
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

        [Fact]
        public void ProcessMessageAsyncTestNullMessageThrows()
        {
            Func<Task> func = async () => await _sut.ProcessMessageAsync(null).ConfigureAwait(false);
            func.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public void ProcessMessageAsyncTestEntityIdNotFoundThrows()
        {
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId)).ReturnsAsync((TenureResponseObject) null);
            Func<Task> func = async () => await _sut.ProcessMessageAsync(null).ConfigureAwait(false);
            func.Should().ThrowAsync<EntityNotFoundException<TenureResponseObject>>();
        }

        [Fact]
        public void ProcessMessageAsyncTestSaveEntityThrows()
        {
            var exMsg = "This is the last error";
            var jsonTenure = JsonSerializer.Serialize(_message);
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                        .ThrowsAsync(new Exception(exMsg));

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);
            _mockGateway.Verify(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId), Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsyncSendsDataToKafka()
        {
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                        .ReturnsAsync(_tenure);
            var mockSchema = @"{
                ""type"": ""record"",
                ""name"": ""Person"",
                ""fields"": [
                   {
                     ""name"": ""firstName"",
                     ""type"": ""string""
                   },
                ]
            }";
            _mockGlue.Setup(x => x.GetSchema("", "", "")).ReturnsAsync(mockSchema);

            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            _mockKafka.Verify(x => x.SendDataToKafka("mtfh-reporting-data-listener", It.IsAny<GenericRecord>(), It.IsAny<Schema>()), Times.Once);
        }

        //TODO - Check generic record has correct data
    }
}

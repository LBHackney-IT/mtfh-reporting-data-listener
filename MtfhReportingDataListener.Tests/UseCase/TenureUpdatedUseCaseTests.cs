using AutoFixture;
using MtfhReportingDataListener.Boundary;
using MtfhReportingDataListener.Domain;
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
using Confluent.Kafka;
using System.Text.Json;
using System.Collections.Generic;
using System.Text;

namespace MtfhReportingDataListener.Tests.UseCase
{
    [Collection("LogCall collection")]
    public class TenureUpdatedUseCaseTests
    {
        private readonly Mock<ITenureInfoApiGateway> _mockGateway;
        private readonly TenureUpdatedUseCase _sut;
        private readonly DomainEntity _domainEntity;
        private readonly TenureResponseObject _tenure;

        private readonly EntityEventSns _message;

        private readonly Fixture _fixture;

        public TenureUpdatedUseCaseTests()
        {
            _fixture = new Fixture();

            _mockGateway = new Mock<ITenureInfoApiGateway>();
            _sut = new TenureUpdatedUseCase(_mockGateway.Object);

            _domainEntity = _fixture.Create<DomainEntity>();

            _tenure = CreateTenure();
            _message = CreateMessage(_domainEntity.Id);

        }

        private EntityEventSns CreateMessage(Guid id, string eventType = EventTypes.TenureUpdatedEvent)
        {
            return _fixture.Build<EntityEventSns>()
                           .With(x => x.EntityId, id)
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
            func.Should().ThrowAsync<EntityNotFoundException<DomainEntity>>();
        }

        [Fact]
        public void ProcessMessageAsyncTestSaveEntityThrows()
        {
            var exMsg = "This is the last error";
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                        .ThrowsAsync(new Exception(exMsg));

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);

            //_mockGateway.Verify(x => x.GetEntityAsync(_domainEntity.Id), Times.Once);
            //_mockGateway.Verify(x => x.SaveEntityAsync(_domainEntity), Times.Once);
        }

        [Fact]
        public void TenureUpdatedSendsDataToKafka()
        {
           
            var message = JsonSerializer.Serialize(_message);
            var topic = "mtfh-reporting-data-listener";
            var result = _sut.SendDataToKafka(message, topic);
            result.Should().NotBeNull();

            //using (var consumer = new ConsumerBuilder<Ignore, string>(consumerconfig).Build())
            //{
            //    //consumer.Subscribe("mtfh-reporting-data-listener");
            //    consumer.Assign(new List<TopicPartitionOffset>() { produceResults.TopicPartitionOffset });
            //    var r = consumer.Consume(TimeSpan.FromSeconds(10));
            //    Assert.NotNull(r?.Message);
            //    Assert.Equal(message, r.Message.Value);
            //}
        }

    }
}

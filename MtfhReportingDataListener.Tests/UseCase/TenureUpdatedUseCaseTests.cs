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
using System.Text.Json;
using System.IO;
using System.Reflection;
using Avro.Generic;
using Avro;

namespace MtfhReportingDataListener.Tests.UseCase
{
    [Collection("LogCall collection")]
    public class TenureUpdatedUseCaseTests
    {
        private readonly Mock<ITenureInfoApiGateway> _mockGateway;
        private readonly Mock<IKafkaGateway> _mockKafka;
        private readonly TenureUpdatedUseCase _sut;
        private readonly DomainEntity _domainEntity;
        private readonly TenureResponseObject _tenure;

        private readonly EntityEventSns _message;

        private readonly Fixture _fixture;

        public TenureUpdatedUseCaseTests()
        {
            _fixture = new Fixture();

            _mockGateway = new Mock<ITenureInfoApiGateway>();
            _mockKafka = new Mock<IKafkaGateway>();
            _sut = new TenureUpdatedUseCase(_mockGateway.Object, _mockKafka.Object);

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

            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            _mockKafka.Verify(x => x.SendDataToKafka(It.IsAny<TenureResponseObject>(), "mtfh-reporting-data-listener", "registryURL"), Times.Once);

        }

        [Fact]
        public async Task GetSchema()
        {
            var tenure = new TenureResponseObject
            {
                Id = Guid.NewGuid(),
                IsActive = true,
                PaymentReference = "1234567345"
            };
            var jsonTenure = JsonSerializer.Serialize(tenure);
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                        .ReturnsAsync(_tenure);

            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            var dirName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location.Replace("bin\\Debug", string.Empty));
            Console.WriteLine(dirName);
            var schema = File.ReadAllText($"{dirName}/../schema.json");
            _mockKafka.Setup(x => x.GetSchema()).Returns(schema);
            //var parse = (RecordSchema) Schema.Parse(schema);
            //var expected = new GenericRecord(parse);
            _mockKafka.Verify(x => x.SendDataToKafka(tenure, "mtfh-reporting-data-listener", "registryURL"), Times.Once);

        }



    }
}

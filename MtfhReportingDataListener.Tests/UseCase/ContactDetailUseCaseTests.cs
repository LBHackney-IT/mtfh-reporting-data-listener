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
using System.Text.Json;
using Avro.Generic;
using Hackney.Shared.ContactDetail.Boundary.Response;
using MtfhReportingDataListener.Helper;

namespace MtfhReportingDataListener.Tests.UseCase
{
    [Collection("LogCall collection")]
    public class ContactDetailUseCaseTests : IDisposable
    {
        private readonly Mock<IContactDetailApiGateway> _mockGateway;
        private readonly Mock<IKafkaGateway> _mockKafka;
        private readonly Mock<ISchemaRegistry> _mockSchemaRegistry;
        private readonly Mock<IConvertToAvroHelper> _mockConvertToAvroHelper;
        private readonly ContactDetailUseCase _sut;
        private readonly ContactDetailsResponseObject _contactDetail;

        private readonly EntityEventSns _contactDetailAddedMessage;

        private readonly Fixture _fixture;

        private readonly string _contactDetailSchemaName;

        public ContactDetailUseCaseTests()
        {
            _fixture = new Fixture();

            _mockGateway = new Mock<IContactDetailApiGateway>();
            _mockKafka = new Mock<IKafkaGateway>();
            _mockSchemaRegistry = new Mock<ISchemaRegistry>();
            _mockConvertToAvroHelper = new Mock<IConvertToAvroHelper>();
            _sut = new ContactDetailUseCase(_mockGateway.Object, _mockKafka.Object, _mockSchemaRegistry.Object, _mockConvertToAvroHelper.Object);


            _contactDetail = CreateContactDetail();
            _contactDetailAddedMessage = CreateContactDetailAddedMessage();

            _contactDetailSchemaName = Environment.GetEnvironmentVariable("CONTACT_DETAIL_SCHEMA_NAME");
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("CONTACT_DETAIL_SCHEMA_NAME", _contactDetailSchemaName);
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
            _mockGateway.Setup(x => x.GetContactDetailByTargetIdAsync(_contactDetailAddedMessage.EntityId, _contactDetailAddedMessage.CorrelationId)).ReturnsAsync((ContactDetailsResponseObject) null);
            Func<Task> func = async () => await _sut.ProcessMessageAsync(null).ConfigureAwait(false);
            func.Should().ThrowAsync<EntityNotFoundException<ContactDetailsResponseObject>>();
        }

        [Fact]
        public void ProcessMessageAsyncTestSaveEntityThrows()
        {
            var exMsg = "This is the last error";
            var jsonTenure = JsonSerializer.Serialize(_contactDetailAddedMessage);
            _mockGateway.Setup(x => x.GetContactDetailByTargetIdAsync(_contactDetailAddedMessage.EntityId, _contactDetailAddedMessage.CorrelationId))
                        .ThrowsAsync(new Exception(exMsg));

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_contactDetailAddedMessage).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);
            _mockGateway.Verify(x => x.GetContactDetailByTargetIdAsync(_contactDetailAddedMessage.EntityId, _contactDetailAddedMessage.CorrelationId), Times.Once);
        }


        [Fact]
        public async Task ProcessMessageAsyncGetsTheSchema()
        {
            _mockGateway.Setup(x => x.GetContactDetailByTargetIdAsync(_contactDetailAddedMessage.EntityId, _contactDetailAddedMessage.CorrelationId))
                        .ReturnsAsync(_contactDetail);
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
            Environment.SetEnvironmentVariable("CONTACT_DETAIL_SCHEMA_NAME", schemaName);

            _mockSchemaRegistry.Setup(x => x.GetSchemaForTopic(schemaName)).ReturnsAsync(mockSchemaResponse).Verifiable();

            await _sut.ProcessMessageAsync(_contactDetailAddedMessage).ConfigureAwait(false);
            _mockSchemaRegistry.Verify();
        }

        [Fact]
        public async Task CallsKafkaGatewayToCreateTopic()
        {
            _mockGateway.Setup(x => x.GetContactDetailByTargetIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(_contactDetail);

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

            Environment.SetEnvironmentVariable("CONTACT_DETAIL_SCHEMA_NAME", schemaName);

            var message = new EntityEventSns();

            await _sut.ProcessMessageAsync(message);

            _mockKafka.Verify(x => x.CreateKafkaTopic(schemaName), Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsyncSendsDataToKafkaWithContactDetailAddedEventType()
        {
            _mockGateway.Setup(x => x.GetContactDetailByTargetIdAsync(_contactDetailAddedMessage.EntityId, _contactDetailAddedMessage.CorrelationId))
                        .ReturnsAsync(_contactDetail);
            var mockSchemaResponse = @"{
                ""type"": ""record"",
                ""name"": ""ContactDetail"",
                ""fields"": [
                   {
                     ""name"": ""Id"",
                     ""type"": ""string""
                   },
                ]
                }";
            _mockSchemaRegistry.Setup(x => x.GetSchemaForTopic(It.IsAny<string>())).ReturnsAsync(mockSchemaResponse);
            var schemaName = "mtfh-reporting-data-listener";

            Environment.SetEnvironmentVariable("CONTACT_DETAIL_SCHEMA_NAME", schemaName);

            await _sut.ProcessMessageAsync(_contactDetailAddedMessage).ConfigureAwait(false);
            _mockKafka.Verify(x => x.SendDataToKafka(schemaName, It.IsAny<GenericRecord>()), Times.Once);
        }

        private EntityEventSns CreateContactDetailAddedMessage(string eventType = EventTypes.ContactDetailAddedEvent)
        {
            return _fixture.Build<EntityEventSns>()
                           .With(x => x.EventType, eventType)
                           .Create();
        }

        private ContactDetailsResponseObject CreateContactDetail()
        {
            return _fixture.Create<ContactDetailsResponseObject>();
        }

    }
}

using MtfhReportingDataListener.Boundary;
using MtfhReportingDataListener.Domain;
using MtfhReportingDataListener.Gateway.Interfaces;
using MtfhReportingDataListener.Infrastructure.Exceptions;
using MtfhReportingDataListener.UseCase.Interfaces;
using Hackney.Core.Logging;
using System;
using System.Threading.Tasks;
using Avro;
using Avro.Generic;
using System.Linq;
using System.Text.Json;
using System.Reflection;
using Newtonsoft.Json;
using Hackney.Shared.ContactDetail.Boundary.Response;
using MtfhReportingDataListener.Helper;

namespace MtfhReportingDataListener.UseCase
{
    public class ContactDetailUseCase : IContactDetailUseCase
    {
        private readonly IContactDetailApiGateway _contactDetailApi;
        private readonly IKafkaGateway _kafkaGateway;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly IConvertToAvroHelper _convertToAvroHelper;

        public ContactDetailUseCase(IContactDetailApiGateway gateway, IKafkaGateway kafkaGateway,
                                    ISchemaRegistry schemaRegistry, IConvertToAvroHelper convertToAvroHelper)
        {
            _contactDetailApi = gateway;
            _kafkaGateway = kafkaGateway;
            _schemaRegistry = schemaRegistry;
            _convertToAvroHelper = convertToAvroHelper;
        }

        [LogCall]
        public async Task ProcessMessageAsync(EntityEventSns message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            var contactDetail = await _contactDetailApi.GetContactDetailByTargetIdAsync(message.EntityId, message.CorrelationId)
                                             .ConfigureAwait(false);
            if (contactDetail is null) throw new EntityNotFoundException<ContactDetailsResponseObject>(message.EntityId);

            var contactDetailChangeEvent = new ContactDetailEvent
            {
                Id = message.Id,
                EventType = message.EventType,
                SourceDomain = message.SourceDomain,
                SourceSystem = message.SourceSystem,
                Version = message.Version,
                CorrelationId = message.CorrelationId,
                DateTime = message.DateTime,
                User = new Domain.User
                {
                    Name = message.User?.Name,
                    Email = message.User?.Email
                },
                ContactDetails = contactDetail
            };

            var topicName = Environment.GetEnvironmentVariable("CONTACT_DETAIL_SCHEMA_NAME");
            var schema = await _schemaRegistry.GetSchemaForTopic(topicName);
            var record = _convertToAvroHelper.BuildRecord(schema, contactDetailChangeEvent);

            await _kafkaGateway.CreateKafkaTopic(topicName);
            _kafkaGateway.SendDataToKafka(topicName, record);
        }
    }
}

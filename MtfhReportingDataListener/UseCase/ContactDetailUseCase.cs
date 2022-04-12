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

namespace MtfhReportingDataListener.UseCase
{
    public class ContactDetailUseCase : IContactDetailUseCase
    {
        private readonly IContactDetailApiGateway _contactDetailApi;
        private readonly IKafkaGateway _kafkaGateway;
        private readonly ISchemaRegistry _schemaRegistry;

        public ContactDetailUseCase(IContactDetailApiGateway gateway, IKafkaGateway kafkaGateway, ISchemaRegistry schemaRegistry)
        {
            _contactDetailApi = gateway;
            _kafkaGateway = kafkaGateway;
            _schemaRegistry = schemaRegistry;
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

            var topicName = Environment.GetEnvironmentVariable("TENURE_SCHEMA_NAME");
            var schema = await _schemaRegistry.GetSchemaForTopic(topicName);
            var record = BuildContactDetailRecord(schema, contactDetailChangeEvent);

            await _kafkaGateway.CreateKafkaTopic(topicName);
            _kafkaGateway.SendDataToKafka(topicName, record);
        }

        public GenericRecord BuildContactDetailRecord(string schema, ContactDetailEvent contactDetailResponse)
        {
            return PopulateFields(contactDetailResponse, Schema.Parse(schema));
        }

        public GenericRecord PopulateFields(object item, Schema schema)
        {
            var record = new GenericRecord((RecordSchema) schema);
            ((RecordSchema) schema).Fields.ForEach(field =>
            {
                PropertyInfo propInfo = item.GetType().GetProperty(field.Name);
                if (propInfo == null)
                {
                    Console.WriteLine($"Field name: {field.Name} not found in {item} with type {item.GetType()}");
                    return;
                }

                var fieldValue = propInfo.GetValue(item);
                var fieldSchema = field.Schema;
                var fieldType = field.Schema.Tag;

                if (fieldType == Schema.Type.Union)
                {
                    if (fieldValue == null)
                    {
                        record.Add(field.Name, null);
                        return;
                    }
                    fieldSchema = GetNonNullablePartOfNullableSchema(field.Schema);
                    fieldType = fieldSchema.Tag;
                }

                if (fieldType == Schema.Type.String)
                {
                    record.Add(field.Name, fieldValue.ToString());
                }
                else if (fieldType == Schema.Type.Enumeration)
                {
                    record.Add(field.Name, new GenericEnum((EnumSchema) fieldSchema, fieldValue.ToString()));
                }
                else if (fieldValue.GetType() == typeof(DateTime))
                {
                    record.Add(field.Name, UnixTimestampNullable(fieldValue));
                }
                else if (fieldType == Schema.Type.Array)
                {
                    var itemsType = fieldValue.GetType().GetGenericArguments().FirstOrDefault();
                    var itemsSchema = GetSchemaForArrayItems(fieldSchema);

                    var fieldValueAsList = ((JsonElement) System.Text.Json.JsonSerializer.Deserialize<object>(JsonConvert.SerializeObject(fieldValue))).EnumerateArray();
                    var recordsList = fieldValueAsList.Select(listItem => PopulateFields(JsonConvert.DeserializeObject(listItem.ToString(), itemsType), itemsSchema)).ToArray();

                    record.Add(field.Name, recordsList);
                }
                else if (fieldType == Schema.Type.Record)
                {
                    record.Add(field.Name, PopulateFields(fieldValue, fieldSchema));
                }
                else
                {
                    record.Add(field.Name, fieldValue);
                }
            });

            return record;
        }

        private Schema GetSchemaForArrayItems(Schema arraySchema)
        {
            var jsonSchema = (JsonElement) System.Text.Json.JsonSerializer.Deserialize<object>(arraySchema.ToString());
            jsonSchema.TryGetProperty("items", out var itemsSchemaJson);
            return Schema.Parse(itemsSchemaJson.ToString());
        }

        private Schema GetNonNullablePartOfNullableSchema(Schema nullableSchema)
        {
            var jsonSchema = (JsonElement) System.Text.Json.JsonSerializer.Deserialize<object>(nullableSchema.ToString());
            jsonSchema.TryGetProperty("type", out var unionList);
            var notNullSchema = unionList.EnumerateArray().First(type => type.ToString() != "null").ToString();
            return Schema.Parse(notNullSchema);
        }

        private int? UnixTimestampNullable(object obj)
        {
            var date = (DateTime?) obj;
            return (int?) (date?.Subtract(new DateTime(1970, 1, 1)))?.TotalSeconds;
        }
    }
}

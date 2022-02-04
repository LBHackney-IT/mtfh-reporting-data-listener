using MtfhReportingDataListener.Boundary;
using MtfhReportingDataListener.Domain;
using MtfhReportingDataListener.Gateway.Interfaces;
using MtfhReportingDataListener.Infrastructure.Exceptions;
using MtfhReportingDataListener.UseCase.Interfaces;
using Hackney.Core.Logging;
using System;
using System.Threading.Tasks;
using Hackney.Shared.Tenure.Boundary.Response;
using Avro;
using Avro.Generic;
using System.Linq;
using System.Text.Json;
using System.Reflection;
using Newtonsoft.Json;

namespace MtfhReportingDataListener.UseCase
{
    public class TenureUseCase : ITenureUseCase
    {
        private readonly ITenureInfoApiGateway _tenureInfoApi;
        private readonly IKafkaGateway _kafkaGateway;
        private readonly IGlueGateway _glueGateway;

        public TenureUseCase(ITenureInfoApiGateway gateway, IKafkaGateway kafkaGateway, IGlueGateway glueGateway)
        {
            _tenureInfoApi = gateway;
            _kafkaGateway = kafkaGateway;
            _glueGateway = glueGateway;
        }

        [LogCall]
        public async Task ProcessMessageAsync(EntityEventSns message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            var tenure = await _tenureInfoApi.GetTenureInfoByIdAsync(message.EntityId, message.CorrelationId)
                                             .ConfigureAwait(false);
            if (tenure is null) throw new EntityNotFoundException<TenureResponseObject>(message.EntityId);

            var schemaArn = Environment.GetEnvironmentVariable("SCHEMA_ARN");
            var schema = await _glueGateway.GetSchema(schemaArn).ConfigureAwait(false);

            var schemaWithMetadata = new Confluent.SchemaRegistry.Schema("tenure", 1, 1, schema.Schema);
            var tenureChangeEvent = new TenureEvent
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
                    Name = message.User.Name,
                    Email = message.User.Email
                },
                Tenure = tenure
            };
            var record = BuildTenureRecord(schema.Schema, tenureChangeEvent);

            var topic = "mtfh-reporting-data-listener";
            _kafkaGateway.SendDataToKafka(topic, record, schemaWithMetadata);
        }

        public GenericRecord BuildTenureRecord(string schema, TenureEvent tenureResponse)
        {
            return PopulateFields(tenureResponse, Schema.Parse(schema));
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

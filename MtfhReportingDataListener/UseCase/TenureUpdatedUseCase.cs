using MtfhReportingDataListener.Boundary;
using MtfhReportingDataListener.Gateway.Interfaces;
using MtfhReportingDataListener.Infrastructure.Exceptions;
using MtfhReportingDataListener.UseCase.Interfaces;
using Hackney.Core.Logging;
using System;
using System.Threading.Tasks;
using Hackney.Shared.Tenure.Boundary.Response;
using Hackney.Shared.Tenure.Domain;
using Avro;
using Avro.Generic;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

namespace MtfhReportingDataListener.UseCase
{
    public class TenureUpdatedUseCase : ITenureUpdatedUseCase
    {
        private readonly ITenureInfoApiGateway _tenureInfoApi;
        private readonly IKafkaGateway _kafkaGateway;
        private readonly IGlueGateway _glueGateway;

        public TenureUpdatedUseCase(ITenureInfoApiGateway gateway, IKafkaGateway kafkaGateway, IGlueGateway glueGateway)
        {
            _tenureInfoApi = gateway;
            _kafkaGateway = kafkaGateway;
            _glueGateway = glueGateway;
        }

        [LogCall]
        public async Task ProcessMessageAsync(EntityEventSns message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            // Get the tenure
            var tenure = await _tenureInfoApi.GetTenureInfoByIdAsync(message.EntityId, message.CorrelationId)
                                             .ConfigureAwait(false);
            if (tenure is null) throw new EntityNotFoundException<TenureResponseObject>(message.EntityId);

            // Get the schema
            var schemaArn = Environment.GetEnvironmentVariable("SCHEMA_ARN");
            var registryName = Environment.GetEnvironmentVariable("REGISTRY_NAME");
            var schemaName = Environment.GetEnvironmentVariable("SCHEMA_NAME");
            var schema = await _glueGateway.GetSchema(registryName, schemaArn, schemaName).ConfigureAwait(false);

            // (subject, version, Id, string) -> get from glue?
            var schemaWithMetadata = new Confluent.SchemaRegistry.Schema("tenure", 1, 1, schema.Schema);
            var logMessageSchema = (RecordSchema) Schema.Parse(schema.Schema);

            // build record here
            var record = BuildTenureRecord(logMessageSchema, tenure);

            // Send the data in Kafka
            var topic = "mtfh-reporting-data-listener";
            _kafkaGateway.SendDataToKafka(topic, record, schemaWithMetadata);
        }

        public GenericRecord PopulateFields(object item, RecordSchema schema)
        {
            var record = new GenericRecord(schema);
            schema.Fields.ForEach(field =>
            {
                var type = item.GetType();
                if (field.Name == "TenuredAssetType")
                {
                    Console.WriteLine(item);
                    Console.WriteLine(item.GetType());
                    Console.WriteLine(field.Name);
                    Console.WriteLine(type.GetProperties());
                }
                var fieldValue = type.GetProperty(field.Name).GetValue(item);
                var fieldType = field.Schema.Tag;


                if (fieldType == Schema.Type.String)
                {
                    record.Add(field.Name, fieldValue.ToString());
                }
                else if (fieldType == Schema.Type.Enumeration)
                {
                    record.Add(field.Name, new GenericEnum((EnumSchema) field.Schema, fieldValue.ToString()));
                }
                else if (fieldValue.GetType() == typeof(DateTime))
                {
                    record.Add(field.Name, UnixTimestampNullable(fieldValue));
                }
                else if (fieldType == Schema.Type.Array)
                {
                    var fieldValueAsList = (List<HouseholdMembers>) fieldValue;

                    var listRecords = fieldValueAsList.Select(listItem =>
                    {
                        var jsonSchema = (JsonElement) JsonSerializer.Deserialize<object>(field.Schema.ToString());
                        jsonSchema.TryGetProperty("items", out var itemsSchemaJson);
                        var itemsSchema = (RecordSchema) Schema.Parse(itemsSchemaJson.ToString());
                        return PopulateFields(listItem, itemsSchema);
                    }).ToArray();

                    record.Add(field.Name, listRecords);
                }
                else if (fieldType == Schema.Type.Record)
                {
                    record.Add(field.Name, PopulateFields(fieldValue, (RecordSchema) field.Schema));
                }
                else
                {
                    record.Add(field.Name, fieldValue);
                }
            });

            return record;
        }

        public GenericRecord BuildTenureRecord(RecordSchema schema, TenureResponseObject tenureResponse)
        {
            return PopulateFields(tenureResponse, schema);
        }

        private int UnixTimestamp(object obj)
        {
            var date = (DateTime) obj;
            return (int) (date.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        private int? UnixTimestampNullable(object obj)
        {
            var date = (DateTime?) obj;
            return (int?) (date?.Subtract(new DateTime(1970, 1, 1)))?.TotalSeconds;
        }
    }
}

using MtfhReportingDataListener.Boundary;
using MtfhReportingDataListener.Gateway.Interfaces;
using MtfhReportingDataListener.Infrastructure.Exceptions;
using MtfhReportingDataListener.UseCase.Interfaces;
using Hackney.Core.Logging;
using System;
using System.Threading.Tasks;
using System.Reflection;
using Hackney.Shared.Tenure.Boundary.Response;
using Hackney.Shared.Tenure.Domain;
using Avro;
using Avro.Generic;
using System.Linq;
using System.Collections.Generic;

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

        public GenericRecord BuildTenureRecord(RecordSchema schema, TenureResponseObject tenureResponse)
        {
            var record = new GenericRecord(schema);

            schema.Fields.ForEach(field =>
            {
                Type tenureType = tenureResponse.GetType();
                PropertyInfo propInfo = tenureType.GetProperty(field.Name);

                var fieldValue = propInfo.GetValue(tenureResponse);
                var fieldtype = field.Schema.Tag.ToString();

                if (fieldtype == "String")
                {
                    record.Add(field.Name, fieldValue.ToString());
                    return;
                }
                if (fieldValue.GetType() == typeof(DateTime))
                {
                    record.Add(field.Name, UnixTimestampNullable(fieldValue));
                    return;
                }
                if (field.Schema.Name == "array")
                {
                    var items = new List<GenericRecord>();
                    (List<HouseholdMembers>) fieldValue

                }
                record.Add(field.Name, fieldValue);
            });

            return record;
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

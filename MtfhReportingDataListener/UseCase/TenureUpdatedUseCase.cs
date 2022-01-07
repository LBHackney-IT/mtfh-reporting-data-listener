using MtfhReportingDataListener.Boundary;
using MtfhReportingDataListener.Gateway.Interfaces;
using MtfhReportingDataListener.Infrastructure.Exceptions;
using MtfhReportingDataListener.UseCase.Interfaces;
using Hackney.Core.Logging;
using System;
using System.Threading.Tasks;
using System.Reflection;
using Hackney.Shared.Tenure.Boundary.Response;
using Avro;
using Avro.Generic;

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
            var schemaArn = "";
            var registryName = "";
            var schemaName = "";
            var schema = await _glueGateway.GetSchema("", "", "").ConfigureAwait(false);

            // (subject, version, Id, string) -> get from glue?
            var schemaWithMetadata = new Confluent.SchemaRegistry.Schema("tenure", 1, 1, schema.Schema);
            var logMessageSchema = (RecordSchema) Schema.Parse(schema.Schema);

            // build record here
            var record = BuildTenureRecord(logMessageSchema, tenure);

            // Send the data in Kafka
            var topic = "mtfh-reporting-data-listener";
            _kafkaGateway.SendDataToKafka(topic, record, schemaWithMetadata);
        }

        public GenericRecord BuildTenureRecord(RecordSchema schema, TenureResponseObject tenure)
        {
            var record = new GenericRecord(schema);

            schema.Fields.ForEach(field =>
            {
                Type tenureType = typeof(TenureResponseObject);
                PropertyInfo propInfo = tenureType.GetProperty(field.Name);

                var fieldValue = propInfo.GetValue(tenure);
                var fieldtype = field.Schema.Tag.ToString();

                if (fieldtype == "String")
                {
                    record.Add(field.Name, fieldValue.ToString());
                    return;
                }

                record.Add(field.Name, fieldValue);
            });

            return record;
        }
    }
}

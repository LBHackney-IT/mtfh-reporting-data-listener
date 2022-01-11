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
                var tenure = AsTenure(tenureResponse);

                Type tenureType = typeof(Tenure);
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

        public class Tenure : TenureResponseObject
        {
            private int? _subletEndDate;
            private int? _potentialEndDate;
            private int? _evictionDate;
            private DateTime? _successionDate;
            public new int? SubletEndDate
            {
                get => _subletEndDate;
                set
                {
                    _subletEndDate = UnixTimestampNullable(value);
                }
            }

            public new int? PotentialEndDate
            {
                get => _potentialEndDate;
                set
                {
                    _potentialEndDate = UnixTimestampNullable(value);
                }
            }

            public new int? EvictionDate
            {
                get => _evictionDate;
                set
                {
                    _evictionDate = UnixTimestampNullable(value);
                }
            }

            public new DateTime? SuccessionDate
            {
                get => UnixTimestampNullable(_successionDate);
                set
                {
                    _successionDate = value;
                }
            }

            private int? UnixTimestampNullable(object obj)
            {
                var date = (DateTime?) obj;
                return (int?) (date?.Subtract(new DateTime(1970, 1, 1)))?.TotalSeconds;
            }
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

        public Tenure AsTenure(TenureResponseObject tenureResponse)
        {
            var tenure = new Tenure();

            PropertyInfo[] properties = tenure.GetType().GetProperties();
            foreach (var property in properties)
            {
                try
                {
                    property.SetValue(tenure, property.GetValue(tenureResponse));
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Failing on {property.Name}");
                    Console.WriteLine(ex.Message);
                }
            }

            return tenure;
        }
    }
}

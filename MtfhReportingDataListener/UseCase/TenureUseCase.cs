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
using MtfhReportingDataListener.Helper;

namespace MtfhReportingDataListener.UseCase
{
    public class TenureUseCase : ITenureUseCase
    {
        private readonly ITenureInfoApiGateway _tenureInfoApi;
        private readonly IKafkaGateway _kafkaGateway;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly IConvertToAvroHelper _convertToAvroHelper;

        public TenureUseCase(ITenureInfoApiGateway gateway, IKafkaGateway kafkaGateway,
                             ISchemaRegistry schemaRegistry, IConvertToAvroHelper convertToAvroHelper)
        {
            _tenureInfoApi = gateway;
            _kafkaGateway = kafkaGateway;
            _schemaRegistry = schemaRegistry;
            _convertToAvroHelper = convertToAvroHelper;
        }

        [LogCall]
        public async Task ProcessMessageAsync(EntityEventSns message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            var tenure = await _tenureInfoApi.GetTenureInfoByIdAsync(message.EntityId, message.CorrelationId)
                                             .ConfigureAwait(false);
            if (tenure is null) throw new EntityNotFoundException<TenureResponseObject>(message.EntityId);

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
                    Name = message.User?.Name,
                    Email = message.User?.Email
                },
                Tenure = tenure
            };

            var topicName = Environment.GetEnvironmentVariable("TENURE_SCHEMA_NAME");
            var schema = await _schemaRegistry.GetSchemaForTopic(topicName);
            var record = _convertToAvroHelper.BuildRecord(schema, tenureChangeEvent);

            await _kafkaGateway.CreateKafkaTopic(topicName);
            _kafkaGateway.SendDataToKafka(topicName, record);
        }
    }
}

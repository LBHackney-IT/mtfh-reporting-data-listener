using MtfhReportingDataListener.Boundary;
using MtfhReportingDataListener.Domain;
using MtfhReportingDataListener.Gateway.Interfaces;
using MtfhReportingDataListener.Infrastructure.Exceptions;
using MtfhReportingDataListener.UseCase.Interfaces;
using Hackney.Core.Logging;
using System;
using System.Threading.Tasks;
using Hackney.Shared.Tenure.Boundary.Response;

using System.Text.Json;
using Confluent.Kafka;
using System.Collections.Generic;
using Avro;
using System.IO;
using Avro.Generic;

namespace MtfhReportingDataListener.UseCase
{
    public class TenureUpdatedUseCase : ITenureUpdatedUseCase
    {
        private readonly ITenureInfoApiGateway _tenureInfoApi;
        private readonly IKafkaGateway _kafkaGateway;

        public TenureUpdatedUseCase(ITenureInfoApiGateway gateway, IKafkaGateway kafkaGateway)
        {
            _tenureInfoApi = gateway;
            _kafkaGateway = kafkaGateway;
        }

        [LogCall]
        public async Task ProcessMessageAsync(EntityEventSns message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            // #1 - Get the tenure
            var tenure = await _tenureInfoApi.GetTenureInfoByIdAsync(message.EntityId, message.CorrelationId)
                                             .ConfigureAwait(false);
            if (tenure is null) throw new EntityNotFoundException<TenureResponseObject>(message.EntityId);

            //#2 - Convert the data to avro
            var schema = _kafkaGateway.GetSchema();
            //var logMessageSchema = (RecordSchema) Schema.Parse(schema);
            //var avro = new GenericRecord(logMessageSchema);
            //avro.Add("Id", tenure.Id);
            //#3 - Send the data in Kafka
            var jsonTenure = JsonSerializer.Serialize(tenure);
            var topic = "mtfh-reporting-data-listener";
            _kafkaGateway.SendDataToKafka(tenure, topic, "registryURL");
        }


    }
}

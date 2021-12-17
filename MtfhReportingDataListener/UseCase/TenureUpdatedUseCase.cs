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

namespace MtfhReportingDataListener.UseCase
{
    public class TenureUpdatedUseCase : ITenureUpdatedUseCase
    {
        private readonly ITenureInfoApiGateway _tenureInfoApi;

        public TenureUpdatedUseCase(ITenureInfoApiGateway gateway)
        {
            _tenureInfoApi = gateway;
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


            //#3 - Send the data in Kafka
            Console.WriteLine(tenure);
            var jsonTenure = JsonSerializer.Serialize(tenure);
            var config = new ProducerConfig
            {
                BootstrapServers = "http://localhost:9092",
                ClientId = "mtfh-reporting-data-listener"
            };

            using (var producer = new ProducerBuilder<string, string>(config).Build())
            {

                producer.Produce("mtfh-reporting-data-listener",
                                 new Message<string, string>
                                 {
                                     Key = Guid.NewGuid().ToString(),
                                     Value = jsonTenure
                                 },
                                 null);
                producer.Flush(TimeSpan.FromSeconds(10));
            }

            var consumerconfig = new ConsumerConfig
            {
                BootstrapServers = "http://localhost:9092",
                GroupId = "4c659d6b-4739-4579-9698-a27d1aaa397d",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            using (var consumer = new ConsumerBuilder<Ignore, string>(consumerconfig).Build())
            {
                consumer.Subscribe("mtfh-reporting-data-listener");

                
                    var result = consumer.Consume();
                    Console.WriteLine(result);

                consumer.Close();
            }
        }
    }
}

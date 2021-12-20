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

            var jsonTenure = JsonSerializer.Serialize(tenure);
            var topic = "mtfh-reporting-data-listener";
            SendDataToKafka(jsonTenure, topic );
            //#2 - Convert the data to avro


            //#3 - Send the data in Kafka
          

        }

        public DeliveryReport<string, string> SendDataToKafka(string message, string topic)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "http://localhost:9092",
                ClientId = "mtfh-reporting-data-listener"
            };
            // DeliveryResult<string, string> result; 
            //using (var producer = new ProducerBuilder<string, string>(config).Build())
            //{

            //     producer.Produce(topic,
            //                     new Message<string, string>
            //                     {
            //                         Key = Guid.NewGuid().ToString(),
            //                         Value = message
            //                     });
            //    producer.Flush(TimeSpan.FromSeconds(1));

            //}

            //var consumerconfig = new ConsumerConfig
            //{
            //    BootstrapServers = "http://localhost:9092",
            //    GroupId = "4c659d6b-4739-4579-9698-a27d1aaa397d",
            //    AutoOffsetReset = AutoOffsetReset.Earliest
            //};

            //using (var consumer = new ConsumerBuilder<Ignore, string>(consumerconfig).Build())
            //{
                //consumer.Subscribe("mtfh-reporting-data-listener");
                // consumer.Assign(new List<TopicPartitionOffset>() { topic });
                    DeliveryReport<string, string> deliveryReport = null;

                using (var producer = new ProducerBuilder<string, string>(config).Build())
                {
                    int numProduced = 0;
                    producer.Produce(topic,
                                    new Message<string, string>
                                    {
                                        Key = Guid.NewGuid().ToString(),
                                        Value = message
                                    },
                    (report) =>
                    {
                        deliveryReport = report;
                        if (deliveryReport.Error.Code != ErrorCode.NoError)
                        {
                            throw new Exception(deliveryReport.Error.Reason);
                        }
                        else
                        {
                            Console.WriteLine($"Produced message to: {deliveryReport.TopicPartitionOffset}");
                            numProduced += 1;
                        }
                    });
                    var poll = producer.Poll(TimeSpan.FromSeconds(10));

                    
                    
                    producer.Flush(TimeSpan.FromSeconds(10));

                //}
                    return deliveryReport;
                    //var r = consumer.Consume(TimeSpan.FromSeconds(10));

               
            }
        }
    }
}

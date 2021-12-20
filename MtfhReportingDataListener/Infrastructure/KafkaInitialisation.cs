using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MtfhReportingDataListener.Boundary;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MtfhReportingDataListener.Infrastructure
{
    public static class KafkaInitialisation
    {
        public static IServiceCollection ConfigureKafka(this IServiceCollection services)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = Environment.GetEnvironmentVariable("hostname"),
                ClientId = "mtfh-reporting-data-listener"
            };

            try
            {
                var producer = new ProducerBuilder<String, String>(config).Build();
                producer.Produce("mtfh-reporting-data-listener",
                                 new Message<string, string>
                                 {
                                     Key = Guid.NewGuid().ToString(),
                                     Value = "New Message: " + DateTime.Now.ToString()
                                 },
                                 null);
                producer.Flush(TimeSpan.FromSeconds(0));
                return (IServiceCollection) producer;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            //finally
            //{
            //    if (producer != null)
            //    {
            //        ((IDisposable) producer).Dispose();
            //    }
            //}



        }
    }
}

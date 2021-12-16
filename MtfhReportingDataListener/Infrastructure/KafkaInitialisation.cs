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
        public async static IServiceCollection ConfigureKafka(this IServiceCollection services)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = Environment.GetEnvironmentVariable("hostname"),
                ClientId = "mtfh-reporting-data-listener"
            };

            using ( var producer = new ProducerBuilder<Null, string>(config).Build())
            {
                var message = JsonSerializer.Serialize<EntityEventSns>();
                producer.Produce("mtfh-reporting", message);
            }

           

        }
    }
}

using Amazon.Glue;
using Amazon.Lambda.SQSEvents;
using Avro.Generic;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry.Serdes;
using FluentAssertions;
using Hackney.Shared.Tenure.Boundary.Response;
using Moq;
using MtfhReportingDataListener.Gateway;
using MtfhReportingDataListener.Infrastructure.Exceptions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MtfhReportingDataListener.Tests.E2ETests.Steps
{
    public class TenureUpdatedUseCaseSteps : BaseSteps
    {
        public SQSEvent.SQSMessage TheMessage { get; private set; }


        public TenureUpdatedUseCaseSteps()
        {
            _eventType = EventTypes.TenureUpdatedEvent;
        }

        public async Task WhenTheFunctionIsTriggered(Guid id, IAmazonGlue glue)
        {
            await TriggerFunction(id, glue).ConfigureAwait(false);
        }

        public void ThenEntityNotFoundExceptionIsThrown(Guid id)
        {
            _lastException.Should().NotBeNull();
            _lastException.Should().BeOfType(typeof(EntityNotFoundException<TenureResponseObject>));
            (_lastException as EntityNotFoundException<TenureResponseObject>).Id.Should().Be(id);
        }
        public void ThenTheUpdatedDataIsSavedToKafka(SQSEvent.SQSMessage message, string schemaDefinition)
        {
            var consumerconfig = new ConsumerConfig
            {
                BootstrapServers = Environment.GetEnvironmentVariable("DATAPLATFORM_KAFKA_HOSTNAME"),
                GroupId = "4c659d6b-4739-4579-9698-a27d1aaa397d",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            var topic = "mtfh-reporting-data-listener";

            var schemaWithMetadata = new Confluent.SchemaRegistry.Schema("tenure", 1, 1, schemaDefinition);

            var schemaRegistryClient = new SchemaRegistryClient(schemaWithMetadata);

            using (var consumer = new ConsumerBuilder<Ignore, GenericRecord>(consumerconfig)
                .SetValueDeserializer(new AvroDeserializer<GenericRecord>(schemaRegistryClient).AsSyncOverAsync())
                .Build()
            )
            {
                consumer.Subscribe(topic);
                var r = consumer.Consume(TimeSpan.FromSeconds(30));
                Assert.NotNull(r?.Message);
                //Assert.Equal(message, r.Message.Value);
                consumer.Close();
            }
        }


    }
}

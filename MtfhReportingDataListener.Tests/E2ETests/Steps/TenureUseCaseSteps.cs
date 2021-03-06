using Amazon.Lambda.SQSEvents;
using Avro.Generic;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry.Serdes;
using FluentAssertions;
using Hackney.Shared.Tenure.Boundary.Response;
using MtfhReportingDataListener.Infrastructure.Exceptions;
using System;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using Confluent.SchemaRegistry;

namespace MtfhReportingDataListener.Tests.E2ETests.Steps
{
    public class TenureUseCaseSteps : BaseSteps
    {
        public SQSEvent.SQSMessage TheMessage { get; private set; }

        public async Task WhenTheFunctionIsTriggered(Guid id, string eventType)
        {
            TheMessage = await TriggerFunction(id, eventType).ConfigureAwait(false);
        }

        public void ThenEntityNotFoundExceptionIsThrown(Guid id)
        {
            _lastException.Should().NotBeNull();
            _lastException.Should().BeOfType(typeof(EntityNotFoundException<TenureResponseObject>));
            (_lastException as EntityNotFoundException<TenureResponseObject>).Id.Should().Be(id);
        }
        public void ThenTheMessageIsSavedToKafka(string topic, TenureResponseObject tenure)
        {
            var consumerconfig = new ConsumerConfig
            {
                BootstrapServers = Environment.GetEnvironmentVariable("DATAPLATFORM_KAFKA_HOSTNAME"),
                GroupId = "4c659d6b-4739-4579-9698-a27d1aaa397d",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using (var schemaRegistry = new CachedSchemaRegistryClient(new SchemaRegistryConfig
            {
                Url = Environment.GetEnvironmentVariable("KAFKA_SCHEMA_REGISTRY_HOSTNAME")
            }))
            using (var consumer = new ConsumerBuilder<Ignore, GenericRecord>(consumerconfig)
                .SetValueDeserializer(new AvroDeserializer<GenericRecord>(schemaRegistry).AsSyncOverAsync())
                .Build()
            )
            {
                try
                {
                    consumer.Subscribe(topic);
                    var r = consumer.Consume(TimeSpan.FromSeconds(30));
                    Assert.NotNull(r?.Message);
                    var receivedRecord = (GenericRecord) r.Message.Value;
                    CheckRecord(receivedRecord, tenure);
                }
                finally
                {
                    consumer.Close();
                }
            }
        }

        private void CheckRecord(GenericRecord record, TenureResponseObject tenure)
        {
            var receivedRecord = (GenericRecord) record["Tenure"];
            receivedRecord["Id"].Should().Be(tenure.Id.ToString());
            receivedRecord["PaymentReference"].Should().Be(tenure.PaymentReference);
            receivedRecord["SuccessionDate"].Should().Be((int?) (tenure.SuccessionDate?.Subtract(new DateTime(1970, 1, 1)))?.TotalSeconds);


            var tenuredAsset = (GenericRecord) receivedRecord["TenuredAsset"];
            tenuredAsset["Id"].Should().Be(tenure.TenuredAsset.Id.ToString());
            ((GenericEnum) tenuredAsset["Type"]).Value.Should().Be(tenure.TenuredAsset.Type.ToString());
            tenuredAsset["FullAddress"].Should().Be(tenure.TenuredAsset.FullAddress);
            tenuredAsset["Uprn"].Should().Be(tenure.TenuredAsset.Uprn);
            tenuredAsset["PropertyReference"].Should().Be(tenure.TenuredAsset.PropertyReference);

            var receivedMember = (GenericRecord) ((object[]) receivedRecord["HouseholdMembers"])[0];
            var expectedMember = tenure.HouseholdMembers.FirstOrDefault(mem => mem.Id.ToString() == receivedMember["Id"].ToString());
            expectedMember.Should().NotBeNull();
            ((GenericEnum) receivedMember["Type"]).Value.Should().Be(expectedMember.Type.ToString());
            receivedMember["FullName"].Should().Be(expectedMember.FullName);
            receivedMember["IsResponsible"].Should().Be(expectedMember.IsResponsible);
            receivedMember["DateOfBirth"].Should().Be((int) expectedMember.DateOfBirth.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
            ((GenericEnum) receivedMember["PersonTenureType"]).Value.Should().Be(expectedMember.PersonTenureType.ToString());
        }
    }
}

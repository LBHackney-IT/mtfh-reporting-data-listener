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
using Hackney.Shared.ContactDetail.Boundary.Response;

namespace MtfhReportingDataListener.Tests.E2ETests.Steps
{
    public class ContactDetailUseCaseSteps : BaseSteps
    {
        public SQSEvent.SQSMessage TheMessage { get; private set; }

        public async Task WhenTheFunctionIsTriggered(Guid id, string eventType)
        {
            TheMessage = await TriggerFunction(id, eventType).ConfigureAwait(false);
        }

        public void ThenEntityNotFoundExceptionIsThrown(Guid id)
        {
            _lastException.Should().NotBeNull();
            _lastException.Should().BeOfType(typeof(EntityNotFoundException<ContactDetailsResponseObject>));
            (_lastException as EntityNotFoundException<ContactDetailsResponseObject>).Id.Should().Be(id);
        }
        public void ThenTheMessageIsSavedToKafka(string topic, ContactDetailsResponseObject contactDetail)
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
                    CheckRecord(receivedRecord, contactDetail);
                }
                finally
                {
                    consumer.Close();
                }
            }
        }

        private void CheckRecord(GenericRecord record, ContactDetailsResponseObject contactDetail)
        {
            var receivedRecord = (GenericRecord) record["ContactDetails"];
            receivedRecord["Id"].Should().Be(contactDetail.Id.ToString());
            receivedRecord["TargetId"].Should().Be(contactDetail.TargetId.ToString());
            ((GenericEnum) receivedRecord["TargetType"]).Value.Should().Be(contactDetail.TargetType.ToString());

            var sourceServiceArea = (GenericRecord) receivedRecord["SourceServiceArea"];
            sourceServiceArea["Area"].Should().Be(contactDetail.SourceServiceArea.Area);
            sourceServiceArea["IsDefault"].Should().Be(contactDetail.SourceServiceArea.IsDefault);

            receivedRecord["RecordValidUntil"].Should().Be((int?) (contactDetail.RecordValidUntil?.Subtract(new DateTime(1970, 1, 1)))?.TotalSeconds);
            receivedRecord["IsActive"].Should().Be(contactDetail.IsActive);


            var contactInformation = (GenericRecord) receivedRecord["ContactInformation"];
            contactInformation["Value"].Should().Be(contactDetail.ContactInformation.Value);
            ((GenericEnum) contactInformation["ContactType"]).Value.Should().Be(contactDetail.ContactInformation.ContactType.ToString());
            contactInformation["Description"].Should().Be(contactDetail.ContactInformation.Description);
            ((GenericEnum) contactInformation["SubType"]).Value.Should().Be(contactDetail.ContactInformation.SubType.ToString());

            var addressExtended = (GenericRecord) contactInformation["AddressExtended"];
            addressExtended["IsOverseasAddress"].Should().Be(contactDetail.ContactInformation.AddressExtended.IsOverseasAddress);
            addressExtended["OverseasAddress"].Should().Be(contactDetail.ContactInformation.AddressExtended.OverseasAddress);
            addressExtended["UPRN"].Should().Be(contactDetail.ContactInformation.AddressExtended.UPRN);

            var createdBy = (GenericRecord) receivedRecord["createdBy"];
            createdBy["FullName"].Should().Be(contactDetail.CreatedBy.FullName);
            createdBy["EmailAddress"].Should().Be(contactDetail.CreatedBy.EmailAddress);
            createdBy["CreatedAt"].Should().Be((int?) (contactDetail.CreatedBy.CreatedAt?.Subtract(new DateTime(1970, 1, 1)))?.TotalSeconds);


        }
    }
}

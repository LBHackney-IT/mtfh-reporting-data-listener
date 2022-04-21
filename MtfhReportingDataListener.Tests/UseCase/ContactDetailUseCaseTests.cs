using AutoFixture;
using MtfhReportingDataListener.Boundary;
using MtfhReportingDataListener.Gateway.Interfaces;
using MtfhReportingDataListener.Infrastructure.Exceptions;
using MtfhReportingDataListener.UseCase;
using MtfhReportingDataListener.Domain;
using FluentAssertions;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using System.Text.Json;
using Avro.Generic;
using Hackney.Shared.ContactDetail.Boundary.Response;

namespace MtfhReportingDataListener.Tests.UseCase
{
    [Collection("LogCall collection")]
    public class ContactDetailUseCaseTests : IDisposable
    {
        private readonly Mock<IContactDetailApiGateway> _mockGateway;
        private readonly Mock<IKafkaGateway> _mockKafka;
        private readonly Mock<ISchemaRegistry> _mockSchemaRegistry;
        private readonly ContactDetailUseCase _sut;
        private readonly ContactDetailsResponseObject _contactDetail;

        private readonly EntityEventSns _contactDetailAddedMessage;

        private readonly Fixture _fixture;

        private readonly string _contactDetailSchemaName;

        public ContactDetailUseCaseTests()
        {
            _fixture = new Fixture();

            _mockGateway = new Mock<IContactDetailApiGateway>();
            _mockKafka = new Mock<IKafkaGateway>();
            _mockSchemaRegistry = new Mock<ISchemaRegistry>();
            _sut = new ContactDetailUseCase(_mockGateway.Object, _mockKafka.Object, _mockSchemaRegistry.Object);


            _contactDetail = CreateContactDetail();
            _contactDetailAddedMessage = CreateContactDetailAddedMessage();

            _contactDetailSchemaName = Environment.GetEnvironmentVariable("CONTACT_DETAIL_SCHEMA_NAME");
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("CONTACT_DETAIL_SCHEMA_NAME", _contactDetailSchemaName);
        }


        [Fact]
        public void ProcessMessageAsyncTestNullMessageThrows()
        {
            Func<Task> func = async () => await _sut.ProcessMessageAsync(null).ConfigureAwait(false);
            func.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public void ProcessMessageAsyncTestEntityIdNotFoundThrows()
        {
            _mockGateway.Setup(x => x.GetContactDetailByTargetIdAsync(_contactDetailAddedMessage.EntityId, _contactDetailAddedMessage.CorrelationId)).ReturnsAsync((ContactDetailsResponseObject) null);
            Func<Task> func = async () => await _sut.ProcessMessageAsync(null).ConfigureAwait(false);
            func.Should().ThrowAsync<EntityNotFoundException<ContactDetailsResponseObject>>();
        }

        [Fact]
        public void ProcessMessageAsyncTestSaveEntityThrows()
        {
            var exMsg = "This is the last error";
            var jsonTenure = JsonSerializer.Serialize(_contactDetailAddedMessage);
            _mockGateway.Setup(x => x.GetContactDetailByTargetIdAsync(_contactDetailAddedMessage.EntityId, _contactDetailAddedMessage.CorrelationId))
                        .ThrowsAsync(new Exception(exMsg));

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_contactDetailAddedMessage).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);
            _mockGateway.Verify(x => x.GetContactDetailByTargetIdAsync(_contactDetailAddedMessage.EntityId, _contactDetailAddedMessage.CorrelationId), Times.Once);
        }


        [Fact]
        public async Task ProcessMessageAsyncGetsTheSchema()
        {
            _mockGateway.Setup(x => x.GetContactDetailByTargetIdAsync(_contactDetailAddedMessage.EntityId, _contactDetailAddedMessage.CorrelationId))
                        .ReturnsAsync(_contactDetail);
            var mockSchemaResponse = @"{
                ""type"": ""record"",
                ""name"": ""Person"",
                ""fields"": [
                   {
                     ""name"": ""Id"",
                     ""type"": ""string""
                   },
                ]
                }";

            var schemaName = _fixture.Create<string>();
            Environment.SetEnvironmentVariable("CONTACT_DETAIL_SCHEMA_NAME", schemaName);

            _mockSchemaRegistry.Setup(x => x.GetSchemaForTopic(schemaName)).ReturnsAsync(mockSchemaResponse).Verifiable();

            await _sut.ProcessMessageAsync(_contactDetailAddedMessage).ConfigureAwait(false);
            _mockSchemaRegistry.Verify();
        }

        [Fact]
        public async Task CallsKafkaGatewayToCreateTopic()
        {
            _mockGateway.Setup(x => x.GetContactDetailByTargetIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(_contactDetail);

            var schemaResponse = @"{
                ""type"": ""record"",
                ""name"": ""Person"",
                ""fields"": [
                    {
                        ""name"": ""Id"",
                        ""type"": ""string""
                    },
                ]
            }";

            _mockSchemaRegistry.Setup(x => x.GetSchemaForTopic(It.IsAny<string>())).ReturnsAsync(schemaResponse);
            var schemaName = "mtfh-reporting-data-listener";

            Environment.SetEnvironmentVariable("CONTACT_DETAIL_SCHEMA_NAME", schemaName);

            var message = new EntityEventSns();

            await _sut.ProcessMessageAsync(message);

            _mockKafka.Verify(x => x.CreateKafkaTopic(schemaName), Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsyncSendsDataToKafkaWithContactDetailAddedEventType()
        {
            _mockGateway.Setup(x => x.GetContactDetailByTargetIdAsync(_contactDetailAddedMessage.EntityId, _contactDetailAddedMessage.CorrelationId))
                        .ReturnsAsync(_contactDetail);
            var mockSchemaResponse = @"{
                ""type"": ""record"",
                ""name"": ""ContactDetail"",
                ""fields"": [
                   {
                     ""name"": ""Id"",
                     ""type"": ""string""
                   },
                ]
                }";
            _mockSchemaRegistry.Setup(x => x.GetSchemaForTopic(It.IsAny<string>())).ReturnsAsync(mockSchemaResponse);
            var schemaName = "mtfh-reporting-data-listener";

            Environment.SetEnvironmentVariable("CONTACT_DETAIL_SCHEMA_NAME", schemaName);

            await _sut.ProcessMessageAsync(_contactDetailAddedMessage).ConfigureAwait(false);
            _mockKafka.Verify(x => x.SendDataToKafka(schemaName, It.IsAny<GenericRecord>()), Times.Once);
        }

        [Fact]
        public void BuildContactDetailRecordCanSetOneStringValueToAGenericRecord()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""ContactDetail"",
                ""fields"": [
                   {
                     ""name"": ""TargetId"",
                     ""type"": ""string"",
                     ""logicalType"": ""uuid""
                   }
                ]
            }";

            var receivedRecord = ExecuteBuildContactDetailRecord(schema, _contactDetail);

            Assert.Equal(_contactDetail.TargetId.ToString(), receivedRecord["TargetId"]);
        }

        [Fact]
        public void BuildContactDetailCanConvertDatesToUnixTimestamps()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""ContactDetail"",
                ""fields"": [
                   {
                     ""name"": ""Id"",
                     ""type"": ""string"",
                     ""logicalType"": ""uuid""
                   },
                   {
                     ""name"": ""RecordValidUntil"",
                     ""type"": [""null"", ""int""]
                   },
                ]
            }";

            var contactDetail = _contactDetail;
            contactDetail.RecordValidUntil = new DateTime(1970, 01, 02);

            var receivedRecord = ExecuteBuildContactDetailRecord(schema, contactDetail);

            Assert.Equal(86400, receivedRecord["RecordValidUntil"]);
        }

        [Fact]
        public void BuildContactDetailRecordCanSetBooleanTypeValuesToAGenericRecord()
        {
            var schema = @$"{{
                ""type"": ""record"",
                ""name"": ""ContactDetail"",
                ""fields"": [
                   {{
                     ""name"": ""IsActive"",
                     ""type"": ""boolean""
                   }}
                ]
            }}";

            var receivedRecord = ExecuteBuildContactDetailRecord(schema, _contactDetail);

            Assert.Equal(_contactDetail.IsActive, receivedRecord["IsActive"]);
        }

        [Fact]
        public void BuildContactDetailRecordCanSetNestedFields()
        {
            var schema = @"{
                    ""type"": ""record"",
                    ""name"": ""ContactDetail"",
                    ""fields"": [
                       {
                         ""name"": ""ContactInformation"",
                         ""type"": {
                            ""type"": ""record"",
                            ""name"": ""charge"",
                            ""fields"": [
                            {
                                ""name"": ""Value"",
                                ""type"": ""string""
                            }]
                            }
                        }
                    ]
                }";

            var receivedRecord = ExecuteBuildContactDetailRecord(schema, _contactDetail);
            var receivedContactInformation = (GenericRecord) receivedRecord["ContactInformation"];

            Assert.Equal(_contactDetail.ContactInformation.Value, receivedContactInformation["Value"]);
        }

        [Fact]
        public void BuildContactDetailCanAssignEnums()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""ContactDetails"",
                ""fields"": [
                   {
                     ""name"": ""TargetType"",
                     ""type"": ""enum"",
                     ""symbols"": [
                         ""Person"",
                         ""Organisation""
                     ]
                   }
                ]
            }";

            var contactDetail = _contactDetail;
            var receivedRecord = ExecuteBuildContactDetailRecord(schema, contactDetail);
            ((GenericEnum) receivedRecord["TargetType"]).Value.Should().Be(_contactDetail.TargetType.ToString());
        }

        [Fact]
        public void BuildContactDetailCanHandleNestedObjects()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""ContactDetail"",
                ""fields"": [
                   {
                     ""name"": ""SourceServiceArea"",
                     ""type"": {
                        ""type"": ""record"",
                        ""name"": ""SourceServiceArea"",
                        ""fields"": [
                            {
                               ""name"": ""Area"",
                               ""type"": ""string""
                            },
                            {
                              ""name"": ""IsDefault"",
                              ""type"": ""boolean""
                            }
                      ]}
                  }
                ]
            }";

            var receivedRecord = ExecuteBuildContactDetailRecord(schema, _contactDetail);
            receivedRecord["SourceServiceArea"].Should().BeOfType<GenericRecord>();

            ((GenericRecord) receivedRecord["SourceServiceArea"])["Area"].Should().Be(_contactDetail.SourceServiceArea.Area);
            ((GenericRecord) receivedRecord["SourceServiceArea"])["IsDefault"].Should().Be(_contactDetail.SourceServiceArea.IsDefault);

        }

        [Fact]
        public void LogsOutSchemaFieldNameWhenItDoesNotExistInContactDetail()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""ContactDetail"",
                ""fields"": [
                   {
                     ""name"": ""FieldNameNotInContactDetail"",
                     ""type"": ""string"",
                   },
                ]
            }";

            Func<GenericRecord> receivedRecord = () => ExecuteBuildContactDetailRecord(schema, _contactDetail);

            receivedRecord.Should().NotThrow<NullReferenceException>();
        }

        private GenericRecord ExecuteBuildContactDetailRecord(string contactDetailSchema, ContactDetailsResponseObject contactDetail)
        {
            var schema = @$"{{
                ""type"": ""record"",
                ""name"": ""ContactDetailAPIAddedEvent"",
                ""namespace"": ""MMH"",
                ""fields"": [
                    {{
                        ""name"": ""Id"",
                        ""type"": ""string"",
                        ""logicalType"": ""uuid""
                    }},
                    {{
                        ""name"": ""EventType"",
                        ""type"": ""string""
                    }},
                    {{
                        ""name"": ""SourceDomain"",
                        ""type"": ""string""
                    }},
                    {{
                        ""name"": ""SourceSystem"",
                        ""type"": ""string""
                    }},
                    {{
                        ""name"": ""Version"",
                        ""type"": ""string""
                    }},
                    {{
                        ""name"": ""CorrelationId"",
                        ""type"": ""string"",
                        ""logicalType"": ""uuid""
                    }},
                    {{
                        ""name"": ""DateTime"",
                        ""type"": ""int"",
                        ""logicalType"": ""date""
                    }},
                    {{
                        ""name"": ""User"",
                        ""type"": {{
                            ""type"": ""record"",
                            ""name"": ""User"",
                            ""fields"": [
                                {{
                                    ""name"": ""Name"",
                                    ""type"": ""string""
                                }},
                                {{
                                    ""name"": ""Email"",
                                    ""type"": ""string""
                                }}
                            ]
                        }}
                    }},
                    {{
                        ""name"": ""ContactDetails"",
                        ""type"": {contactDetailSchema}
                    }}
                ]
            }}";
            var contactDetailAddedEvent = _fixture.Create<ContactDetailEvent>();
            contactDetailAddedEvent.ContactDetails = _contactDetail;
            var record = _sut.BuildContactDetailRecord(schema, contactDetailAddedEvent);
            return (GenericRecord) record["ContactDetails"];
        }

        private EntityEventSns CreateContactDetailAddedMessage(string eventType = EventTypes.ContactDetailAddedEvent)
        {
            return _fixture.Build<EntityEventSns>()
                           .With(x => x.EventType, eventType)
                           .Create();
        }

        private ContactDetailsResponseObject CreateContactDetail()
        {
            return _fixture.Create<ContactDetailsResponseObject>();
        }

    }
}

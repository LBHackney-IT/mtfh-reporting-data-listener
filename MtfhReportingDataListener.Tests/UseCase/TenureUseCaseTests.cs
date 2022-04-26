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
using Hackney.Shared.Tenure.Boundary.Response;
using Hackney.Shared.Tenure.Domain;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Avro.Generic;
using MtfhReportingDataListener.Helper;

namespace MtfhReportingDataListener.Tests.UseCase
{
    [Collection("LogCall collection")]
    public class TenureUseCaseTests : IDisposable
    {
        private readonly Mock<ITenureInfoApiGateway> _mockGateway;
        private readonly Mock<IKafkaGateway> _mockKafka;
        private readonly Mock<ISchemaRegistry> _mockSchemaRegistry;
        private readonly Mock<IConvertToAvroHelper> _mockConvertToAvroHelper;
        private readonly TenureUseCase _sut;
        private readonly TenureResponseObject _tenure;

        private readonly EntityEventSns _tenureUpdatedMessage;
        private readonly EntityEventSns _tenureCreatedMessage;

        private readonly Fixture _fixture;

        private readonly string _tenureSchemaName;

        public TenureUseCaseTests()
        {
            _fixture = new Fixture();

            _mockGateway = new Mock<ITenureInfoApiGateway>();
            _mockKafka = new Mock<IKafkaGateway>();
            _mockSchemaRegistry = new Mock<ISchemaRegistry>();
            _mockConvertToAvroHelper = new Mock<IConvertToAvroHelper>();
            _sut = new TenureUseCase(_mockGateway.Object, _mockKafka.Object, _mockSchemaRegistry.Object, _mockConvertToAvroHelper.Object);


            _tenure = CreateTenure();
            _tenureUpdatedMessage = CreateTenureUpdatedMessage();
            _tenureCreatedMessage = CreateTenureCreatedMessage();

            _tenureSchemaName = Environment.GetEnvironmentVariable("TENURE_SCHEMA_NAME");
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("TENURE_SCHEMA_NAME", _tenureSchemaName);
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
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_tenureCreatedMessage.EntityId, _tenureCreatedMessage.CorrelationId)).ReturnsAsync((TenureResponseObject) null);
            Func<Task> func = async () => await _sut.ProcessMessageAsync(null).ConfigureAwait(false);
            func.Should().ThrowAsync<EntityNotFoundException<TenureResponseObject>>();
        }

        [Fact]
        public void ProcessMessageAsyncTestSaveEntityThrows()
        {
            var exMsg = "This is the last error";
            var jsonTenure = JsonSerializer.Serialize(_tenureCreatedMessage);
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_tenureCreatedMessage.EntityId, _tenureCreatedMessage.CorrelationId))
                        .ThrowsAsync(new Exception(exMsg));

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_tenureCreatedMessage).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);
            _mockGateway.Verify(x => x.GetTenureInfoByIdAsync(_tenureCreatedMessage.EntityId, _tenureCreatedMessage.CorrelationId), Times.Once);
        }


        [Fact]
        public async Task ProcessMessageAsyncGetsTheSchema()
        {
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_tenureCreatedMessage.EntityId, _tenureCreatedMessage.CorrelationId))
                        .ReturnsAsync(_tenure);
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
            Environment.SetEnvironmentVariable("TENURE_SCHEMA_NAME", schemaName);

            _mockSchemaRegistry.Setup(x => x.GetSchemaForTopic(schemaName)).ReturnsAsync(mockSchemaResponse).Verifiable();

            await _sut.ProcessMessageAsync(_tenureCreatedMessage).ConfigureAwait(false);
            _mockSchemaRegistry.Verify();
        }

        [Fact]
        public async Task CallsKafkaGatewayToCreateTopic()
        {
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(_tenure);

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

            Environment.SetEnvironmentVariable("TENURE_SCHEMA_NAME", schemaName);

            var message = new EntityEventSns();

            await _sut.ProcessMessageAsync(message);

            _mockKafka.Verify(x => x.CreateKafkaTopic(schemaName), Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsyncSendsDataToKafkaWithTenureCreatedEventType()
        {
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_tenureCreatedMessage.EntityId, _tenureCreatedMessage.CorrelationId))
                        .ReturnsAsync(_tenure);
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
            _mockSchemaRegistry.Setup(x => x.GetSchemaForTopic(It.IsAny<string>())).ReturnsAsync(mockSchemaResponse);
            var schemaName = "mtfh-reporting-data-listener";

            Environment.SetEnvironmentVariable("TENURE_SCHEMA_NAME", schemaName);

            await _sut.ProcessMessageAsync(_tenureCreatedMessage).ConfigureAwait(false);
            _mockKafka.Verify(x => x.SendDataToKafka(schemaName, It.IsAny<GenericRecord>()), Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsyncSendsDataToKafkaWithTenureUpdatedEventType()
        {
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_tenureUpdatedMessage.EntityId, _tenureUpdatedMessage.CorrelationId))
                        .ReturnsAsync(_tenure);
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
            _mockSchemaRegistry.Setup(x => x.GetSchemaForTopic(It.IsAny<string>())).ReturnsAsync(mockSchemaResponse);
            var schemaName = "mtfh-reporting-data-listener";

            Environment.SetEnvironmentVariable("TENURE_SCHEMA_NAME", schemaName);

            await _sut.ProcessMessageAsync(_tenureUpdatedMessage).ConfigureAwait(false);
            _mockKafka.Verify(x => x.SendDataToKafka(schemaName, It.IsAny<GenericRecord>()), Times.Once);
        }

        //[Fact]
        //public void BuildTenureRecordCanSetOneStringValueToAGenericRecord()
        //{
        //    var schema = @"{
        //        ""type"": ""record"",
        //        ""name"": ""TenureInformation"",
        //        ""fields"": [
        //           {
        //             ""name"": ""Id"",
        //             ""type"": ""string"",
        //             ""logicalType"": ""uuid""
        //           }
        //        ]
        //    }";

        //    var receivedRecord = ExecuteBuildTenureRecord(schema, _tenure);

        //    Assert.Equal(_tenure.Id.ToString(), receivedRecord["Id"]);
        //}

        //[Fact]
        //public void BuildTenureRecordCanSetMultipleStringsToAGenericRecord()
        //{
        //    var schema = @"{
        //        ""type"": ""record"",
        //        ""name"": ""TenureInformation"",
        //        ""fields"": [
        //           {
        //             ""name"": ""Id"",
        //             ""type"": ""string"",
        //             ""logicalType"": ""uuid""
        //           },
        //           {
        //             ""name"": ""PaymentReference"",
        //             ""type"": ""string""
        //           },
        //        ]
        //    }";

        //    var receivedRecord = ExecuteBuildTenureRecord(schema, _tenure);

        //    Assert.Equal(_tenure.Id.ToString(), receivedRecord["Id"]);
        //    Assert.Equal(_tenure.PaymentReference, receivedRecord["PaymentReference"]);
        //}

        //[Fact]
        //public void BuildTenureRecordCanConvertDatesToUnixTimestamps()
        //{
        //    var schema = @"{
        //        ""type"": ""record"",
        //        ""name"": ""TenureInformation"",
        //        ""fields"": [
        //           {
        //             ""name"": ""Id"",
        //             ""type"": ""string"",
        //             ""logicalType"": ""uuid""
        //           },
        //           {
        //             ""name"": ""SuccessionDate"",
        //             ""type"": [""null"", ""int""]
        //           },
        //        ]
        //    }";

        //    var tenure = _tenure;
        //    tenure.SuccessionDate = new DateTime(1970, 01, 02);

        //    var receivedRecord = ExecuteBuildTenureRecord(schema, tenure);

        //    Assert.Equal(86400, receivedRecord["SuccessionDate"]);
        //}

        //[Theory]
        //[InlineData("IsTenanted")]
        //public void BuildTenureRecordCanSetBooleanTypeValuesToAGenericRecord(string nullableBoolFieldName)
        //{
        //    var schema = @$"{{
        //        ""type"": ""record"",
        //        ""name"": ""TenureInformation"",
        //        ""fields"": [
        //           {{
        //             ""name"": ""IsActive"",
        //             ""type"": ""boolean""
        //           }},
        //           {{
        //             ""name"": ""{nullableBoolFieldName}"",
        //             ""type"": [""boolean"", ""null""]
        //           }}
        //        ]
        //    }}";

        //    var fieldValue = GetFieldValueFromStringName<bool>(nullableBoolFieldName, _tenure);

        //    var receivedRecord = ExecuteBuildTenureRecord(schema, _tenure);

        //    Assert.Equal(_tenure.IsActive, receivedRecord["IsActive"]);
        //    Assert.Equal(fieldValue, receivedRecord[nullableBoolFieldName]);
        //}

        //[Fact]
        //public void BuildTenureRecordCanSetNestedFields()
        //{
        //    var schema = @"{
        //            ""type"": ""record"",
        //            ""name"": ""TenureInformation"",
        //            ""fields"": [
        //               {
        //                 ""name"": ""TenureType"",
        //                 ""type"": {
        //                    ""type"": ""record"",
        //                    ""name"": ""charge"",
        //                    ""fields"": [
        //                    {
        //                        ""name"": ""Code"",
        //                        ""type"": ""string""
        //                    }]
        //                    }
        //                }
        //            ]
        //        }";

        //    var receivedRecord = ExecuteBuildTenureRecord(schema, _tenure);
        //    var receivedTenureType = (GenericRecord) receivedRecord["TenureType"];

        //    Assert.Equal(_tenure.TenureType.Code, receivedTenureType["Code"]);
        //}

        //[Fact]
        //public void BuildTenureRecordCanSetLists()
        //{
        //    var schema = @"{
        //        ""type"": ""record"",
        //        ""name"": ""TenureInformation"",
        //        ""fields"": [
        //            {
        //                ""name"": ""HouseholdMembers"",
        //                ""type"": {
        //                    ""type"": ""array"",
        //                    ""items"": {
        //                        ""name"": ""HouseholdMember"",
        //                        ""type"": ""record"",
        //                        ""fields"": [
        //                            {
        //                                ""name"": ""Id"",
        //                                ""type"": ""string"",
        //                                ""logicalType"": ""uuid""
        //                            }
        //                        ]
        //                    }
        //                }
        //            }
        //        ]
        //    }";

        //    var tenure = _tenure;
        //    tenure.HouseholdMembers = new List<HouseholdMembers> { tenure.HouseholdMembers.First() };

        //    var receivedRecord = ExecuteBuildTenureRecord(schema, tenure);
        //    receivedRecord["HouseholdMembers"].Should().BeOfType<GenericRecord[]>();

        //    var firstRecord = ((GenericRecord[]) receivedRecord["HouseholdMembers"]).ToList().First();
        //    firstRecord["Id"].Should().Be(tenure.HouseholdMembers.First().Id.ToString());

        //}

        //[Fact]
        //public void BuildTenureRecordCanAssignEnums()
        //{
        //    var schema = @"{
        //        ""type"": ""record"",
        //        ""name"": ""TenureInformation"",
        //        ""fields"": [
        //            {
        //                ""name"": ""HouseholdMembers"",
        //                ""type"": {
        //                    ""type"": ""array"",
        //                    ""items"": {
        //                        ""name"": ""HouseholdMember"",
        //                        ""type"": ""record"",
        //                        ""fields"": [
        //                            {
        //                                ""name"": ""Id"",
        //                                ""type"": ""string"",
        //                                ""logicalType"": ""uuid""
        //                            },
        //                            {
        //                                ""name"": ""Type"",
        //                                ""type"": {
        //                                    ""name"": ""HouseholdMembersType"",
        //                                    ""type"": ""enum"",
        //                                    ""symbols"": [
        //                                        ""Person"",
        //                                        ""Organisation""
        //                                    ]
        //                                }
        //                            }
        //                        ]
        //                    }
        //                }
        //            }
        //        ]
        //    }";

        //    var tenure = _tenure;
        //    tenure.HouseholdMembers = new List<HouseholdMembers> { tenure.HouseholdMembers.First() };
        //    var receivedRecord = ExecuteBuildTenureRecord(schema, tenure);
        //    var receivedHouseholdMember = ((GenericRecord[]) receivedRecord["HouseholdMembers"])[0];

        //    receivedHouseholdMember["Id"].Should().Be(_tenure.HouseholdMembers.First().Id.ToString());
        //    ((GenericEnum) receivedHouseholdMember["Type"]).Value.Should().Be(_tenure.HouseholdMembers.First().Type.ToString());
        //}

        //[Fact]
        //public void BuildTenureCanHandleNestedObjects()
        //{
        //    var schema = @"{
        //        ""type"": ""record"",
        //        ""name"": ""TenureInformation"",
        //        ""fields"": [
        //            {
        //            ""name"": ""TenuredAsset"",
        //            ""type"": {
        //                ""type"": ""record"",
        //                ""name"": ""TenuredAsset"",
        //                ""fields"": [
        //                {
        //                    ""name"": ""Id"",
        //                    ""type"": ""string"",
        //                    ""logicalType"": ""uuid""
        //                },
        //                {
        //                    ""name"": ""Type"",
        //                    ""type"": [{
        //                    ""name"": ""TenuredAssetType"",
        //                    ""type"": ""enum"",
        //                    ""symbols"": [
        //                        ""Block"",
        //                        ""Concierge"",
        //                        ""Dwelling"",
        //                        ""LettableNonDwelling"",
        //                        ""MediumRiseBlock"",
        //                        ""NA"",
        //                        ""TravellerSite""
        //                    ]
        //                    }, ""null""]
        //                },
        //                ]
        //            }
        //        }
        //        ]
        //    }";

        //    var receivedRecord = ExecuteBuildTenureRecord(schema, _tenure);
        //    var receivedRecordEnum = (GenericEnum) ((GenericRecord) receivedRecord["TenuredAsset"])["Type"];
        //    receivedRecord["TenuredAsset"].Should().BeOfType<GenericRecord>();

        //    ((GenericRecord) receivedRecord["TenuredAsset"])["Id"].Should().Be(_tenure.TenuredAsset.Id.ToString());
        //    receivedRecordEnum.Value.Should().Be(_tenure.TenuredAsset.Type.ToString());

        //}

        //[Fact]
        //public void LogsOutSchemaFieldNameWhenItDoesNotExistInTenure()
        //{
        //    var schema = @"{
        //        ""type"": ""record"",
        //        ""name"": ""TenureInformation"",
        //        ""fields"": [
        //           {
        //             ""name"": ""FieldNameNotInTenure"",
        //             ""type"": ""string"",
        //           },
        //        ]
        //    }";

        //    Func<GenericRecord> receivedRecord = () => ExecuteBuildTenureRecord(schema, _tenure);

        //    receivedRecord.Should().NotThrow<NullReferenceException>();
        //}

        //private T GetFieldValueFromStringName<T>(string fieldName, TenureResponseObject tenure)
        //{
        //    return (T) typeof(TenureResponseObject).GetProperty(fieldName).GetValue(tenure);
        //}

        //private GenericRecord ExecuteBuildTenureRecord(string tenureSchema, TenureResponseObject tenure)
        //{
        //    var schema = @$"{{
        //        ""type"": ""record"",
        //        ""name"": ""TenureAPIChangeEvent"",
        //        ""namespace"": ""MMH"",
        //        ""fields"": [
        //            {{
        //                ""name"": ""Id"",
        //                ""type"": ""string"",
        //                ""logicalType"": ""uuid""
        //            }},
        //            {{
        //                ""name"": ""EventType"",
        //                ""type"": ""string""
        //            }},
        //            {{
        //                ""name"": ""SourceDomain"",
        //                ""type"": ""string""
        //            }},
        //            {{
        //                ""name"": ""SourceSystem"",
        //                ""type"": ""string""
        //            }},
        //            {{
        //                ""name"": ""Version"",
        //                ""type"": ""string""
        //            }},
        //            {{
        //                ""name"": ""CorrelationId"",
        //                ""type"": ""string"",
        //                ""logicalType"": ""uuid""
        //            }},
        //            {{
        //                ""name"": ""DateTime"",
        //                ""type"": ""int"",
        //                ""logicalType"": ""date""
        //            }},
        //            {{
        //                ""name"": ""User"",
        //                ""type"": {{
        //                    ""type"": ""record"",
        //                    ""name"": ""User"",
        //                    ""fields"": [
        //                        {{
        //                            ""name"": ""Name"",
        //                            ""type"": ""string""
        //                        }},
        //                        {{
        //                            ""name"": ""Email"",
        //                            ""type"": ""string""
        //                        }}
        //                    ]
        //                }}
        //            }},
        //            {{
        //                ""name"": ""Tenure"",
        //                ""type"": {tenureSchema}
        //            }}
        //        ]
        //    }}";
        //    var tenureChangeEvent = _fixture.Create<TenureEvent>();
        //    tenureChangeEvent.Tenure = _tenure;
        //    var record = _sut.BuildTenureRecord(schema, tenureChangeEvent);
        //    return (GenericRecord) record["Tenure"];
        //}


        private EntityEventSns CreateTenureUpdatedMessage(string eventType = EventTypes.TenureUpdatedEvent)
        {
            return _fixture.Build<EntityEventSns>()
                           .With(x => x.EventType, eventType)
                           .Create();
        }
        private EntityEventSns CreateTenureCreatedMessage(string eventType = EventTypes.TenureCreatedEvent)
        {
            return _fixture.Build<EntityEventSns>()
                           .With(x => x.EventType, eventType)
                           .Create();
        }

        private TenureResponseObject CreateTenure()
        {
            return _fixture.Build<TenureResponseObject>()
                           .With(x => x.HouseholdMembers, _fixture.Build<HouseholdMembers>()
                                                                  .With(x => x.PersonTenureType, PersonTenureType.Tenant)
                                                                  .CreateMany(3)
                                                                  .ToList())
                           .Create();
        }

    }
}

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
using Schema = Confluent.SchemaRegistry.Schema;

namespace MtfhReportingDataListener.Tests.UseCase
{
    [Collection("LogCall collection")]
    public class TenureUpdatedUseCaseTests
    {
        private readonly Mock<ITenureInfoApiGateway> _mockGateway;
        private readonly Mock<IKafkaGateway> _mockKafka;
        private readonly Mock<IGlueGateway> _mockGlue;
        private readonly TenureUpdatedUseCase _sut;
        private readonly TenureResponseObject _tenure;

        private readonly EntityEventSns _message;

        private readonly Fixture _fixture;

        public TenureUpdatedUseCaseTests()
        {
            _fixture = new Fixture();

            _mockGateway = new Mock<ITenureInfoApiGateway>();
            _mockKafka = new Mock<IKafkaGateway>();
            _mockGlue = new Mock<IGlueGateway>();
            _sut = new TenureUpdatedUseCase(_mockGateway.Object, _mockKafka.Object, _mockGlue.Object);


            _tenure = CreateTenure();
            _message = CreateMessage();

        }

        private EntityEventSns CreateMessage(string eventType = EventTypes.TenureUpdatedEvent)
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

        [Fact]
        public void ProcessMessageAsyncTestNullMessageThrows()
        {
            Func<Task> func = async () => await _sut.ProcessMessageAsync(null).ConfigureAwait(false);
            func.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public void ProcessMessageAsyncTestEntityIdNotFoundThrows()
        {
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId)).ReturnsAsync((TenureResponseObject) null);
            Func<Task> func = async () => await _sut.ProcessMessageAsync(null).ConfigureAwait(false);
            func.Should().ThrowAsync<EntityNotFoundException<TenureResponseObject>>();
        }

        [Fact]
        public void ProcessMessageAsyncTestSaveEntityThrows()
        {
            var exMsg = "This is the last error";
            var jsonTenure = JsonSerializer.Serialize(_message);
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                        .ThrowsAsync(new Exception(exMsg));

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);
            _mockGateway.Verify(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId), Times.Once);
        }


        [Fact]
        public async Task ProcessMessageAsyncGetsTheSchemaFromGlue()
        {
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                        .ReturnsAsync(_tenure);
            var mockSchemaResponse = new SchemaResponse
            {
                Schema = @"{
                ""type"": ""record"",
                ""name"": ""Person"",
                ""fields"": [
                   {
                     ""name"": ""Id"",
                     ""type"": ""string""
                   },
                ]
                }"
            };

            var schemaArn = "arn:aws:glue:blah";
            Environment.SetEnvironmentVariable("SCHEMA_ARN", schemaArn);

            _mockGlue.Setup(x => x.GetSchema(schemaArn)).ReturnsAsync(mockSchemaResponse).Verifiable();

            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            _mockGlue.Verify();
        }

        [Fact]
        public async Task ProcessMessageAsyncSendsDataToKafka()
        {
            _mockGateway.Setup(x => x.GetTenureInfoByIdAsync(_message.EntityId, _message.CorrelationId))
                        .ReturnsAsync(_tenure);
            var mockSchemaResponse = new SchemaResponse
            {
                Schema = @"{
                ""type"": ""record"",
                ""name"": ""Person"",
                ""fields"": [
                   {
                     ""name"": ""Id"",
                     ""type"": ""string""
                   },
                ]
                }"
            };
            _mockGlue.Setup(x => x.GetSchema(It.IsAny<string>())).ReturnsAsync(mockSchemaResponse);

            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            _mockKafka.Verify(x => x.SendDataToKafka("mtfh-reporting-data-listener", It.IsAny<GenericRecord>(), It.IsAny<Schema>()), Times.Once);
        }

        [Fact]
        public void BuildTenureRecordCanSetOneStringValueToAGenericRecord()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""TenureInformation"",
                ""fields"": [
                   {
                     ""name"": ""Id"",
                     ""type"": ""string"",
                     ""logicalType"": ""uuid""
                   }
                ]
            }";

            var receivedRecord = ExecuteBuildTenureRecord(schema, _tenure);

            Assert.Equal(_tenure.Id.ToString(), receivedRecord["Id"]);
        }

        [Fact]
        public void BuildTenureRecordCanSetMultipleStringsToAGenericRecord()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""TenureInformation"",
                ""fields"": [
                   {
                     ""name"": ""Id"",
                     ""type"": ""string"",
                     ""logicalType"": ""uuid""
                   },
                   {
                     ""name"": ""PaymentReference"",
                     ""type"": ""string""
                   },
                ]
            }";

            var receivedRecord = ExecuteBuildTenureRecord(schema, _tenure);

            Assert.Equal(_tenure.Id.ToString(), receivedRecord["Id"]);
            Assert.Equal(_tenure.PaymentReference, receivedRecord["PaymentReference"]);
        }

        [Fact]
        public void BuildTenureRecordCanConvertDatesToUnixTimestamps()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""TenureInformation"",
                ""fields"": [
                   {
                     ""name"": ""Id"",
                     ""type"": ""string"",
                     ""logicalType"": ""uuid""
                   },
                   {
                     ""name"": ""SuccessionDate"",
                     ""type"": [""null"", ""int""]
                   },
                ]
            }";

            var tenure = _tenure;
            tenure.SuccessionDate = new DateTime(1970, 01, 02);

            var receivedRecord = ExecuteBuildTenureRecord(schema, tenure);

            Assert.Equal(86400, receivedRecord["SuccessionDate"]);
        }

        [Theory]
        [InlineData("IsTenanted")]
        public void BuildTenureRecordCanSetBooleanTypeValuesToAGenericRecord(string nullableBoolFieldName)
        {
            var schema = @$"{{
                ""type"": ""record"",
                ""name"": ""TenureInformation"",
                ""fields"": [
                   {{
                     ""name"": ""IsActive"",
                     ""type"": ""boolean""
                   }},
                   {{
                     ""name"": ""{nullableBoolFieldName}"",
                     ""type"": [""boolean"", ""null""]
                   }}
                ]
            }}";

            var fieldValue = GetFieldValueFromStringName<bool>(nullableBoolFieldName, _tenure);

            var receivedRecord = ExecuteBuildTenureRecord(schema, _tenure);

            Assert.Equal(_tenure.IsActive, receivedRecord["IsActive"]);
            Assert.Equal(fieldValue, receivedRecord[nullableBoolFieldName]);
        }

        [Fact]
        public void BuildTenureRecordCanSetNestedFields()
        {
            var schema = @"{
                    ""type"": ""record"",
                    ""name"": ""TenureInformation"",
                    ""fields"": [
                       {
                         ""name"": ""TenureType"",
                         ""type"": {
                            ""type"": ""record"",
                            ""name"": ""charge"",
                            ""fields"": [
                            {
                                ""name"": ""Code"",
                                ""type"": ""string""
                            }]
                            }
                        }
                    ]
                }";

            var receivedRecord = ExecuteBuildTenureRecord(schema, _tenure);
            var receivedTenureType = (GenericRecord) receivedRecord["TenureType"];

            Assert.Equal(_tenure.TenureType.Code, receivedTenureType["Code"]);
        }

        [Fact]
        public void BuildTenureRecordCanSetLists()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""TenureInformation"",
                ""fields"": [
                    {
                        ""name"": ""HouseholdMembers"",
                        ""type"": {
                            ""type"": ""array"",
                            ""items"": {
                                ""name"": ""HouseholdMember"",
                                ""type"": ""record"",
                                ""fields"": [
                                    {
                                        ""name"": ""Id"",
                                        ""type"": ""string"",
                                        ""logicalType"": ""uuid""
                                    }
                                ]
                            }
                        }
                    }
                ]
            }";

            var tenure = _tenure;
            tenure.HouseholdMembers = new List<HouseholdMembers> { tenure.HouseholdMembers.First() };

            var receivedRecord = ExecuteBuildTenureRecord(schema, tenure);
            receivedRecord["HouseholdMembers"].Should().BeOfType<GenericRecord[]>();

            var firstRecord = ((GenericRecord[]) receivedRecord["HouseholdMembers"]).ToList().First();
            firstRecord["Id"].Should().Be(tenure.HouseholdMembers.First().Id.ToString());

        }

        [Fact]
        public void BuildTenureRecordCanAssignEnums()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""TenureInformation"",
                ""fields"": [
                    {
                        ""name"": ""HouseholdMembers"",
                        ""type"": {
                            ""type"": ""array"",
                            ""items"": {
                                ""name"": ""HouseholdMember"",
                                ""type"": ""record"",
                                ""fields"": [
                                    {
                                        ""name"": ""Id"",
                                        ""type"": ""string"",
                                        ""logicalType"": ""uuid""
                                    },
                                    {
                                        ""name"": ""Type"",
                                        ""type"": {
                                            ""name"": ""HouseholdMembersType"",
                                            ""type"": ""enum"",
                                            ""symbols"": [
                                                ""Person"",
                                                ""Organisation""
                                            ]
                                        }
                                    }
                                ]
                            }
                        }
                    }
                ]
            }";

            var tenure = _tenure;
            tenure.HouseholdMembers = new List<HouseholdMembers> { tenure.HouseholdMembers.First() };
            var receivedRecord = ExecuteBuildTenureRecord(schema, tenure);
            var receivedHouseholdMember = ((GenericRecord[]) receivedRecord["HouseholdMembers"])[0];

            receivedHouseholdMember["Id"].Should().Be(_tenure.HouseholdMembers.First().Id.ToString());
            ((GenericEnum) receivedHouseholdMember["Type"]).Value.Should().Be(_tenure.HouseholdMembers.First().Type.ToString());
        }

        [Fact]
        public void BuildTenureCanHandleNestedObjects()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""TenureInformation"",
                ""fields"": [
                    {
                    ""name"": ""TenuredAsset"",
                    ""type"": {
                        ""type"": ""record"",
                        ""name"": ""TenuredAsset"",
                        ""fields"": [
                        {
                            ""name"": ""Id"",
                            ""type"": ""string"",
                            ""logicalType"": ""uuid""
                        },
                        {
                            ""name"": ""Type"",
                            ""type"": [{
                            ""name"": ""TenuredAssetType"",
                            ""type"": ""enum"",
                            ""symbols"": [
                                ""Block"",
                                ""Concierge"",
                                ""Dwelling"",
                                ""LettableNonDwelling"",
                                ""MediumRiseBlock"",
                                ""NA"",
                                ""TravellerSite""
                            ]
                            }, ""null""]
                        },
                        ]
                    }
                }
                ]
            }";

            var receivedRecord = ExecuteBuildTenureRecord(schema, _tenure);
            var receivedRecordEnum = (GenericEnum) ((GenericRecord) receivedRecord["TenuredAsset"])["Type"];
            receivedRecord["TenuredAsset"].Should().BeOfType<GenericRecord>();

            ((GenericRecord) receivedRecord["TenuredAsset"])["Id"].Should().Be(_tenure.TenuredAsset.Id.ToString());
            receivedRecordEnum.Value.Should().Be(_tenure.TenuredAsset.Type.ToString());

        }

        [Fact]
        public void LogsOutSchemaFieldNameWhenItDoesNotExistInTenure()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""TenureInformation"",
                ""fields"": [
                   {
                     ""name"": ""FieldNameNotInTenure"",
                     ""type"": ""string"",
                   },
                ]
            }";

            Func<GenericRecord> receivedRecord = () => ExecuteBuildTenureRecord(schema, _tenure);

            receivedRecord.Should().NotThrow<NullReferenceException>();
        }

        private T GetFieldValueFromStringName<T>(string fieldName, TenureResponseObject tenure)
        {
            return (T) typeof(TenureResponseObject).GetProperty(fieldName).GetValue(tenure);
        }

        private GenericRecord ExecuteBuildTenureRecord(string tenureSchema, TenureResponseObject tenure)
        {
            var schema = @$"{{
                ""type"": ""record"",
                ""name"": ""TenureAPIChangeEvent"",
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
                        ""name"": ""Tenure"",
                        ""type"": {tenureSchema}
                    }}
                ]
            }}";
            var tenureChangeEvent = _fixture.Create<TenureChangeEvent>();
            tenureChangeEvent.Tenure = _tenure;
            var record = _sut.BuildTenureRecord(schema, tenureChangeEvent);
            return (GenericRecord) record["Tenure"];
        }
    }
}

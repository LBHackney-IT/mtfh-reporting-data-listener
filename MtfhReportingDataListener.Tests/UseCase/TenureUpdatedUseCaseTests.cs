using AutoFixture;
using MtfhReportingDataListener.Boundary;
using MtfhReportingDataListener.Gateway.Interfaces;
using MtfhReportingDataListener.Infrastructure.Exceptions;
using MtfhReportingDataListener.UseCase;
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
using Avro;
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
            _mockGlue.Setup(x => x.GetSchema("", "", "")).ReturnsAsync(mockSchemaResponse);

            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            _mockKafka.Verify(x => x.SendDataToKafka("mtfh-reporting-data-listener", It.IsAny<GenericRecord>(), It.IsAny<Schema>()), Times.Once);
            _mockGlue.Verify(x => x.GetSchema("", "", ""), Times.Once());
        }

        [Fact]
        public void BuildTenureRecordCanSetOneStringValueToAGenericRecord()
        {
            var schema = (RecordSchema) Avro.Schema.Parse(@"{
                ""type"": ""record"",
                ""name"": ""TenureInformation"",
                ""fields"": [
                   {
                     ""name"": ""Id"",
                     ""type"": ""string"",
                     ""logicalType"": ""uuid""
                   }
                ]
            }");

            var tenure = _tenure;

            var expectedRecord = new GenericRecord(schema);
            expectedRecord.Add("Id", _tenure.Id);

            var receivedRecord = _sut.BuildTenureRecord(schema, tenure);


            Assert.Equal(expectedRecord["Id"].ToString(), receivedRecord["Id"]);
        }


        [Fact]
        public void BuildTenureRecordCanSetMultipleStringsToAGenericRecord()
        {
            var schema = (RecordSchema) Avro.Schema.Parse(@"{
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
            }");

            var tenure = _tenure;

            var expectedRecord = new GenericRecord(schema);
            expectedRecord.Add("Id", _tenure.Id);
            expectedRecord.Add("PaymentReference", _tenure.PaymentReference);

            var receivedRecord = _sut.BuildTenureRecord(schema, tenure);


            Assert.Equal(expectedRecord["Id"].ToString(), receivedRecord["Id"]);
            Assert.Equal(expectedRecord["PaymentReference"], receivedRecord["PaymentReference"]);
        }

        [Theory]
        [InlineData("IsTenanted")]
        public void BuildTenureRecordCanSetBooleanTypeValuesToAGenericRecord(string nullableBoolFieldName)
        {
            var schema = (RecordSchema) Avro.Schema.Parse(@$"{{
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
            }}");

            var tenure = _tenure;
            var fieldValue = GetFieldValueFromStringName<bool>(nullableBoolFieldName, tenure);

            var expectedRecord = new GenericRecord(schema);
            expectedRecord.Add("IsActive", tenure.IsActive);
            expectedRecord.Add(nullableBoolFieldName, fieldValue);

            var receivedRecord = _sut.BuildTenureRecord(schema, tenure);


            Assert.Equal(expectedRecord["IsActive"], receivedRecord["IsActive"]);
            Assert.Equal(expectedRecord[nullableBoolFieldName], receivedRecord[nullableBoolFieldName]);
        }

        [Fact]
        public void BuildTenureRecordCanSetIntTypeValuesToAGenericRecord()
        {
            var schema = (RecordSchema) Avro.Schema.Parse(@"{
                ""type"": ""record"",
                ""name"": ""TenureInformation"",
                ""fields"": [
                   {
                     ""name"": ""StartOfTenureDate"",
                     ""type"": [""int"", ""null""],
                     ""logicalType"": ""date""
                   },
                   {
                     ""name"": ""EndOfTenureDate"",
                     ""type"": [""int"", ""null""],
                   },
                ]
            }");

            var tenure = _tenure;

            var expectedRecord = new GenericRecord(schema);
            expectedRecord.Add("StartOfTenureDate", _tenure.StartOfTenureDate);
            expectedRecord.Add("EndOfTenureDate", _tenure.EndOfTenureDate);

            var receivedRecord = _sut.BuildTenureRecord(schema, tenure);
            Assert.Equal(expectedRecord["StartOfTenureDate"], receivedRecord["StartOfTenureDate"]);
            Assert.Equal(expectedRecord["EndOfTenureDate"], receivedRecord["EndOfTenureDate"]);
        }

        [Fact]
        public void BuildTenureRecordCanSetNestedFields()
        {
            var schema = (RecordSchema) Avro.Schema.Parse(@"{
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
                }");

            var receivedRecord = _sut.BuildTenureRecord(schema, _tenure);
            var receivedTenureType = (TenureType) receivedRecord["TenureType"];

            Assert.Equal(_tenure.TenureType.Code, receivedTenureType.Code);
        }

        [Fact]
        public void BuildTenureRecordCanSetLists()
        {
            var schema = (RecordSchema) Avro.Schema.Parse(@"{
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
            }");

            var receivedRecord = _sut.BuildTenureRecord(schema, _tenure);
            var receivedHouseholdMembers = (List<HouseholdMembers>) receivedRecord["HouseholdMembers"];

            Assert.Equal(_tenure.HouseholdMembers.First().Id, receivedHouseholdMembers.First().Id);
        }

        [Fact]
        public void BuildTenureRecordCanAssignEnums()
        {
            var schema = (RecordSchema) Avro.Schema.Parse(@"{
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
                                                ""Organization""
                                            ]
                                        }
                                    }
                                ]
                            }
                        }
                    }
                ]
            }");

            var receivedRecord = _sut.BuildTenureRecord(schema, _tenure);
            var receivedHouseholdMembers = (List<HouseholdMembers>) receivedRecord["HouseholdMembers"];

            Assert.Equal(_tenure.HouseholdMembers.First().Type, receivedHouseholdMembers.First().Type);
        }

        private T GetFieldValueFromStringName<T>(string fieldName, TenureResponseObject tenure)
        {
            return (T) typeof(TenureResponseObject).GetProperty(fieldName).GetValue(tenure);
        }
    }
}

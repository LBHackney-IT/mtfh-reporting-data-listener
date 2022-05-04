using AutoFixture;
using Avro.Generic;
using FluentAssertions;
using MtfhReportingDataListener.Domain;
using MtfhReportingDataListener.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace MtfhReportingDataListener.Tests.Helper
{
    public class ConvertToAvroHelperTests
    {
        private readonly ConvertToAvroHelper _classUnderTest;
        private readonly Fixture _fixture = new Fixture();
        private readonly FakeResponseObject _entity;
        public ConvertToAvroHelperTests()
        {
            _classUnderTest = new ConvertToAvroHelper();
            _entity = _fixture.Create<FakeResponseObject>();
        }

        [Fact]
        public void BuildRecordCanSetOneStringValueToAGenericRecord()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""ExampleRecord"",
                ""fields"": [
                   {
                     ""name"": ""Id"",
                     ""type"": ""string"",
                     ""logicalType"": ""uuid""
                   }
                ]
            }";

            var receivedRecord = ExecuteBuildRecord(schema, _entity);

            Assert.Equal(_entity.Id.ToString(), receivedRecord["Id"]);
        }

        [Fact]
        public void BuildRecordCanSetMultipleStringsToAGenericRecord()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""ExampleRecord"",
                ""fields"": [
                   {
                     ""name"": ""Id"",
                     ""type"": ""string"",
                     ""logicalType"": ""uuid""
                   },
                   {
                     ""name"": ""String"",
                     ""type"": ""string""
                   },
                ]
            }";

            var receivedRecord = ExecuteBuildRecord(schema, _entity);

            Assert.Equal(_entity.Id.ToString(), receivedRecord["Id"]);
            Assert.Equal(_entity.String, receivedRecord["String"]);
        }

        [Fact]
        public void BuildRecordCanConvertDatesToUnixTimestamps()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""ExampleRecord"",
                ""fields"": [
                   {
                     ""name"": ""Id"",
                     ""type"": ""string"",
                     ""logicalType"": ""uuid""
                   },
                   {
                     ""name"": ""Date"",
                     ""type"": [""null"", ""int""]
                   },
                ]
            }";

            var entity = _entity;
            entity.Date = new DateTime(1970, 01, 02);

            var receivedRecord = ExecuteBuildRecord(schema, entity);

            Assert.Equal(86400, receivedRecord["Date"]);
        }

        [Theory]
        [InlineData("NullableBool")]
        public void BuildRecordCanSetBooleanTypeValuesToAGenericRecord(string nullableBoolFieldName)
        {
            var schema = @$"{{
                ""type"": ""record"",
                ""name"": ""ExampleRecord"",
                ""fields"": [
                   {{
                     ""name"": ""Bool"",
                     ""type"": ""boolean""
                   }},
                   {{
                     ""name"": ""{nullableBoolFieldName}"",
                     ""type"": [""boolean"", ""null""]
                   }}
                ]
            }}";

            var fieldValue = GetFieldValueFromStringName<bool>(nullableBoolFieldName, _entity);

            var receivedRecord = ExecuteBuildRecord(schema, _entity);

            Assert.Equal(_entity.Bool, receivedRecord["Bool"]);
            Assert.Equal(fieldValue, receivedRecord[nullableBoolFieldName]);
        }

        [Fact]
        public void BuildRecordCanSetNestedFields()
        {
            var schema = @"{
                    ""type"": ""record"",
                    ""name"": ""ExampleRecord"",
                    ""fields"": [
                       {
                         ""name"": ""NestedField"",
                         ""type"": {
                            ""type"": ""record"",
                            ""name"": ""charge"",
                            ""fields"": [
                            {
                                ""name"": ""Id"",
                                ""type"": ""string""
                            }]
                            }
                        }
                    ]
                }";

            var receivedRecord = ExecuteBuildRecord(schema, _entity);
            var receivedNestedFields = (GenericRecord) receivedRecord["NestedField"];

            Assert.Equal(_entity.NestedField.Id, receivedNestedFields["Id"]);
        }

        [Fact]
        public void BuildRecordCanSetLists()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""ExampleRecord"",
                ""fields"": [
                    {
                        ""name"": ""FakeList"",
                        ""type"": {
                            ""type"": ""array"",
                            ""items"": {
                                ""name"": ""FakeList"",
                                ""type"": ""string""
                            }
                        }
                    }
                ]
            }";

            var entity = _entity;
            entity.FakeList = new List<string>();

            var receivedRecord = ExecuteBuildRecord(schema, entity);
            receivedRecord["FakeList"].Should().BeOfType<GenericRecord[]>();

        }

        [Fact]
        public void BuildRecordCanAssignEnums()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""ExampleRecord"",
                ""fields"": [
                    {
                            ""name"": ""FakeEnum"",
                            ""type"": [{
                            ""name"": ""FakeEnum"",
                            ""type"": ""enum"",
                            ""symbols"": [
                                ""Option1"",
                                ""Option2"" 
                            ]
                            }, ""null""]
                   }
                   ]
            }";

            var entity = _entity;
            var receivedRecord = ExecuteBuildRecord(schema, entity);

            ((GenericEnum) receivedRecord["FakeEnum"]).Value.Should().Be(_entity.FakeEnum.ToString());
        }

        [Fact]
        public void BuildRecordCanHandleNestedObjects()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""ExampleRecord"",
                ""fields"": [
                    {
                    ""name"": ""NestedField"",
                    ""type"": {
                        ""type"": ""record"",
                        ""name"": ""NestedField"",
                        ""fields"": [
                        {
                            ""name"": ""Id"",
                            ""type"": ""string"",
                        },
                         {
                            ""name"": ""FakeEnum"",
                            ""type"": [{
                            ""name"": ""FakeEnum"",
                            ""type"": ""enum"",
                            ""symbols"": [
                                ""Option1"",
                                ""Option2""
                            ]
                            }, ""null""]
                        },
                        ]
                    }
                }
                ]
            }";

            var receivedRecord = ExecuteBuildRecord(schema, _entity);
            var receivedRecordEnum = (GenericEnum) ((GenericRecord) receivedRecord["NestedField"])["FakeEnum"];
            receivedRecord["NestedField"].Should().BeOfType<GenericRecord>();


            ((GenericRecord) receivedRecord["NestedField"])["Id"].Should().Be(_entity.NestedField.Id);
            receivedRecordEnum.Value.Should().Be(_entity.NestedField.FakeEnum.ToString());

        }

        [Fact]
        public void LogsOutSchemaFieldNameWhenItDoesNotExistInEntity()
        {
            var schema = @"{
                ""type"": ""record"",
                ""name"": ""ExampleRecord"",
                ""fields"": [
                   {
                     ""name"": ""FieldNameNotInTenure"",
                     ""type"": ""string"",
                   },
                ]
            }";

            Func<GenericRecord> receivedRecord = () => ExecuteBuildRecord(schema, _entity);

            receivedRecord.Should().NotThrow<NullReferenceException>();
        }
        private GenericRecord ExecuteBuildRecord(string entitySchema, FakeResponseObject entity)
        {
            var schema = @$"{{
                ""type"": ""record"",
                ""name"": ""ChangeEvent"",
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
                        ""name"": ""Entity"",
                        ""type"": {entitySchema}
                    }}
                ]
            }}";
            var entityEvent = _fixture.Create<FakeEvent>();
            entityEvent.Entity = _entity;
            var record = _classUnderTest.BuildRecord(schema, entityEvent);
            return (GenericRecord) record["Entity"];
        }

        private T GetFieldValueFromStringName<T>(string fieldName, FakeResponseObject entity)
        {
            return (T) typeof(FakeResponseObject).GetProperty(fieldName).GetValue(entity);
        }
    }
}

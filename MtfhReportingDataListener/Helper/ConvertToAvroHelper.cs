using Avro;
using Avro.Generic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace MtfhReportingDataListener.Helper
{
    public class ConvertToAvroHelper : IConvertToAvroHelper
    {
        public ConvertToAvroHelper()
        {
        }
        public GenericRecord BuildRecord(string schema, object entityResponse)
        {
            return PopulateFields(entityResponse, Schema.Parse(schema));
        }
        public GenericRecord PopulateFields(object item, Schema schema)
        {
            var record = new GenericRecord((RecordSchema) schema);
            ((RecordSchema) schema).Fields.ForEach(field =>
            {
                PropertyInfo propInfo = item.GetType().GetProperty(field.Name);
                if (propInfo == null)
                {
                    Console.WriteLine($"Field name: {field.Name} not found in {item} with type {item.GetType()}");
                    return;
                }

                var fieldValue = propInfo.GetValue(item);
                var fieldSchema = field.Schema;
                var fieldType = field.Schema.Tag;

                if (fieldType == Schema.Type.Union)
                {
                    if (fieldValue == null)
                    {
                        record.Add(field.Name, null);
                        return;
                    }
                    fieldSchema = GetNonNullablePartOfNullableSchema(field.Schema);
                    fieldType = fieldSchema.Tag;
                }

                if (fieldType == Schema.Type.String)
                {
                    record.Add(field.Name, fieldValue.ToString());
                }
                else if (fieldType == Schema.Type.Enumeration)
                {
                    record.Add(field.Name, new GenericEnum((EnumSchema) fieldSchema, fieldValue.ToString()));
                }
                else if (fieldValue.GetType() == typeof(DateTime))
                {
                    record.Add(field.Name, UnixTimestampNullable(fieldValue));
                }
                else if (fieldType == Schema.Type.Array)
                {
                    var itemsType = fieldValue.GetType().GetGenericArguments().FirstOrDefault();
                    var itemsSchema = GetSchemaForArrayItems(fieldSchema);

                    var fieldValueAsList = ((JsonElement) System.Text.Json.JsonSerializer.Deserialize<object>(JsonConvert.SerializeObject(fieldValue))).EnumerateArray();
                    var recordsList = fieldValueAsList.Select(listItem => PopulateFields(JsonConvert.DeserializeObject(listItem.ToString(), itemsType), itemsSchema)).ToArray();

                    record.Add(field.Name, recordsList);
                }
                else if (fieldType == Schema.Type.Record)
                {
                    record.Add(field.Name, PopulateFields(fieldValue, fieldSchema));
                }
                else
                {
                    record.Add(field.Name, fieldValue);
                }
            });

            return record;
        }

        private Schema GetSchemaForArrayItems(Schema arraySchema)
        {
            var jsonSchema = (JsonElement) JsonSerializer.Deserialize<object>(arraySchema.ToString());
            jsonSchema.TryGetProperty("items", out var itemsSchemaJson);
            return Schema.Parse(itemsSchemaJson.ToString());
        }

        private Schema GetNonNullablePartOfNullableSchema(Schema nullableSchema)
        {
            var jsonSchema = (JsonElement) JsonSerializer.Deserialize<object>(nullableSchema.ToString());
            jsonSchema.TryGetProperty("type", out var unionList);
            var notNullSchema = unionList.EnumerateArray().First(type => type.ToString() != "null").ToString();
            return Schema.Parse(notNullSchema);
        }

        private int? UnixTimestampNullable(object obj)
        {
            var date = (DateTime?) obj;
            return (int?) (date?.Subtract(new DateTime(1970, 1, 1)))?.TotalSeconds;
        }

    }
}

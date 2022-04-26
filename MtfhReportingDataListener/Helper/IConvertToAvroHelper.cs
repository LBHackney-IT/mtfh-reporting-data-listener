using Avro;
using Avro.Generic;
using System;
using System.Collections.Generic;
using System.Text;

namespace MtfhReportingDataListener.Helper
{
    public interface IConvertToAvroHelper
    {
        GenericRecord BuildRecord(string schema, object entityResponse);
        GenericRecord PopulateFields(object item, Schema schema);
    }
}

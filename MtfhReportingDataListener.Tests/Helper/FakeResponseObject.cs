using MtfhReportingDataListener.Boundary;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace MtfhReportingDataListener.Tests.Helper
{
    public class FakeResponseObject
    {
        public Guid Id { get; set; }
        public string String { get; set; }
        public List<string> FakeList { get; set; }
        public FakeEnum FakeEnum { get; set; }
        public DateTime? Date { get; set; }
        public bool Bool { get; set; }
        public bool? NullableBool { get; set; }

        public NestedField NestedField { get; set; }

    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FakeEnum
    {
        Option1,
        Option2
    }

    public class NestedField
    {
        public string Id { get; set; }
        public FakeEnum FakeEnum { get; set; }
    }

    public class FakeEvent
    {
        public EntityEventSns EntityEventSns { get; set; }

        public FakeResponseObject Entity { get; set; }

    }
}

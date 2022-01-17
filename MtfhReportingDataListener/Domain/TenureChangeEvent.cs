using System;
using Hackney.Shared.Tenure.Boundary.Response;

namespace MtfhReportingDataListener.Domain
{
    public class TenureChangeEvent
    {
        public Guid Id { get; set; }

        public string EventType { get; set; }

        public string SourceDomain { get; set; }

        public string SourceSystem { get; set; }

        public string Version { get; set; }

        public Guid CorrelationId { get; set; }

        public DateTime DateTime { get; set; }

        public User User { get; set; }

        public TenureResponseObject Tenure { get; set; }
    }

    public class User
    {
        public string Name { get; set;}

        public string Email { get; set; }
    }
}

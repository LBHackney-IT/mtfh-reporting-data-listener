using Hackney.Shared.ContactDetail.Boundary.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace MtfhReportingDataListener.Domain
{
    public class ContactDetailEvent
    {
        public Guid Id { get; set; }

        public string EventType { get; set; }

        public string SourceDomain { get; set; }

        public string SourceSystem { get; set; }

        public string Version { get; set; }

        public Guid CorrelationId { get; set; }

        public DateTime DateTime { get; set; }

        public User User { get; set; }

        public ContactDetailsResponseObject ContactDetails { get; set; }
    }
}

using Hackney.Shared.ContactDetail.Boundary.Response;
using Hackney.Shared.Tenure.Boundary.Response;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MtfhReportingDataListener.Gateway.Interfaces
{
    public interface IContactDetailApiGateway
    {
        Task<ContactDetailsResponseObject> GetContactDetailByTargetIdAsync(Guid targetId, Guid correlationId);
    }
}

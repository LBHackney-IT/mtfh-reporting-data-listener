using Hackney.Shared.Tenure.Boundary.Response;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MtfhReportingDataListener.Gateway.Interfaces
{
    public interface ITenureInfoApiGateway
    {
        Task<TenureResponseObject> GetTenureInfoByIdAsync(Guid id, Guid correlationId);
    }
}

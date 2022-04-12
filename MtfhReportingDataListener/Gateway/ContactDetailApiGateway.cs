using Hackney.Core.Http;
using Hackney.Core.Logging;
using Hackney.Shared.ContactDetail.Boundary.Response;
using Hackney.Shared.Tenure.Boundary.Response;
using MtfhReportingDataListener.Gateway.Interfaces;
using System;
using System.Threading.Tasks;

namespace MtfhReportingDataListener.Gateway
{
    public class ContactDetailApiGateway : IContactDetailApiGateway
    {
        private const string ApiName = "ContactDetail";
        private const string ContactDetailApiUrl = "ContactDetailApiUrl";
        private const string ContactDetailApiToken = "ContactDetailApiToken";

        private readonly IApiGateway _apiGateway;

        public ContactDetailApiGateway(IApiGateway apiGateway)
        {
            _apiGateway = apiGateway;
            _apiGateway.Initialise(ApiName, ContactDetailApiUrl, ContactDetailApiToken);
        }

        [LogCall]
        public async Task<ContactDetailsResponseObject> GetContactDetailByTargetIdAsync(Guid targetId, Guid correlationId)
        {
            var route = $"{_apiGateway.ApiRoute}/contactDetails?{targetId}";
            return await _apiGateway.GetByIdAsync<ContactDetailsResponseObject>(route, targetId, correlationId);
        }
    }
}

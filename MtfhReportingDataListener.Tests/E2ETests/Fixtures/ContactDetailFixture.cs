using AutoFixture;
using Hackney.Core.Testing.Shared.E2E;
using Hackney.Shared.ContactDetail.Boundary.Response;
using System;


namespace MtfhReportingDataListener.Tests.E2ETests.Fixtures
{
    public class ContactDetailFixture : BaseApiFixture<ContactDetailsResponseObject>
    {
        private readonly Fixture _fixture = new Fixture();
        private const string ContactDetailApiRoute = "http://localhost:5678/api/v1/";
        private const string ContactDetailApiToken = "sdjkhfgsdkjfgsdjfgh";
        public Guid TargetId;

        public ContactDetailFixture() : base(ContactDetailApiRoute, ContactDetailApiToken)
        {
            Environment.SetEnvironmentVariable("ContactDetailApiUrl", ContactDetailApiRoute);
            Environment.SetEnvironmentVariable("ContactDetailApiToken", ContactDetailApiToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                base.Dispose(disposing);
            }
        }

        public ContactDetailsResponseObject GivenContactDetailHasBeenCreated()
        {

            var contactDetailsResponseObject = _fixture.Build<ContactDetailsResponseObject>()
                           .Create();
            TargetId = contactDetailsResponseObject.TargetId;
            ResponseObject = contactDetailsResponseObject;
            return ResponseObject;
        }

        public void GivenNonExistingContactDetailHasBeenCreated()
        {
            TargetId = _fixture.Create<Guid>();
        }
    }
}

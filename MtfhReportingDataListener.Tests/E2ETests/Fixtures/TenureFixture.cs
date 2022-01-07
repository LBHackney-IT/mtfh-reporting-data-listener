using AutoFixture;
using Hackney.Core.Testing.Shared.E2E;
using Hackney.Shared.Tenure.Boundary.Response;
using Hackney.Shared.Tenure.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MtfhReportingDataListener.Tests.E2ETests.Fixtures
{
    public class TenureFixture : BaseApiFixture<TenureResponseObject>
    {
        private readonly Fixture _fixture = new Fixture();
        private const string TenureApiRoute = "http://localhost:5678/api/v1/";
        private const string TenureApiToken = "sdjkhfgsdkjfgsdjfgh";
        public Guid TenureId;

        public TenureFixture() : base(TenureApiRoute, TenureApiToken)
        {
            Environment.SetEnvironmentVariable("TenureApiUrl", TenureApiRoute);
            Environment.SetEnvironmentVariable("TenureApiToken", TenureApiToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                base.Dispose(disposing);
            }
        }

        public TenureResponseObject GivenTenureHasBeenUpdated()
        {
            var hms = _fixture.Build<HouseholdMembers>()
                            .With(x => x.DateOfBirth, DateTime.UtcNow.AddYears(-40))
                            .CreateMany(3).ToList();
            var tenureResponseObject = _fixture.Build<TenureResponseObject>()
                           .With(x => x.HouseholdMembers, hms)
                           .Create();
            TenureId = tenureResponseObject.Id;
            return tenureResponseObject;
        }

        public void GivenNonExistingTenureHasBeenUpdated()
        {
            TenureId = _fixture.Create<Guid>(); 
        }
    }
}

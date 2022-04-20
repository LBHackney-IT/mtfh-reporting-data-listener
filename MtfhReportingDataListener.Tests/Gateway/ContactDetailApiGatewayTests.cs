using AutoFixture;
using FluentAssertions;
using Hackney.Core.Http;
using Hackney.Shared.ContactDetail.Boundary.Response;
using Moq;
using MtfhReportingDataListener.Gateway;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MtfhReportingDataListener.Tests.Gateway
{
    [Collection("LogCall collection")]
    public class ContactDetailApiGatewayTests
    {
        private readonly Mock<IApiGateway> _mockApiGateway;

        private static readonly Guid _targetId = Guid.NewGuid();
        private static readonly Guid _correlationId = Guid.NewGuid();
        private const string ContactDetailApiRoute = "https://some-domain.com/api/";
        private const string ContactDetailApiToken = "dksfghjskueygfakseygfaskjgfsdjkgfdkjsgfdkjgf";

        private const string ApiName = "ContactDetail";
        private const string ContactDetailApiUrlKey = "ContactDetailApiUrl";
        private const string ContactDetailApiTokenKey = "ContactDetailApiToken";

        public ContactDetailApiGatewayTests()
        {
            _mockApiGateway = new Mock<IApiGateway>();

            _mockApiGateway.SetupGet(x => x.ApiName).Returns(ApiName);
            _mockApiGateway.SetupGet(x => x.ApiRoute).Returns(ContactDetailApiRoute);
            _mockApiGateway.SetupGet(x => x.ApiToken).Returns(ContactDetailApiToken);
        }

        private static string Route => $"{ContactDetailApiRoute}/contactDetails?{_targetId}";

        [Fact]
        public void ConstructorTestInitialisesApiGateway()
        {
            new ContactDetailApiGateway(_mockApiGateway.Object);
            _mockApiGateway.Verify(x => x.Initialise(ApiName, ContactDetailApiUrlKey, ContactDetailApiTokenKey, null),
                                   Times.Once);
        }

        [Fact]
        public void GetContactDetailByIdAsyncGetExceptionThrown()
        {
            var exMessage = "This is an exception";
            _mockApiGateway.Setup(x => x.GetByIdAsync<ContactDetailsResponseObject>(Route, _targetId, _correlationId))
                           .ThrowsAsync(new Exception(exMessage));

            var sut = new ContactDetailApiGateway(_mockApiGateway.Object);
            Func<Task<ContactDetailsResponseObject>> func =
                async () => await sut.GetContactDetailByTargetIdAsync(_targetId, _correlationId).ConfigureAwait(false);

            func.Should().ThrowAsync<Exception>().WithMessage(exMessage);
        }

        [Fact]
        public async Task GetContactDetailByIdAsyncNotFoundReturnsNull()
        {
            var sut = new ContactDetailApiGateway(_mockApiGateway.Object);
            var result = await sut.GetContactDetailByTargetIdAsync(_targetId, _correlationId).ConfigureAwait(false);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetContactDetailByIdAsyncCallReturnsContactDetail()
        {
            var contactDetail = new Fixture().Create<ContactDetailsResponseObject>();
            _mockApiGateway.Setup(x => x.GetByIdAsync<ContactDetailsResponseObject>(Route, _targetId, _correlationId))
                           .ReturnsAsync(contactDetail);

            var sut = new ContactDetailApiGateway(_mockApiGateway.Object);
            var result = await sut.GetContactDetailByTargetIdAsync(_targetId, _correlationId).ConfigureAwait(false);

            result.Should().BeEquivalentTo(contactDetail);
        }


    }
}

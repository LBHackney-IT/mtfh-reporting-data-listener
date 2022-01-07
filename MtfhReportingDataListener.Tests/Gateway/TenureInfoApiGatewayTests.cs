using AutoFixture;
using FluentAssertions;
using Hackney.Core.Http;
using Hackney.Shared.Tenure.Boundary.Response;
using Moq;
using MtfhReportingDataListener.Gateway;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MtfhReportingDataListener.Tests.Gateway
{
    [Collection("LogCall collection")]
    public class TenureInfoApiGatewayTests
    {
        private readonly Mock<IApiGateway> _mockApiGateway;

        private static readonly Guid _id = Guid.NewGuid();
        private static readonly Guid _correlationId = Guid.NewGuid();
        private const string TenureApiRoute = "https://some-domain.com/api/";
        private const string TenureApiToken = "dksfghjskueygfakseygfaskjgfsdjkgfdkjsgfdkjgf";

        private const string ApiName = "Tenure";
        private const string TenureApiUrlKey = "TenureApiUrl";
        private const string TenureApiTokenKey = "TenureApiToken";

        public TenureInfoApiGatewayTests()
        {
            _mockApiGateway = new Mock<IApiGateway>();

            _mockApiGateway.SetupGet(x => x.ApiName).Returns(ApiName);
            _mockApiGateway.SetupGet(x => x.ApiRoute).Returns(TenureApiRoute);
            _mockApiGateway.SetupGet(x => x.ApiToken).Returns(TenureApiToken);
        }

        private static string Route => $"{TenureApiRoute}/tenures/{_id}";

        [Fact]
        public void ConstructorTestInitialisesApiGateway()
        {
            new TenureInfoApiGateway(_mockApiGateway.Object);
            _mockApiGateway.Verify(x => x.Initialise(ApiName, TenureApiUrlKey, TenureApiTokenKey, null),
                                   Times.Once);
        }

        [Fact]
        public void GetTenureInfoByIdAsyncGetExceptionThrown()
        {
            var exMessage = "This is an exception";
            _mockApiGateway.Setup(x => x.GetByIdAsync<TenureResponseObject>(Route, _id, _correlationId))
                           .ThrowsAsync(new Exception(exMessage));

            var sut = new TenureInfoApiGateway(_mockApiGateway.Object);
            Func<Task<TenureResponseObject>> func =
                async () => await sut.GetTenureInfoByIdAsync(_id, _correlationId).ConfigureAwait(false);

            func.Should().ThrowAsync<Exception>().WithMessage(exMessage);
        }

        [Fact]
        public async Task GetTenureInfoByIdAsyncNotFoundReturnsNull()
        {
            var sut = new TenureInfoApiGateway(_mockApiGateway.Object);
            var result = await sut.GetTenureInfoByIdAsync(_id, _correlationId).ConfigureAwait(false);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetTenureInfoByIdAsyncCallReturnsTenure()
        {
            var tenure = new Fixture().Create<TenureResponseObject>();
            _mockApiGateway.Setup(x => x.GetByIdAsync<TenureResponseObject>(Route, _id, _correlationId))
                           .ReturnsAsync(tenure);

            var sut = new TenureInfoApiGateway(_mockApiGateway.Object);
            var result = await sut.GetTenureInfoByIdAsync(_id, _correlationId).ConfigureAwait(false);

            result.Should().BeEquivalentTo(tenure);
        }


    }
}

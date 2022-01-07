using MtfhReportingDataListener.Tests.E2ETests.Fixtures;
using MtfhReportingDataListener.Tests.E2ETests.Steps;
using System;
using System.Collections.Generic;
using System.Text;
using TestStack.BDDfy;
using Xunit;

namespace MtfhReportingDataListener.Tests.E2ETests.Stories
{
    [Story(
       AsA = "SQS Entity Listener",
       IWant = "a function to process the TenureUpdated message",
       SoThat = "The data is saved to kafka")]
    [Collection("AppTest collection")]
    public class TenureUpdatedTests : IDisposable
    {
        private readonly TenureFixture _tenureFixture;
        private readonly TenureUpdatedUseCaseSteps _steps;
        private readonly MockApplicationFactory _appFactory;

        public TenureUpdatedTests(MockApplicationFactory appFactory)
        {
            _tenureFixture = new TenureFixture();
            _appFactory = appFactory;
            _steps = new TenureUpdatedUseCaseSteps();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _tenureFixture.Dispose();

                _disposed = true;
            }
        }

        [Fact]
        public void ListenerSavesTheUpdatedTenureToKafka()
        {
            this.Given(g => _tenureFixture.GivenTenureHasBeenUpdated())
               .When(w => _steps.WhenTheFunctionIsTriggered(_tenureFixture.TenureId))
               .Then(t => _steps.ThenTheUpdatedDataIsSavedToKafka(_appFactory, _steps.TheMessage))
               .BDDfy();
        }

        [Fact(Skip = "Not sure if this test is required")]
        public void ListenerNotFoundException()
        {
            this.Given(g => _tenureFixture.GivenNonExistingTenureHasBeenUpdated())
               .When(w => _steps.WhenTheFunctionIsTriggered(_tenureFixture.TenureId))
               .Then(t => _steps.ThenEntityNotFoundExceptionIsThrown(_tenureFixture.TenureId))
               .BDDfy();
        }
    }
}

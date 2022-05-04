using MtfhReportingDataListener.Tests.E2ETests.Fixtures;
using MtfhReportingDataListener.Tests.E2ETests.Steps;
using System;
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
        private readonly MockTenureSchemaRegistry _schemaRegistry;
        private readonly TenureUseCaseSteps _steps;
        private readonly MockApplicationFactory _appFactory;

        public TenureUpdatedTests(MockApplicationFactory appFactory)
        {
            _tenureFixture = new TenureFixture();
            _appFactory = appFactory;
            _steps = new TenureUseCaseSteps();
            _schemaRegistry = new MockTenureSchemaRegistry();
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
        public void ListenerSavesAnUpdatedTenureToKafka()
        {

            this.Given(g => _tenureFixture.GivenTenureHasBeenUpdated())
                .And(g => _schemaRegistry.GivenThereIsAMatchingSchemaInTheRegistry(_appFactory.HttpClient))
               .When(w => _steps.WhenTheFunctionIsTriggered(_tenureFixture.TenureId, EventTypes.TenureUpdatedEvent))
               .Then(t => _steps.ThenTheMessageIsSavedToKafka(_schemaRegistry.Topic, _tenureFixture.ResponseObject))
               .BDDfy();
        }

        [Fact]
        public void TenureNotFoundException()
        {
            this.Given(g => _tenureFixture.GivenNonExistingTenureHasBeenUpdated())
               .When(w => _steps.WhenTheFunctionIsTriggered(_tenureFixture.TenureId, EventTypes.TenureUpdatedEvent))
               .Then(t => _steps.ThenEntityNotFoundExceptionIsThrown(_tenureFixture.TenureId))
               .BDDfy();
        }
    }
}

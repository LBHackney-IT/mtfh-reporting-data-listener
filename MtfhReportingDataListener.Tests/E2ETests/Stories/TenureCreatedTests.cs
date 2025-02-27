using MtfhReportingDataListener.Tests.E2ETests.Fixtures;
using MtfhReportingDataListener.Tests.E2ETests.Steps;
using System;
using TestStack.BDDfy;
using Xunit;

namespace MtfhReportingDataListener.Tests.E2ETests.Stories
{
    [Story(
       AsA = "SQS Entity Listener",
       IWant = "a function to process the TenureCreated message",
       SoThat = "The data is saved to kafka")]
    [Collection("AppTest collection")]
    public class TenureCreatedTests : IDisposable
    {
        private readonly TenureFixture _tenureFixture;
        private readonly MockSchemaRegistry _schemaRegistry;
        private readonly TenureUseCaseSteps _steps;
        private readonly MockApplicationFactory _appFactory;

        public TenureCreatedTests(MockApplicationFactory appFactory)
        {
            _tenureFixture = new TenureFixture();
            _appFactory = appFactory;
            _steps = new TenureUseCaseSteps();
            _schemaRegistry = new MockSchemaRegistry();
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

        [Fact(Skip = "This is currently not working as we need to find a way to test if a certian message " +
            "(a unique property between generic record tenure and message) has come through to kafka")]
        public void ListenerSavesTheCreatedTenureToKafka()
        {

            this.Given(g => _tenureFixture.GivenTenureHasBeenCreated())
                .And(g => _schemaRegistry.GivenThereIsAMatchingSchemaInTheRegistry(_appFactory.HttpClient))
               .When(w => _steps.WhenTheFunctionIsTriggered(_tenureFixture.TenureId, EventTypes.TenureCreatedEvent))
               .Then(t => _steps.ThenTheMessageIsSavedToKafka(_schemaRegistry.Topic, _tenureFixture.ResponseObject))
               .BDDfy();
        }

        [Fact]
        public void TenureNotFoundException()
        {
            this.Given(g => _tenureFixture.GivenNonExistingTenureHasBeenCreated())
               .When(w => _steps.WhenTheFunctionIsTriggered(_tenureFixture.TenureId, EventTypes.TenureCreatedEvent))
               .Then(t => _steps.ThenEntityNotFoundExceptionIsThrown(_tenureFixture.TenureId))
               .BDDfy();
        }
    }
}

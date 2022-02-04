using MtfhReportingDataListener.Tests.E2ETests.Fixtures;
using MtfhReportingDataListener.Tests.E2ETests.Steps;
using MtfhReportingDataListener.Factories;
using System;
using TestStack.BDDfy;
using Xunit;
using Moq;

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
        private readonly TenureCreatedUseCaseSteps _steps;
        private readonly MockApplicationFactory _appFactory;
        private readonly Mock<IGlueFactory> _mockGlue;

        public TenureCreatedTests(MockApplicationFactory appFactory)
        {
            _tenureFixture = new TenureFixture();
            _appFactory = appFactory;
            _steps = new TenureCreatedUseCaseSteps();
            _mockGlue = new Mock<IGlueFactory>();
            _schemaRegistry = new MockSchemaRegistry(_mockGlue);
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
        public void ListenerSavesTheCreatedTenureToKafka()
        {

            this.Given(g => _tenureFixture.GivenTenureHasBeenCreated())
                .And(g => _schemaRegistry.GivenThereIsAMatchingSchemaInGlueRegistry())
               .When(w => _steps.WhenTheFunctionIsTriggered(_tenureFixture.TenureId, _mockGlue.Object))
               .Then(t => _steps.ThenTheCreatedDataIsSavedToKafka(_schemaRegistry.SchemaDefinition, _tenureFixture.ResponseObject))
               .BDDfy();
        }

        [Fact]
        public void ListenerNotFoundException()
        {
            this.Given(g => _tenureFixture.GivenNonExistingTenureHasBeenCreated())
               .When(w => _steps.WhenTheFunctionIsTriggered(_tenureFixture.TenureId, _mockGlue.Object))
               .Then(t => _steps.ThenEntityNotFoundExceptionIsThrown(_tenureFixture.TenureId))
               .BDDfy();
        }
    }
}

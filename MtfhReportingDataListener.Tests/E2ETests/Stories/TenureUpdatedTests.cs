using MtfhReportingDataListener.Tests.E2ETests.Fixtures;
using MtfhReportingDataListener.Tests.E2ETests.Steps;
using System;
using System.Collections.Generic;
using System.Text;
using TestStack.BDDfy;
using Xunit;
using Moq;
using Amazon.Glue;

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
        private readonly MockSchemaRegistry _schemaRegistry;
        private readonly TenureUpdatedUseCaseSteps _steps;
        private readonly MockApplicationFactory _appFactory;
        private readonly Mock<IAmazonGlue> _mockGlue;

        public TenureUpdatedTests(MockApplicationFactory appFactory)
        {
            _tenureFixture = new TenureFixture();
            _appFactory = appFactory;
            _steps = new TenureUpdatedUseCaseSteps();
            _mockGlue = new Mock<IAmazonGlue>();
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
        public void ListenerSavesTheUpdatedTenureToKafka()
        {

            this.Given(g => _tenureFixture.GivenTenureHasBeenUpdated()) // Creates a tenure
                .And(g => _schemaRegistry.GivenThereIsAMatchingSchemaInGlueRegistry()) // and Given there is a matching schema in glue registry -> create registryName, schemaArn, schemaName, schemaDefinition & setup mock
               .When(w => _steps.WhenTheFunctionIsTriggered(_tenureFixture.TenureId, _mockGlue.Object)) // Calling the lambda
               .Then(t => _steps.ThenTheUpdatedDataIsSavedToKafka(_steps.TheMessage, _schemaRegistry.SchemaDefinition)) // assertion -> registry & schema details
               .BDDfy();
        }

        [Fact]
        public void ListenerNotFoundException()
        {
            this.Given(g => _tenureFixture.GivenNonExistingTenureHasBeenUpdated())
               .When(w => _steps.WhenTheFunctionIsTriggered(_tenureFixture.TenureId, _mockGlue.Object))
               .Then(t => _steps.ThenEntityNotFoundExceptionIsThrown(_tenureFixture.TenureId))
               .BDDfy();
        }
    }
}

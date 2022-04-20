using MtfhReportingDataListener.Tests.E2ETests.Fixtures;
using MtfhReportingDataListener.Tests.E2ETests.Steps;
using System;
using TestStack.BDDfy;
using Xunit;

namespace MtfhReportingDataListener.Tests.E2ETests.Stories
{
    [Story(
       AsA = "SQS Entity Listener",
       IWant = "a function to process the ContactDetailAdded message",
       SoThat = "The data is saved to kafka")]
    [Collection("AppTest collection")]
    public class ContactDetailAddedTests : IDisposable
    {
        private readonly ContactDetailFixture _contactDetailFixture;
        private readonly MockSchemaRegistry _schemaRegistry;
        private readonly ContactDetailUseCaseSteps _steps;
        private readonly MockApplicationFactory _appFactory;

        public ContactDetailAddedTests(MockApplicationFactory appFactory)
        {
            _contactDetailFixture = new ContactDetailFixture();
            _appFactory = appFactory;
            _steps = new ContactDetailUseCaseSteps();
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
                _contactDetailFixture.Dispose();

                _disposed = true;
            }
        }

        [Fact]
        public void ListenerSavesAnCreatedContactDetailToKafka()
        {

            this.Given(g => _contactDetailFixture.GivenContactDetailHasBeenCreated())
                .And(g => _schemaRegistry.GivenThereIsAMatchingSchemaInTheRegistry(_appFactory.HttpClient))
               .When(w => _steps.WhenTheFunctionIsTriggered(_contactDetailFixture.TargetId, EventTypes.ContactDetailAddedEvent))
               .Then(t => _steps.ThenTheMessageIsSavedToKafka(_schemaRegistry.Topic, _contactDetailFixture.ResponseObject))
               .BDDfy();
        }

        [Fact]
        public void ContactDetailNotFoundException()
        {
            this.Given(g => _contactDetailFixture.GivenNonExistingContactDetailHasBeenCreated())
               .When(w => _steps.WhenTheFunctionIsTriggered(_contactDetailFixture.TargetId, EventTypes.ContactDetailAddedEvent))
               .Then(t => _steps.ThenEntityNotFoundExceptionIsThrown(_contactDetailFixture.TargetId))
               .BDDfy();
        }
    }
}

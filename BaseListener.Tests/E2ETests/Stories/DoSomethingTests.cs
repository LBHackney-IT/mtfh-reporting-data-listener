using BaseListener.Tests.E2ETests.Fixtures;
using BaseListener.Tests.E2ETests.Steps;
using Hackney.Core.Testing.DynamoDb;
using System;
using TestStack.BDDfy;
using Xunit;

namespace BaseListener.Tests.E2ETests.Stories
{
    [Story(
        AsA = "SQS Entity Listener",
        IWant = "a function to process the DoSomething message",
        SoThat = "The correct details are set on the entity")]
    [Collection("AppTest collection")]
    public class DoSomethingTests : IDisposable
    {
        private readonly IDynamoDbFixture _dbFixture;
        private readonly EntityFixture _entityFixture;

        private readonly DoSomethingUseCaseSteps _steps;

        public DoSomethingTests(MockApplicationFactory appFactory)
        {
            _dbFixture = appFactory.DynamoDbFixture;

            _entityFixture = new EntityFixture(_dbFixture.DynamoDbContext);

            _steps = new DoSomethingUseCaseSteps();
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
                _entityFixture.Dispose();

                _disposed = true;
            }
        }

        [Fact]
        public void ListenerUpdatesTheEntity()
        {
            var id = Guid.NewGuid();
            this.Given(g => _entityFixture.GivenAnEntityAlreadyExists(id))
                .When(w => _steps.WhenTheFunctionIsTriggered(id))
                .Then(t => _steps.ThenTheEntityIsUpdated(_entityFixture.DbEntity, _dbFixture.DynamoDbContext))
                .BDDfy();
        }

        [Fact]
        public void EntityNotFound()
        {
            var id = Guid.NewGuid();
            this.Given(g => _entityFixture.GivenAnEntityDoesNotExist(id))
                .When(w => _steps.WhenTheFunctionIsTriggered(id))
                .Then(t => _steps.ThenAnEntityNotFoundExceptionIsThrown(id))
                .BDDfy();
        }
    }
}

using Amazon.Lambda.SQSEvents;
using FluentAssertions;
using Hackney.Shared.Tenure.Boundary.Response;
using MtfhReportingDataListener.Infrastructure.Exceptions;
using System;
using System.Threading.Tasks;

namespace MtfhReportingDataListener.Tests.E2ETests.Steps
{
    public class TenureUpdatedUseCaseSteps : BaseSteps
    {
        public SQSEvent.SQSMessage TheMessage { get; private set; }

        public TenureUpdatedUseCaseSteps()
        {
            _eventType = EventTypes.TenureUpdatedEvent;
        }

        public async Task WhenTheFunctionIsTriggered(Guid id)
        {
            await TriggerFunction(id).ConfigureAwait(false);
        }

        public async Task WhenTheFunctionIsTriggered(SQSEvent.SQSMessage message)
        {
            await TriggerFunction(message).ConfigureAwait(false);
        }

        //Not sure if this is required
        public void ThenEntityNotFoundExceptionIsThrown(Guid id)
        {
            _lastException.Should().NotBeNull();
            _lastException.Should().BeOfType(typeof(EntityNotFoundException<TenureResponseObject>));
                (_lastException as EntityNotFoundException<TenureResponseObject>).Id.Should().Be(id);
        }
        public void ThenTheUpdatedDataIsSavedToKafka()
        {
            //TODO
        }


    }
}

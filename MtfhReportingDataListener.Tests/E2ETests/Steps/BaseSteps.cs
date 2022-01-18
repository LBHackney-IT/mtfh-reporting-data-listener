using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using AutoFixture;
using MtfhReportingDataListener.Boundary;
using MtfhReportingDataListener.Infrastructure;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Amazon.Glue;

namespace MtfhReportingDataListener.Tests.E2ETests.Steps
{
    public class BaseSteps
    {
        protected readonly JsonSerializerOptions _jsonOptions = JsonOptions.CreateJsonOptions();
        protected readonly Fixture _fixture = new Fixture();
        protected string _eventType;
        protected readonly Guid _correlationId = Guid.NewGuid();
        protected Exception _lastException;

        public BaseSteps()
        { }

        protected EntityEventSns CreateEvent(Guid tenureId)
        {
            return _fixture.Build<EntityEventSns>()
                           .With(x => x.EntityId, tenureId)
                           .With(x => x.EventType, _eventType)
                           .With(x => x.CorrelationId, _correlationId)
                           .Create();
        }

        protected SQSEvent.SQSMessage CreateMessage(EntityEventSns eventSns)
        {
            var msgBody = JsonSerializer.Serialize(eventSns, _jsonOptions);
            return _fixture.Build<SQSEvent.SQSMessage>()
                           .With(x => x.Body, msgBody)
                           .With(x => x.MessageAttributes, new Dictionary<string, SQSEvent.MessageAttribute>())
                           .Create();
        }

        protected async Task<SQSEvent.SQSMessage> TriggerFunction(Guid id, IAmazonGlue glue)
        {
            var snsEvent = CreateEvent(id);
            var message = CreateMessage(snsEvent);
            return await TriggerFunction(message, glue).ConfigureAwait(false);
        }

        protected async Task<SQSEvent.SQSMessage> TriggerFunction(SQSEvent.SQSMessage message, IAmazonGlue glue)
        {
            var mockLambdaLogger = new Mock<ILambdaLogger>();
            ILambdaContext lambdaContext = new TestLambdaContext()
            {
                Logger = mockLambdaLogger.Object
            };

            var sqsEvent = _fixture.Build<SQSEvent>()
                                   .With(x => x.Records, new List<SQSEvent.SQSMessage> { message })
                                   .Create();

            Func<Task> func = async () =>
            {
                var fn = new SqsFunction(glue);
                await fn.FunctionHandler(sqsEvent, lambdaContext).ConfigureAwait(false);
            };

            _lastException = await Record.ExceptionAsync(func);
            return message;

        }
    }
}

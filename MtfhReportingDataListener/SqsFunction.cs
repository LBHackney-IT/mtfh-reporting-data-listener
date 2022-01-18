using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using MtfhReportingDataListener.Boundary;
using MtfhReportingDataListener.Gateway;
using MtfhReportingDataListener.Gateway.Interfaces;
using MtfhReportingDataListener.UseCase;
using MtfhReportingDataListener.UseCase.Interfaces;
using Hackney.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Glue;
using MtfhReportingDataListener.Factories;
using Hackney.Core.Http;
using Microsoft.Extensions.Configuration;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MtfhReportingDataListener
{
    /// <summary>
    /// Lambda function triggered by an SQS message
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SqsFunction : BaseFunction
    {
        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public SqsFunction() { }

        /// <summary>
        /// Constructor to be used by the E2E tests so that we can mock API calls to AWS Glue.
        /// </summary>
        public SqsFunction(IAmazonGlue glue) : base(glue) { }

        /// <summary>
        /// Use this method to perform any DI configuration required
        /// </summary>
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddScoped<ITenureUpdatedUseCase, TenureUpdatedUseCase>();

            services.AddScoped<ITenureInfoApiGateway, TenureInfoApiGateway>();
            services.AddScoped<IGlueGateway, GlueGateway>();
            services.AddScoped<IKafkaGateway, KafkaGateway>();
            services.AddSingleton<IConfiguration>(Configuration);

            services.AddApiGateway();

            // register glue SDK
            services.AddSingleton<IAmazonGlue>(Glue);
            base.ConfigureServices(services);
        }


        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used
        /// to respond to SQS messages.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
        {
            // Do this in parallel???
            foreach (var message in evnt.Records)
            {
                await ProcessMessageAsync(message, context).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Method called to process every distinct message received.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        [LogCall(LogLevel.Information)]
        private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            context.Logger.LogLine($"Processing message {message.MessageId}");

            var entityEvent = JsonSerializer.Deserialize<EntityEventSns>(message.Body, _jsonOptions);

            using (Logger.BeginScope("CorrelationId: {CorrelationId}", entityEvent.CorrelationId))
            {
                try
                {
                    IMessageProcessing processor = entityEvent.CreateUseCaseForMessage(ServiceProvider);
                    if (processor != null)
                        await processor.ProcessMessageAsync(entityEvent).ConfigureAwait(false);
                    else
                        Logger.LogInformation($"No processors available for message so it will be ignored. " +
                            $"Message id: {message.MessageId}; type: {entityEvent.EventType}; version: {entityEvent.Version}; entity id: {entityEvent.EntityId}");

                    await processor.ProcessMessageAsync(entityEvent).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Exception processing message id: {message.MessageId}; type: {entityEvent.EventType}; entity id: {entityEvent.EntityId}");
                    throw; // AWS will handle retry/moving to the dead letter queue
                }
            }
        }
    }
}

using MtfhReportingDataListener.Boundary;
using MtfhReportingDataListener.UseCase.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MtfhReportingDataListener.Factories
{
    public static class UseCaseFactory
    {
        public static IMessageProcessing CreateUseCaseForMessage(this EntityEventSns entityEvent, IServiceProvider serviceProvider)
        {
            if (entityEvent is null) throw new ArgumentNullException(nameof(entityEvent));
            if (serviceProvider is null) throw new ArgumentNullException(nameof(serviceProvider));

            IMessageProcessing processor = null;
            switch (entityEvent.EventType)
            {
                case EventTypes.TenureUpdatedEvent:
                    {
                        processor = serviceProvider.GetService<ITenureUseCase>();
                        break;
                    }

                case EventTypes.TenureCreatedEvent:
                    {
                        processor = serviceProvider.GetService<ITenureUseCase>();
                        break;
                    }

                default:
                    return null;
            }

            return processor;
        }
    }
}

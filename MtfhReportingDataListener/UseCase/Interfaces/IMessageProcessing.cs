using MtfhReportingDataListener.Boundary;
using System.Threading.Tasks;

namespace MtfhReportingDataListener.UseCase.Interfaces
{
    public interface IMessageProcessing
    {
        Task ProcessMessageAsync(EntityEventSns message);
    }
}

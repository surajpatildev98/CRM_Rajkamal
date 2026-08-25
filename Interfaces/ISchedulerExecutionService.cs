namespace CRMWebServices.Interfaces
{
    public interface ISchedulerExecutionService
    {
        Task ProcessDueItemsAsync(CancellationToken cancellationToken);
    }
}

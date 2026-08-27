namespace CRMWebServices.Interfaces
{
    public interface ISchedulerExecutionService
    {
        Task ProcessDueItemsAsync(CancellationToken cancellationToken);
        Task SendEmployeeNotificationAsync(long? schedulerJobItemId,int employeeId,int entityId,string title,string message,string entityType,string recipientType);
        Task NotifyUnassignedLeadAsync(int leadId);
    }
}

namespace CRMWebServices.Interfaces
{
    public interface ISchedulerEmailComposer
    {
        string Compose(string actionType, string title, string message, string recipientType,IReadOnlyDictionary<string, string?>? data = null);
    }
}

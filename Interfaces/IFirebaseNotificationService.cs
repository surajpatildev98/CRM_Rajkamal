namespace CRMWebServices.Interfaces
{
    public interface IFirebaseNotificationService
    {
        Task<FirebaseSendResult> SendNotificationAsync(string deviceToken,string title,string message,Dictionary<string, string>? data = null);
    }
    public class FirebaseSendResult
    {
        public bool Success { get; set; }
        public string? MessageId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
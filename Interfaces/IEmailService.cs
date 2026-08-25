using CRMWebServices.Models;

namespace CRMWebServices.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendAsync(EmailMessage message,CancellationToken cancellationToken = default);
        Task<EmailSendResult> SendEmailAsync(string toEmail,string subject,string body);

        public class EmailSendResult
        {
            public bool Success { get; set; }

            public string? ErrorMessage { get; set; }
        }
    }


}

using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using static CRMWebServices.Interfaces.IEmailService;

namespace CRMWebServices.Services
{
    public class EmailService : IEmailService
    {
        private readonly IEmailConfigurationRepository _configurationRepository;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IEmailConfigurationRepository configurationRepository,
            ILogger<EmailService> logger)
        {
            _configurationRepository = configurationRepository;
            _logger = logger;
        }

        public async Task<bool> SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(message.ToEmail))
            {
                _logger.LogWarning("Email recipient is empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(message.Subject))
            {
                _logger.LogWarning("Email subject is empty for {Email}.",message.ToEmail);
                return false;
            }

            try
            {
                var config =await _configurationRepository.GetActiveConfiguration();

                if (config == null)
                {
                    _logger.LogError("No active SMTP configuration found.");
                    return false;
                }

                var email = new MimeMessage();

                email.From.Add(new MailboxAddress(config.FromName,config.FromEmail));
                email.To.Add(MailboxAddress.Parse(message.ToEmail));
                email.Subject = message.Subject;

                email.Body = new BodyBuilder
                {
                    HtmlBody = message.HtmlBody
                }.ToMessageBody();

                using var smtp = new SmtpClient();

                var security =config.SSL? SecureSocketOptions.StartTls: SecureSocketOptions.None;

                await smtp.ConnectAsync(
                    config.Host,
                    config.Port,
                    security,
                    cancellationToken);

                if (!string.IsNullOrWhiteSpace(config.Email))
                {
                    await smtp.AuthenticateAsync(
                        config.Email,
                        config.Password,
                        cancellationToken);
                }

                await smtp.SendAsync(email,cancellationToken);
                await smtp.DisconnectAsync(true,cancellationToken);
                _logger.LogInformation("Email sent successfully to {Email}.",message.ToEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Failed to send email to {Email}.",message.ToEmail);
                return false;
            }
        }

     
        // Scheduler adapter
     

        public async Task<EmailSendResult> SendEmailAsync(string toEmail,string subject,string body)
        {
            try
            {
                var message = new EmailMessage
                {
                    ToEmail = toEmail,
                    Subject = subject,
                    HtmlBody = body
                };

                var success =await SendAsync(message);

                return new EmailSendResult
                {
                    Success = success,
                    ErrorMessage = success
                        ? null
                        : "Email could not be sent."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Failed to send scheduler email to {Email}.",toEmail);

                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
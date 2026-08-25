namespace CRMWebServices.Models
{
    public class SmtpConfiguration
    {
        public int EmailConfigurationId { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Host { get; set; } = string.Empty;

        public int Port { get; set; }

        public bool SSL { get; set; }

        public string FromEmail { get; set; } = string.Empty;

        public string FromName { get; set; } = string.Empty;
    }

    public class EmailMessage
    {
        public string ToEmail { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string HtmlBody { get; set; } = string.Empty;
    }
}
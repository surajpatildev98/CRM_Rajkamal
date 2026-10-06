using System.Net;
using CRMWebServices.Interfaces;

namespace CRMWebServices.Services
{
    public class SchedulerEmailComposer : ISchedulerEmailComposer
    {
        private const string FallbackTemplate = "GenericNotification";
        private const string FallbackColor = "#333333";

        // actionType -> template file name
        private static readonly Dictionary<string, string> Templates = new(StringComparer.OrdinalIgnoreCase)
        {
            ["PROBATION_EXPIRY"] = "ProbationReminder",
            ["BIRTHDAY"] = "EmployeeBirthday",
            ["UNASSIGNED"] = "UnassignedLead",
            ["BIRTHDAY_WISH"] = "BirthdayWish",
            ["PROBATION_CONFIRMED"] = "ProbationConfirmed",
        };

        // moved from SchedulerExecutionService.ActionColors
        private static readonly Dictionary<string, string> Colors = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CREATED"] = "#6A1B9A",
            ["ASSIGNED"] = "#2E7D32",
            ["TRANSFERRED"] = "#1565C0",
            ["UNASSIGNED"] = "#C8161D",
            ["PROBATION_EXPIRY"] = "#C8161D",
            ["BIRTHDAY"] = "#C8161D",
            ["BIRTHDAY_WISH"] = "#C8161D",
            ["PROBATION_CONFIRMED"] = "#C8161D",
        };

        private readonly IEmailTemplateRenderer _renderer;
        //private readonly string _logoBlock;

        public SchedulerEmailComposer(IEmailTemplateRenderer renderer, IConfiguration config)
        {
            _renderer = renderer;

            string? baseUrl = config["EmailSettings:PublicBaseUrl"];
            //_logoBlock = string.IsNullOrWhiteSpace(baseUrl)
            //    ? ""
            //    : "<tr><td align='center' bgcolor='#ffffff' style='background-color:#ffffff;padding:20px 28px 16px 28px;border-bottom:1px solid #e5e7eb;'>" +
            //      $"<img src='{WebUtility.HtmlEncode(baseUrl.TrimEnd('/'))}/assets/companylogo.jpg' alt='Rajkamal Bar Scan Systems Pvt. Ltd.' width='200' style='display:block;height:auto;max-width:200px;border:0;' />" +
            //      "</td></tr>";
        }

        public string Compose(string actionType, string title, string message, string recipientType,
                              IReadOnlyDictionary<string, string?>? data = null)
        {
            string key = actionType ?? "";

            string template = Templates.TryGetValue(key, out var t) ? t : FallbackTemplate;

            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Title"] = title,
                ["Message"] = message,
                ["Preheader"] = message,
                ["HeaderColor"] = Colors.TryGetValue(key, out var c) ? c : FallbackColor,
                //["LogoBlock"] = _logoBlock,
                ["RecipientRole"] = RoleLabel(recipientType),
                ["CompanyName"] = "Rajkamal BarScan Systems Pvt. Ltd.",
            };

            if (data != null)
                foreach (var kv in data)
                    values[kv.Key] = kv.Value;

            return _renderer.Render(template, values);
        }

        private static string RoleLabel(string? recipientType) =>
            (recipientType ?? "").ToUpperInvariant() switch
            {
                "MANAGER" => "Reporting Manager",
                "HR_MANAGER" => "HR Manager",
                "CREATOR" => "Lead Creator",
                _ => "Recipient"
            };
    }
}
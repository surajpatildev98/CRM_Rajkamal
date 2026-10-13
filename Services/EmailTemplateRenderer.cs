using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using CRMWebServices.Interfaces;

namespace CRMWebServices.Services
{
    public class EmailTemplateRenderer : IEmailTemplateRenderer
    {
        // {{{Raw}}} or {{Encoded}}
        private static readonly Regex TokenRegex = new(@"\{\{\{\s*(\w+)\s*\}\}\}|\{\{\s*(\w+)\s*\}\}", RegexOptions.Compiled);
        private static readonly Regex IfRegex = new(@"\{\{#if\s+(\w+)\s*\}\}(.*?)\{\{/if\}\}", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly string TemplateRoot = Path.Combine(AppContext.BaseDirectory, "Templates", "Email");
        private readonly ConcurrentDictionary<string, string> _cache = new();
        private readonly ILogger<EmailTemplateRenderer> _logger;

        private const string LogoBlock =
            "<tr><td align='center' bgcolor='#C8161D' style='background-color:#C8161D;padding:14px 20px;'>" +
            "<table role='presentation' align='center' cellpadding='0' cellspacing='0' border='0'><tr>" +
            "<td bgcolor='#ffffff' style='background-color:#ffffff;border-radius:6px;padding:8px 12px;'>" +
            "<img src='cid:companylogo' alt='Rajkamal Bar Scan Systems Pvt. Ltd.' width='180' style='display:block;height:auto;max-width:180px;border:0;' />" +
            "</td></tr></table>" +
            "</td></tr>";

        public EmailTemplateRenderer(ILogger<EmailTemplateRenderer> logger)
        {
            _logger = logger;
        }

        public string Render(string templateName, IReadOnlyDictionary<string, string?> values)
        {
            var all = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["HeaderLabel"] = "CRM Notification",
                ["HeaderColor"] = "#1e3a8a",
                ["LogoBlock"] = LogoBlock,
                ["Preheader"] = ""
            };

            foreach (var kv in values) all[kv.Key] = kv.Value;

            all["Content"] = Replace(Load(templateName), all);

            return Replace(Load("_Layout"), all);
        }

        private string Load(string name) =>
            _cache.GetOrAdd(name, n =>
            {
                string path = Path.Combine(TemplateRoot, n + ".html");
                if (!File.Exists(path)) throw new FileNotFoundException($"Email template not found: {path}");
                return File.ReadAllText(path);
            });
       
        private string Replace(string template, IReadOnlyDictionary<string, string?> values)
        {
            var lookup = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase);

            // {{#if Key}} ... {{/if}} is kept only when Key has a non-empty value
            template = IfRegex.Replace(template, m =>
                lookup.TryGetValue(m.Groups[1].Value, out var v) && !string.IsNullOrWhiteSpace(v)
                    ? m.Groups[2].Value
                    : "");

            return TokenRegex.Replace(template, m =>
            {
                bool raw = m.Groups[1].Success;
                string key = raw ? m.Groups[1].Value : m.Groups[2].Value;

                if (!lookup.TryGetValue(key, out var value))
                {
                    _logger.LogWarning("Email template token '{Token}' has no value.", key);
                    return "";
                }

                return raw ? value ?? "" : WebUtility.HtmlEncode(value ?? "");
            });
        }
    }
}
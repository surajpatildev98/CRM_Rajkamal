using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using CRMWebServices.Interfaces;

namespace CRMWebServices.Services
{
    public class EmailTemplateRenderer : IEmailTemplateRenderer
    {
        // {{{Raw}}} or {{Encoded}}
        private static readonly Regex TokenRegex =
            new(@"\{\{\{\s*(\w+)\s*\}\}\}|\{\{\s*(\w+)\s*\}\}", RegexOptions.Compiled);

        private static readonly string TemplateRoot =
            Path.Combine(AppContext.BaseDirectory, "Templates", "Email");

        private readonly ConcurrentDictionary<string, string> _cache = new();
        private readonly ILogger<EmailTemplateRenderer> _logger;

        public EmailTemplateRenderer(ILogger<EmailTemplateRenderer> logger)
        {
            _logger = logger;
        }

        public string Render(string templateName, IReadOnlyDictionary<string, string?> values)
        {
            string body = Replace(Load(templateName), values);

            var layoutValues = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase)
            {
                ["Content"] = body
            };

            return Replace(Load("_Layout"), layoutValues);
        }

        private string Load(string name) =>
            _cache.GetOrAdd(name, n =>
            {
                string path = Path.Combine(TemplateRoot, n + ".html");
                if (!File.Exists(path))
                    throw new FileNotFoundException($"Email template not found: {path}");
                return File.ReadAllText(path);
            });

        private string Replace(string template, IReadOnlyDictionary<string, string?> values)
        {
            var lookup = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase);

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
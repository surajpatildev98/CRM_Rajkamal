namespace CRMWebServices.Interfaces
{
    public interface IEmailTemplateRenderer
    {
        /// <summary>
        /// {{Token}} is HTML-encoded, {{{Token}}} is inserted raw.
        /// </summary>
        string Render(string templateName, IReadOnlyDictionary<string, string?> values);
    }
}
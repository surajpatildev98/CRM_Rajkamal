using CRMWebServices.Models;

namespace CRMWebServices.Interfaces
{
    public interface IEmailConfigurationRepository
    {
        Task<SmtpConfiguration?> GetActiveConfiguration();
    }
}

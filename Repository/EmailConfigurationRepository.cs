using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CRMWebServices.Repository
{
    public class EmailConfigurationRepository
        : IEmailConfigurationRepository
    {
        private readonly IDatabaseService _database;

        public EmailConfigurationRepository(
            IDatabaseService database)
        {
            _database = database;
        }

        public async Task<SmtpConfiguration?> GetActiveConfiguration()
        {
            var dt = await _database.ExecuteDataTableAsync("USP_CRM_EmailConfiguration",
                new SqlParameter("@Status","GET_ACTIVE")
            );

            if (dt == null || dt.Rows.Count == 0)
                return null;

            DataRow row = dt.Rows[0];

            return new SmtpConfiguration
            {
                EmailConfigurationId =Convert.ToInt32(row["EmailConfigurationId"]),
                Email =row["Email"]?.ToString() ?? string.Empty,
                Password =row["Password"]?.ToString() ?? string.Empty,
                Host =row["Host"]?.ToString() ?? string.Empty,
                Port =Convert.ToInt32(row["Port"]),
                SSL =Convert.ToBoolean(row["SSL"]),
                FromEmail =row["FromEmail"]?.ToString() ?? string.Empty,
                FromName =row["FromName"]?.ToString() ?? string.Empty
            };
        }
    }
}
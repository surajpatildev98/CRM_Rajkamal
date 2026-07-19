using CRMWebServices.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CRMWebServices.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IDatabaseService _database;

        public AuthRepository(IDatabaseService database)
        {
            _database = database;
        }

        public async Task<DataRow> GetEmployeeForLogin(string loginId)
        {

            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_Authentication",
                new SqlParameter("@Status", "LOGIN"),
                new SqlParameter("@LoginId", loginId));

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }
    }
}
using Microsoft.Data.SqlClient;

namespace CRMWebServices.Data
{
    public class DBConnection
    {
        private readonly IConfiguration _configuration;

        public DBConnection(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection"));
        }
    }
}

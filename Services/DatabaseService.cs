using Microsoft.Data.SqlClient;
using System.Data;

namespace CRMWebServices.Interfaces
{
    public interface IDatabaseService
    {
        Task<DataTable> ExecuteDataTableAsync(string storedProcedure,params SqlParameter[] parameters);
        Task<int> ExecuteNonQueryAsync(string storedProcedure,params SqlParameter[] parameters);
        Task<object?> ExecuteScalarAsync(string storedProcedure, params SqlParameter[] parameters);
        Task<DataSet> ExecuteDataSetAsync(string storedProcedure, params SqlParameter[] parameters);
    }
}
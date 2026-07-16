using CRMWebServices.Data;
using CRMWebServices.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CRMWebServices.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly DBConnection _dbConnection;

        public DatabaseService(DBConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<DataTable> ExecuteDataTableAsync(
            string storedProcedure,
            params SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();

            using (SqlConnection con = _dbConnection.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(storedProcedure, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (parameters != null && parameters.Length > 0)
                        cmd.Parameters.AddRange(parameters);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        await Task.Run(() => da.Fill(dt));
                    }
                }
            }

            return dt;
        }

        //public async Task<int> ExecuteNonQueryAsync(
        //    string storedProcedure,
        //    params SqlParameter[] parameters)
        //{
        //    using (SqlConnection con = _dbConnection.GetConnection())
        //    {
        //        await con.OpenAsync();

        //        using (SqlCommand cmd = new SqlCommand(storedProcedure, con))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;

        //            if (parameters != null && parameters.Length > 0)
        //                cmd.Parameters.AddRange(parameters);

        //            return await cmd.ExecuteNonQueryAsync();
        //        }
        //    }
        //}

        public async Task<int> ExecuteNonQueryAsync(string storedProcedure,params SqlParameter[] parameters)
        {
            using (SqlConnection con = _dbConnection.GetConnection())
            {
                await con.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(storedProcedure, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (parameters != null && parameters.Length > 0)
                        cmd.Parameters.AddRange(parameters);

                    int result = await cmd.ExecuteNonQueryAsync();

                    Console.WriteLine("Rows Affected : " + result);

                    return result;
                }
            }
        }

        public async Task<object?> ExecuteScalarAsync(
            string storedProcedure,
            params SqlParameter[] parameters)
        {
            using (SqlConnection con = _dbConnection.GetConnection())
            {
                await con.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(storedProcedure, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (parameters != null && parameters.Length > 0)
                        cmd.Parameters.AddRange(parameters);

                    return await cmd.ExecuteScalarAsync();
                }
            }
        }

        public async Task<DataSet> ExecuteDataSetAsync(
    string storedProcedure,
    params SqlParameter[] parameters)
        {
            DataSet ds = new DataSet();

            using (SqlConnection con = _dbConnection.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(storedProcedure, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (parameters != null && parameters.Length > 0)
                        cmd.Parameters.AddRange(parameters);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        await Task.Run(() => da.Fill(ds));
                    }
                }
            }

            return ds;
        }
    }
}
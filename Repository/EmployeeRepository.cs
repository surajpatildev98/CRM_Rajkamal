using BCrypt.Net;
using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.Data.SqlClient;

namespace CRMWebServices.Repository
{
    public class EmployeeRepository
    {
        private readonly IDatabaseService _database;

        public EmployeeRepository(IDatabaseService database)
        {
            _database = database;
        }

        public async Task<ApiResponse> Register(EmployeeRegisterRequest model)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "REGISTER"),
                new SqlParameter("@FirstName", model.FirstName),
                new SqlParameter("@MiddleName", (object?)model.MiddleName ?? DBNull.Value),
                new SqlParameter("@LastName", (object?)model.LastName ?? DBNull.Value),
                new SqlParameter("@OfficialEmail", model.OfficialEmail),
                new SqlParameter("@PersonalEmail", (object?)model.PersonalEmail ?? DBNull.Value),
                new SqlParameter("@Mobile", model.Mobile),
                new SqlParameter("@AlternateMobile", (object?)model.AlternateMobile ?? DBNull.Value),
                new SqlParameter("@Gender", (object?)model.Gender ?? DBNull.Value),
                new SqlParameter("@DOB", (object?)model.DOB ?? DBNull.Value),
                new SqlParameter("@DOJ", (object?)model.DOJ ?? DBNull.Value),
                //new SqlParameter("@LoginId", model.UserName),
                new SqlParameter("@Password", passwordHash),
                new SqlParameter("@RefDesignationId", (object?)model.RefDesignationId ?? DBNull.Value),
                new SqlParameter("@ReportingManagerId", (object?)model.ReportingManagerId ?? DBNull.Value),
                new SqlParameter("@RefOfficeId", model.RefOfficeId),
                new SqlParameter("@RefDepartmentId", model.RefDepartmentId),
                new SqlParameter("@RefRoleId", model.RefRoleId),
                new SqlParameter("@CreatedBy", (object?)model.CreatedBy ?? DBNull.Value)
            );

            var response = new ApiResponse();

            if (dt == null || dt.Rows.Count == 0)
            {
                response.Status = false;
                response.Message = "No response returned from database.";
                return response;
            }

            var row = dt.Rows[0];
            bool status = Convert.ToInt32(row["Status"]) == 1;

            response.Status = status;
            response.Message = row["Message"]?.ToString();
            response.Data = status
                ? new { EmployeeId = Convert.ToInt32(row["EmployeeId"]), EmployeeCode = row["EmployeeCode"]?.ToString(), LoginId = row["LoginId"]?.ToString() }
                : null;

            return response;
        }
    }
}
using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Services;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using static CRMWebServices.Models.PermissionModels;
namespace CRMWebServices.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IDatabaseService _database;
        private readonly ILogger<AuthRepository> _logger;

        public AuthRepository(IDatabaseService database, ILogger<AuthRepository> logger)
        {
            _database = database;
            _logger = logger;
        }

        #region // IAuthRepository Implementation used for authentication and authorization     

        // Repository — was likely ExecuteDataTableAsync returning one row; now needs the second table too
        public async Task<(DataRow? EmployeeRow, List<ModuleAccessInfo> Modules)> GetEmployeeForLogin(string loginId)
        {
            var ds = await _database.ExecuteDataSetAsync("USP_ORG_Authentication",
                new SqlParameter("@Status", "LOGIN"),
                new SqlParameter("@LoginId", loginId)
            );

            if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                return (null, new List<ModuleAccessInfo>());

            var employeeRow = ds.Tables[0].Rows[0];

            var modules = new List<ModuleAccessInfo>();

            if (ds.Tables.Count > 1)
            {
                foreach (DataRow r in ds.Tables[1].Rows)
                {
                    var module = new ModuleAccessInfo
                    {
                        ModuleId = Convert.ToInt32(r["ModuleId"]),
                        ModuleCode = r["ModuleCode"]?.ToString(),
                        ModuleName = r["ModuleName"]?.ToString(),
                        IconName = r["IconName"] == DBNull.Value ? null : r["IconName"].ToString(),
                        IsAllowed = Convert.ToBoolean(r["IsAllowed"])
                    };

                    if (r["PermissionsJson"] != DBNull.Value)
                    {
                        var json = r["PermissionsJson"]?.ToString();
                        if (!string.IsNullOrEmpty(json))
                        {
                            var parsed = JsonConvert.DeserializeObject<List<dynamic>>(json);
                            module.Permissions = parsed.Select(p => new PermissionAccessInfo
                            {
                                PermissionId = Convert.ToInt32(p.PermissionId),
                                PermissionCode = p.PermissionCode?.ToString(),
                                PermissionName = p.PermissionName?.ToString(),
                                IsAllowed = Convert.ToBoolean(p.IsAllowed)
                            }).ToList();
                        }
                    }

                    modules.Add(module);
                }
            }

            return (employeeRow, modules);
        }
        #endregion

        /*public async Task<ApiResponse> RegisterEmployeeDevice(int employeeId,RegisterEmployeeDeviceRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_ORG_Authentication",

                    new SqlParameter("@Status","REGISTER_EMPLOYEE_DEVICE"),
                    new SqlParameter("@EmployeeId",employeeId),
                    new SqlParameter("@DeviceId",request.DeviceId),
                    new SqlParameter("@DeviceToken",request.DeviceToken),
                    new SqlParameter("@DeviceType",(object?)request.DeviceType ?? DBNull.Value),
                    new SqlParameter("@AppVersion",(object?)request.AppVersion ?? DBNull.Value)
                );

                if (dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Device registration failed."
                    };
                }

                return new ApiResponse
                {
                    Status = Convert.ToBoolean(dt.Rows[0]["Status"]),
                    Message = dt.Rows[0]["Message"]?.ToString()
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }*/

        public async Task<ApiResponse> RegisterEmployeeDevice(
    int employeeId,
    RegisterEmployeeDeviceRequest request)
        {
            try
            {
                // STEP 2: Check what values are coming from the app
                _logger.LogInformation(
                    "REGISTER DEVICE - EmployeeId: {EmployeeId}",
                    employeeId);

                _logger.LogInformation(
                    "REGISTER DEVICE - DeviceId: {DeviceId}",
                    request.DeviceId);

                _logger.LogInformation(
                    "REGISTER DEVICE - DeviceType: {DeviceType}",
                    request.DeviceType);

                _logger.LogInformation(
                    "REGISTER DEVICE - AppVersion: {AppVersion}",
                    request.AppVersion);

                _logger.LogInformation(
                    "REGISTER DEVICE - DeviceToken exists: {HasToken}",
                    !string.IsNullOrWhiteSpace(request.DeviceToken));


                var dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_Authentication",

                    new SqlParameter(
                        "@Status",
                        "REGISTER_EMPLOYEE_DEVICE"),

                    new SqlParameter(
                        "@EmployeeId",
                        employeeId),

                    new SqlParameter(
                        "@DeviceId",
                        (object?)request.DeviceId ?? DBNull.Value),

                    new SqlParameter(
                        "@DeviceToken",
                        (object?)request.DeviceToken ?? DBNull.Value),

                    new SqlParameter(
                        "@DeviceType",
                        (object?)request.DeviceType ?? DBNull.Value),

                    new SqlParameter(
                        "@AppVersion",
                        (object?)request.AppVersion ?? DBNull.Value)
                );


                if (dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Device registration failed."
                    };
                }


                return new ApiResponse
                {
                    Status = Convert.ToBoolean(
                        dt.Rows[0]["Status"]),

                    Message = dt.Rows[0]["Message"]?.ToString()
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error while registering device for EmployeeId {EmployeeId}",
                    employeeId);

                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while registering device for EmployeeId {EmployeeId}",
                    employeeId);

                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }
    }
}
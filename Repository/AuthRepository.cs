using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Services;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using static CRMWebServices.Models.LogoutModels;
using static CRMWebServices.Models.PermissionModels;
namespace CRMWebServices.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IDatabaseService _database;
        private readonly ILogger<AuthRepository> _logger;
        private readonly IFirebaseNotificationService _firebaseNotificationService;

        public AuthRepository(IDatabaseService database, ILogger<AuthRepository> logger, IFirebaseNotificationService firebaseNotificationService)
        {
            _database = database;
            _logger = logger;
            _firebaseNotificationService = firebaseNotificationService;
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

        public async Task<ApiResponse> ForceLogoutDevice(int employeeId,string deviceId)
        {
            try
            {
                _logger.LogInformation(
                    "FORCE LOGOUT - EmployeeId: {EmployeeId}, DeviceId: {DeviceId}",
                    employeeId,
                    deviceId);

                var dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_Authentication",

                    new SqlParameter(
                        "@Status",
                        "FORCE_LOGOUT_DEVICE"),

                    new SqlParameter(
                        "@EmployeeId",
                        employeeId),

                    new SqlParameter(
                        "@DeviceId",
                        deviceId)
                );


                if (dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Old device not found."
                    };
                }


                bool status = Convert.ToBoolean(
                    dt.Rows[0]["Status"]);

                string message =
                    dt.Rows[0]["Message"]?.ToString()
                    ?? "Force logout failed.";


                if (!status)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = message
                    };
                }


                // ------------------------------------------
                // Get OLD DEVICE Firebase token
                // ------------------------------------------

                string? oldDeviceToken = null;

                if (dt.Columns.Contains("DeviceToken") &&
                    dt.Rows[0]["DeviceToken"] != DBNull.Value)
                {
                    oldDeviceToken =
                        dt.Rows[0]["DeviceToken"]?.ToString();
                }


                _logger.LogInformation(
                    "FORCE LOGOUT - Old Device Token exists: {HasToken}",
                    !string.IsNullOrWhiteSpace(oldDeviceToken));


                // ------------------------------------------
                // Send FORCE_LOGOUT notification
                // ------------------------------------------

                if (!string.IsNullOrWhiteSpace(oldDeviceToken))
                {
                    var notificationResult =
                        await _firebaseNotificationService.SendNotificationAsync(
                            oldDeviceToken,
                            "Session Expired",
                            "Your account has been logged in on another device. You have been logged out.",
                            new Dictionary<string, string>
                            {
                        { "type", "FORCE_LOGOUT" },
                        { "employeeId", employeeId.ToString() },
                        { "deviceId", deviceId }
                            });

                    if (notificationResult.Success)
                    {
                        _logger.LogInformation(
                            "FORCE LOGOUT notification sent successfully. MessageId: {MessageId}",
                            notificationResult.MessageId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "FORCE LOGOUT notification failed. Error: {Error}",
                            notificationResult.ErrorMessage);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "FORCE LOGOUT - Old device has no Firebase token.");
                }


                return new ApiResponse
                {
                    Status = true,
                    Message = "Old device logged out successfully."
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error while force logging out EmployeeId {EmployeeId}, DeviceId {DeviceId}",
                    employeeId,
                    deviceId);

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
                    "Error while force logging out EmployeeId {EmployeeId}, DeviceId {DeviceId}",
                    employeeId,
                    deviceId);

                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ApiResponse> Logout(int employeeId,string deviceId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_Authentication",

                    new SqlParameter(
                        "@Status",
                        "LOGOUT_DEVICE"),

                    new SqlParameter(
                        "@EmployeeId",
                        employeeId),

                    new SqlParameter(
                        "@DeviceId",
                        deviceId)
                );


                if (dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Logout failed."
                    };
                }


                bool status = Convert.ToBoolean(
                    dt.Rows[0]["Status"]);


                string message =
                    dt.Rows[0]["Message"]?.ToString()
                    ?? "Logout failed.";


                return new ApiResponse
                {
                    Status = status,
                    Message = message
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error while logging out EmployeeId {EmployeeId}, DeviceId {DeviceId}",
                    employeeId,
                    deviceId);

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
                    "Error while logging out EmployeeId {EmployeeId}, DeviceId {DeviceId}",
                    employeeId,
                    deviceId);

                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ActiveDeviceInfo?> CheckActiveDevice(int employeeId,string deviceId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_Authentication",

                    new SqlParameter(
                        "@Status",
                        "CHECK_ACTIVE_DEVICE"),

                    new SqlParameter(
                        "@EmployeeId",
                        employeeId),

                    new SqlParameter(
                        "@DeviceId",
                        deviceId)
                );


                // No active device
                if (dt.Rows.Count == 0)
                {
                    return null;
                }


                DataRow row = dt.Rows[0];


                return new ActiveDeviceInfo
                {
                    EmployeeDeviceId = Convert.ToInt32(
                        row["EmployeeDeviceId"]),

                    Ref_EmployeeId = Convert.ToInt32(
                        row["Ref_EmployeeId"]),

                    ActiveDeviceId = row["ActiveDeviceId"]?.ToString(),

                    ActiveDeviceToken =
                        row["ActiveDeviceToken"] == DBNull.Value
                            ? null
                            : row["ActiveDeviceToken"]?.ToString(),

                    DeviceType =
                        row["DeviceType"] == DBNull.Value
                            ? null
                            : row["DeviceType"]?.ToString(),

                    AppVersion =
                        row["AppVersion"] == DBNull.Value
                            ? null
                            : row["AppVersion"]?.ToString(),

                    IsActive = Convert.ToBoolean(
                        row["IsActive"]),

                    IsLoggedIn = Convert.ToBoolean(row["IsLoggedIn"]),

                    LastSeenOn =
                        row["LastSeenOn"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(row["LastSeenOn"]),

                    IsSameDevice = Convert.ToBoolean(
                        row["IsSameDevice"])
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error while checking active device for EmployeeId {EmployeeId}",
                    employeeId);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while checking active device for EmployeeId {EmployeeId}",
                    employeeId);

                throw;
            }
        }

        #region // Password Reset - Generate OTP
        public async Task<ApiResponse> GenerateResetOtp(int employeeId, string otpHash)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_Authentication",
                    new SqlParameter("@Status", "GENERATE_RESET_OTP"),
                    new SqlParameter("@EmployeeId", employeeId),
                    new SqlParameter("@OTPHash", otpHash)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "No response returned from database."
                    };
                }

                var row = dt.Rows[0];

                return new ApiResponse
                {
                    Status = row["Status"] != DBNull.Value && Convert.ToBoolean(row["Status"]),
                    Message = row["Message"]?.ToString() ?? string.Empty
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region // Password Reset - Get OTP
        public async Task<DataRow?> GetResetOtp(int employeeId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_Authentication",
                    new SqlParameter("@Status", "GET_RESET_OTP"),
                    new SqlParameter("@EmployeeId", employeeId)
                );

                if (dt == null || dt.Rows.Count == 0)
                    return null;

                return dt.Rows[0];
            }
            catch
            {
                throw;
            }
        }
        #endregion

        #region // Password Reset - Increment OTP Attempt
        public async Task<ApiResponse> IncrementOtpAttempt(int employeeId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_Authentication",
                    new SqlParameter("@Status", "INCREMENT_OTP_ATTEMPT"),
                    new SqlParameter("@EmployeeId", employeeId)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "No response returned from database."
                    };
                }

                var row = dt.Rows[0];

                return new ApiResponse
                {
                    Status = row["Status"] != DBNull.Value && Convert.ToBoolean(row["Status"]),
                    Message = row["Message"]?.ToString() ?? string.Empty
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region // Password Reset - Verify OTP
        public async Task<ApiResponse> VerifyResetOtp(int employeeId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_Authentication",
                    new SqlParameter("@Status", "VERIFY_RESET_OTP"),
                    new SqlParameter("@EmployeeId", employeeId)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "No response returned from database."
                    };
                }

                var row = dt.Rows[0];

                bool status =
                    row["Status"] != DBNull.Value &&
                    Convert.ToBoolean(row["Status"]);

                return new ApiResponse
                {
                    Status = status,
                    Message = row["Message"]?.ToString() ?? string.Empty,
                    Data = status
                        ? new
                        {
                            ResetToken = row["ResetToken"] != DBNull.Value
                                ? Guid.Parse(row["ResetToken"].ToString()!)
                                : Guid.Empty
                        }
                        : null
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region // Password Reset - Reset Password
        public async Task<ApiResponse> ResetPassword(int employeeId, Guid resetToken, string newPasswordHash)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_ORG_Authentication",
                    new SqlParameter("@Status", "RESET_PASSWORD"),
                    new SqlParameter("@EmployeeId", employeeId),
                    new SqlParameter("@ResetToken", resetToken),
                    new SqlParameter("@NewPassword", newPasswordHash)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "No response returned from database."
                    };
                }

                var row = dt.Rows[0];

                return new ApiResponse
                {
                    Status = row["Status"] != DBNull.Value && Convert.ToBoolean(row["Status"]),
                    Message = row["Message"]?.ToString() ?? string.Empty
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region // Password Reset - Get Employee By Reset Token
        public async Task<int?> GetResetEmployeeByToken(Guid resetToken)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_Authentication",
                    new SqlParameter("@Status", "GET_RESET_EMPLOYEE"),
                    new SqlParameter("@ResetToken", resetToken)
                );

                if (dt == null || dt.Rows.Count == 0)
                    return null;

                if (dt.Rows[0]["EmployeeId"] == DBNull.Value)
                    return null;

                return Convert.ToInt32(dt.Rows[0]["EmployeeId"]);
            }
            catch
            {
                throw;
            }
        }
        #endregion
    }
}
using CRMWebServices.Data;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Repository
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<PermissionRepository> _logger;

        public PermissionRepository(IDatabaseService databaseService, ILogger<PermissionRepository> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        #region GetUserPermissions for EmployeeId
        public async Task<List<UserPermission>> GetUserPermissions(int employeeId)
        {
            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@Status", "GET_USER_PERMISSIONS"),
                    new SqlParameter("@EmployeeId", employeeId),
                    new SqlParameter("@PermissionCode", DBNull.Value),
                    new SqlParameter("@ResourceType", DBNull.Value),
                    new SqlParameter("@ResourceId", DBNull.Value),
                    new SqlParameter("@Action", DBNull.Value)
                };

                var dt = await _databaseService.ExecuteDataTableAsync("USP_CRM_PermissionManagement", parameters);

                var result = new List<UserPermission>();
                foreach (DataRow row in dt.Rows)
                {
                    result.Add(new UserPermission
                    {
                        PermissionCode = row["PermissionCode"]?.ToString() ?? string.Empty,
                        PermissionName = row["PermissionName"]?.ToString() ?? string.Empty,
                        ModuleName = row["ModuleName"]?.ToString() ?? string.Empty,
                        Description = row["Description"]?.ToString() ?? string.Empty,
                        IsGranted = row["IsGranted"] != DBNull.Value && Convert.ToBoolean(row["IsGranted"]),
                        PermissionId = row["PermissionId"] != DBNull.Value ? Convert.ToInt32(row["PermissionId"]) : 0
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user permissions for EmployeeId: {EmployeeId}", employeeId);
                return new List<UserPermission>();
            }
        }
        #endregion

        #region Check if user has a specific permission
        public async Task<bool> UserHasPermission(int employeeId, string permissionCode)
        {
            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@Status", "CHECK_PERMISSION"),
                    //new SqlParameter("@Status", "CHECK_PERMISSION_NEW120826"),   // was "CHECK_PERMISSION"
                    new SqlParameter("@EmployeeId", employeeId),
                    new SqlParameter("@PermissionCode", permissionCode),
                    new SqlParameter("@ResourceType", DBNull.Value),
                    new SqlParameter("@ResourceId", DBNull.Value),
                    new SqlParameter("@Action", DBNull.Value)
                };

                var result = await _databaseService.ExecuteScalarAsync("USP_CRM_PermissionManagement", parameters);
                return result != null && result != DBNull.Value && Convert.ToBoolean(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {PermissionCode} for EmployeeId: {EmployeeId}",
                    permissionCode, employeeId);
                return false;
            }
        }
        #endregion

        #region Check if user has multiple permissions
        public async Task<Dictionary<string, bool>> UserHasMultiplePermissions(int employeeId, string[] permissionCodes)
        {
            var result = new Dictionary<string, bool>();

            if (permissionCodes == null || permissionCodes.Length == 0)
                return result;

            try
            {
                var permissionList = string.Join(",", permissionCodes);

                var parameters = new[]
                {
                    new SqlParameter("@Status", "CHECK_MULTIPLE_PERMISSIONS"),
                    new SqlParameter("@EmployeeId", employeeId),
                    new SqlParameter("@PermissionCode", permissionList),
                    new SqlParameter("@ResourceType", DBNull.Value),
                    new SqlParameter("@ResourceId", DBNull.Value),
                    new SqlParameter("@Action", DBNull.Value)
                };

                var dt = await _databaseService.ExecuteDataTableAsync("USP_CRM_PermissionManagement", parameters);

                foreach (DataRow row in dt.Rows)
                {
                    var code = row["PermissionCode"]?.ToString() ?? string.Empty;
                    var hasPermission = row["HasPermission"] != DBNull.Value && Convert.ToBoolean(row["HasPermission"]);
                    if (!string.IsNullOrEmpty(code))
                    {
                        result[code] = hasPermission;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking multiple permissions for EmployeeId: {EmployeeId}", employeeId);
                return result;
            }
        }
        #endregion

        #region Get the owner of a specific resource
        public async Task<ResourceOwner> GetResourceOwner(string resourceType, int resourceId)
        {
            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@Status", "GET_RESOURCE_OWNER"),
                    new SqlParameter("@EmployeeId", DBNull.Value),
                    new SqlParameter("@PermissionCode", DBNull.Value),
                    new SqlParameter("@ResourceType", resourceType),
                    new SqlParameter("@ResourceId", resourceId),
                    new SqlParameter("@Action", DBNull.Value)
                };

                var dt = await _databaseService.ExecuteDataTableAsync("USP_CRM_PermissionManagement", parameters);

                if (dt.Rows.Count == 0)
                    return null;

                var row = dt.Rows[0];
                return new ResourceOwner
                {
                    ResourceId = row["ResourceId"] != DBNull.Value ? Convert.ToInt32(row["ResourceId"]) : 0,
                    CreatedBy = row["CreatedBy"] == DBNull.Value ? null : (int?)Convert.ToInt32(row["CreatedBy"]),
                    AssignedEmployeeId = row["AssignedEmployeeId"] == DBNull.Value ? null : (int?)Convert.ToInt32(row["AssignedEmployeeId"]),
                    ResourceType = row["ResourceType"]?.ToString() ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting resource owner for {ResourceType}:{ResourceId}",
                    resourceType, resourceId);
                return null;
            }
        }
        #endregion

        #region Get user role permissions
        public async Task<List<UserRolePermission>> GetUserRolePermissions(int employeeId)
        {
            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@Status", "GET_USER_ROLES_PERMISSIONS"),
                    new SqlParameter("@EmployeeId", employeeId),
                    new SqlParameter("@PermissionCode", DBNull.Value),
                    new SqlParameter("@ResourceType", DBNull.Value),
                    new SqlParameter("@ResourceId", DBNull.Value),
                    new SqlParameter("@Action", DBNull.Value)
                };

                var dt = await _databaseService.ExecuteDataTableAsync("USP_CRM_PermissionManagement", parameters);

                var result = new List<UserRolePermission>();
                foreach (DataRow row in dt.Rows)
                {
                    result.Add(new UserRolePermission
                    {
                        RoleId = row["RoleId"] != DBNull.Value ? Convert.ToInt32(row["RoleId"]) : 0,
                        RoleCode = row["RoleCode"]?.ToString() ?? string.Empty,
                        RoleName = row["RoleName"]?.ToString() ?? string.Empty,
                        RoleDescription = row["RoleDescription"]?.ToString() ?? string.Empty,
                        IsPrimaryRole = row["IsPrimaryRole"] != DBNull.Value && Convert.ToBoolean(row["IsPrimaryRole"]),
                        PermissionCode = row["PermissionCode"]?.ToString(),
                        PermissionName = row["PermissionName"]?.ToString(),
                        ModuleName = row["ModuleName"]?.ToString(),
                        PermissionDescription = row["PermissionDescription"]?.ToString(),
                        IsAllowed = row["IsAllowed"] != DBNull.Value && Convert.ToBoolean(row["IsAllowed"]),
                        PermissionStatus = row["PermissionStatus"]?.ToString()
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user role permissions for EmployeeId: {EmployeeId}", employeeId);
                return new List<UserRolePermission>();
            }
        }
        #endregion

        #region Get user modules with total permissions
        public async Task<List<ModulePermission>> GetUserModules(int employeeId)
        {
            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@Status", "GET_USER_MODULES"),
                    new SqlParameter("@EmployeeId", employeeId),
                    new SqlParameter("@PermissionCode", DBNull.Value),
                    new SqlParameter("@ResourceType", DBNull.Value),
                    new SqlParameter("@ResourceId", DBNull.Value),
                    new SqlParameter("@Action", DBNull.Value)
                };

                var dt = await _databaseService.ExecuteDataTableAsync("USP_CRM_PermissionManagement", parameters);

                var result = new List<ModulePermission>();
                foreach (DataRow row in dt.Rows)
                {
                    result.Add(new ModulePermission
                    {
                        ModuleName = row["ModuleName"]?.ToString() ?? string.Empty,
                        ModuleDisplayName = row["ModuleDisplayName"]?.ToString() ?? string.Empty,
                        TotalPermissions = row["TotalPermissions"] != DBNull.Value ? Convert.ToInt32(row["TotalPermissions"]) : 0
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user modules for EmployeeId: {EmployeeId}", employeeId);
                return new List<ModulePermission>();
            }
        }
        #endregion

        #region Get all permissions in the system
        public async Task<List<PermissionDto>> GetAllPermissions()
        {
            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@Status", "GET_ALL_PERMISSIONS"),
                    new SqlParameter("@EmployeeId", DBNull.Value),
                    new SqlParameter("@PermissionCode", DBNull.Value),
                    new SqlParameter("@ResourceType", DBNull.Value),
                    new SqlParameter("@ResourceId", DBNull.Value),
                    new SqlParameter("@Action", DBNull.Value)
                };

                var dt = await _databaseService.ExecuteDataTableAsync("USP_CRM_PermissionManagement", parameters);

                var result = new List<PermissionDto>();
                foreach (DataRow row in dt.Rows)
                {
                    result.Add(new PermissionDto
                    {
                        PermissionId = row["PermissionId"] != DBNull.Value ? Convert.ToInt32(row["PermissionId"]) : 0,
                        ModuleName = row["ModuleName"]?.ToString() ?? string.Empty,
                        PermissionCode = row["PermissionCode"]?.ToString() ?? string.Empty,
                        PermissionName = row["PermissionName"]?.ToString() ?? string.Empty,
                        Description = row["Description"]?.ToString() ?? string.Empty,
                        IsActive = row["IsActive"] != DBNull.Value && Convert.ToBoolean(row["IsActive"]),
                        CreatedOn = row["CreatedOn"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(row["CreatedOn"]),
                        CreatedBy = row["CreatedBy"] == DBNull.Value ? null : (int?)Convert.ToInt32(row["CreatedBy"])
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all permissions");
                return new List<PermissionDto>();
            }
        }
        #endregion

        #region Batch check permissions for an employee
        public async Task<Dictionary<string, bool>> BatchCheckPermissions(int employeeId)
        {
            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@Status", "BATCH_PERMISSION_CHECK"),
                    new SqlParameter("@EmployeeId", employeeId),
                    new SqlParameter("@PermissionCode", DBNull.Value),
                    new SqlParameter("@ResourceType", DBNull.Value),
                    new SqlParameter("@ResourceId", DBNull.Value),
                    new SqlParameter("@Action", DBNull.Value)
                };

                var dt = await _databaseService.ExecuteDataTableAsync("USP_CRM_PermissionManagement", parameters);

                var result = new Dictionary<string, bool>();
                foreach (DataRow row in dt.Rows)
                {
                    var code = row["PermissionCode"]?.ToString() ?? string.Empty;
                    var hasPermission = row["HasPermission"] != DBNull.Value && Convert.ToBoolean(row["HasPermission"]);
                    if (!string.IsNullOrEmpty(code))
                    {
                        result[code] = hasPermission;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch checking permissions for EmployeeId: {EmployeeId}", employeeId);
                return new Dictionary<string, bool>();
            }
        }
        #endregion
    }
}
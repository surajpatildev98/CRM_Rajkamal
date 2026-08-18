using CRMWebServices.Data;
using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Repository
{
    public class PermissionManagerRepository : IPermissionManagerRepository
    {
        private const string SP_NAME = "USP_CRM_PermissionManagement";
        private readonly IDatabaseService _database;
        private readonly ILogger<PermissionManagerRepository> _logger;

        public PermissionManagerRepository(IDatabaseService database, ILogger<PermissionManagerRepository> logger)
        {
            _database = database;
            _logger = logger;
        }

        #region CREATE_MODULE
        public async Task<ApiResponse> CreateModule(CreateModuleRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "CREATE_MODULE"),
                    new SqlParameter("@ModuleCode", request.ModuleCode),
                    new SqlParameter("@ModuleName", request.ModuleName),
                    new SqlParameter("@Description", (object?)request.Description ?? DBNull.Value),
                    new SqlParameter("@IconName", (object?)request.IconName ?? DBNull.Value),
                    new SqlParameter("@DisplayOrder", (object?)request.DisplayOrder ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", (object?)request.CreatedBy ?? DBNull.Value)
                );

                var row = dt.Rows[0];
                var success = Convert.ToBoolean(row["Success"]);

                return new ApiResponse
                {
                    Status = success,
                    Message = row["Message"]?.ToString(),
                    Data = success && dt.Columns.Contains("ModuleId")
                        ? new { ModuleId = Convert.ToInt32(row["ModuleId"]) }
                        : null
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error creating module");
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
        }
        #endregion

        #region UPDATE_MODULE
        public async Task<ApiResponse> UpdateModule(UpdateModuleRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "UPDATE_MODULE"),
                    new SqlParameter("@ModuleId", request.ModuleId),
                    new SqlParameter("@ModuleName", (object?)request.ModuleName ?? DBNull.Value),
                    new SqlParameter("@Description", (object?)request.Description ?? DBNull.Value),
                    new SqlParameter("@IconName", (object?)request.IconName ?? DBNull.Value),
                    new SqlParameter("@DisplayOrder", (object?)request.DisplayOrder ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", (object?)request.CreatedBy ?? DBNull.Value)
                );

                var row = dt.Rows[0];
                return new ApiResponse
                {
                    Status = Convert.ToBoolean(row["Success"]),
                    Message = row["Message"]?.ToString()
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error updating module {ModuleId}", request.ModuleId);
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
        }
        #endregion

        #region DEACTIVATE_MODULE
        public async Task<ApiResponse> DeactivateModule(DeactivateModuleRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "DEACTIVATE_MODULE"),
                    new SqlParameter("@ModuleId", request.ModuleId),
                    new SqlParameter("@CreatedBy", (object?)request.CreatedBy ?? DBNull.Value)
                );

                var row = dt.Rows[0];
                return new ApiResponse
                {
                    Status = Convert.ToBoolean(row["Success"]),
                    Message = row["Message"]?.ToString()
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error deactivating module {ModuleId}", request.ModuleId);
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
        }
        #endregion

        #region GET_MODULES
        public async Task<ApiResponse> GetModules()
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "GET_MODULES")
                );

                var result = dt.Rows.Cast<DataRow>().Select(r => new ModuleInfo
                {
                    ModuleId = Convert.ToInt32(r["ModuleId"]),
                    ModuleCode = r["ModuleCode"]?.ToString(),
                    ModuleName = r["ModuleName"]?.ToString(),
                    Description = r["Description"] == DBNull.Value ? null : r["Description"].ToString(),
                    IconName = r["IconName"] == DBNull.Value ? null : r["IconName"].ToString(),
                    DisplayOrder = r["DisplayOrder"] == DBNull.Value ? null : Convert.ToInt32(r["DisplayOrder"]),
                    IsActive = Convert.ToBoolean(r["IsActive"]),
                    TotalPermissions = Convert.ToInt32(r["TotalPermissions"])
                }).ToList();

                return new ApiResponse { Status = true, Message = "Modules fetched successfully.", Data = result };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error fetching modules");
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
        }
        #endregion

        #region CREATE_PERMISSION
        public async Task<ApiResponse> CreatePermission(CreatePermissionRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "CREATE_PERMISSION"),
                    new SqlParameter("@ModuleId", request.ModuleId),
                    new SqlParameter("@PermissionCode", request.PermissionCode),
                    new SqlParameter("@PermissionName", request.PermissionName),
                    new SqlParameter("@Description", (object?)request.Description ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", (object?)request.CreatedBy ?? DBNull.Value)
                );

                var row = dt.Rows[0];
                var success = Convert.ToBoolean(row["Success"]);

                return new ApiResponse
                {
                    Status = success,
                    Message = row["Message"]?.ToString(),
                    Data = success && dt.Columns.Contains("PermissionId")
                        ? new { PermissionId = Convert.ToInt32(row["PermissionId"]) }
                        : null
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error creating permission {Code}", request.PermissionCode);
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
        }
        #endregion

        #region UPDATE_PERMISSION
        public async Task<ApiResponse> UpdatePermission(UpdatePermissionRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "UPDATE_PERMISSION"),
                    new SqlParameter("@PermissionId", request.PermissionId),
                    new SqlParameter("@PermissionName", (object?)request.PermissionName ?? DBNull.Value),
                    new SqlParameter("@Description", (object?)request.Description ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", (object?)request.CreatedBy ?? DBNull.Value)
                );

                var row = dt.Rows[0];
                return new ApiResponse
                {
                    Status = Convert.ToBoolean(row["Success"]),
                    Message = row["Message"]?.ToString()
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error updating permission {PermissionId}", request.PermissionId);
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
        }
        #endregion

        #region DEACTIVATE_PERMISSION
        public async Task<ApiResponse> DeactivatePermission(DeactivatePermissionRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "DEACTIVATE_PERMISSION"),
                    new SqlParameter("@PermissionId", request.PermissionId),
                    new SqlParameter("@CreatedBy", (object?)request.CreatedBy ?? DBNull.Value)
                );

                var row = dt.Rows[0];
                return new ApiResponse
                {
                    Status = Convert.ToBoolean(row["Success"]),
                    Message = row["Message"]?.ToString()
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error deactivating permission {PermissionId}", request.PermissionId);
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
        }
        #endregion

        #region GET_MODULE_PERMISSIONS
        public async Task<ApiResponse> GetModulePermissions(int moduleId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "GET_MODULE_PERMISSIONS"),
                    new SqlParameter("@ModuleId", moduleId)
                );

                var result = dt.Rows.Cast<DataRow>().Select(r => new ModulePermissionCatalogItem
                {
                    PermissionId = Convert.ToInt32(r["PermissionId"]),
                    PermissionCode = r["PermissionCode"]?.ToString(),
                    PermissionName = r["PermissionName"]?.ToString(),
                    Description = r["Description"] == DBNull.Value ? null : r["Description"].ToString(),
                    IsActive = Convert.ToBoolean(r["IsActive"])
                }).ToList();

                return new ApiResponse { Status = true, Message = "Permissions fetched successfully.", Data = result };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error fetching permissions for module {ModuleId}", moduleId);
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
        }
        #endregion

        #region GET_ROLE_PERMISSIONS
        public async Task<ApiResponse> GetRolePermissions(int roleId, int? moduleId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "GET_ROLE_PERMISSIONS"),
                    new SqlParameter("@Ref_RoleId", roleId),
                    new SqlParameter("@ModuleId", (object?)moduleId ?? DBNull.Value)
                );

                //var result = dt.Rows.Cast<DataRow>().Select(r => new RolePermissionItem
                //{
                //    ModuleId = Convert.ToInt32(r["ModuleId"]),
                //    ModuleName = r["ModuleName"]?.ToString(),
                //    PermissionId = Convert.ToInt32(r["PermissionId"]),
                //    PermissionCode = r["PermissionCode"]?.ToString(),
                //    PermissionName = r["PermissionName"]?.ToString(),
                //    Description = r["Description"] == DBNull.Value ? null : r["Description"].ToString(),
                //    IsAllowed = Convert.ToBoolean(r["IsAllowed"])
                //}).ToList();
                var result = dt.Rows.Cast<DataRow>().Select(r => new RolePermissionItem
                {
                    ModuleId = Convert.ToInt32(r["ModuleId"]),

                    ModuleName = r["ModuleName"] == DBNull.Value? null
                            : r["ModuleName"]?.ToString(),

                                        // Module-level access
                                        ModuleIsAllowed = r["ModuleIsAllowed"] != DBNull.Value
                            && Convert.ToBoolean(r["ModuleIsAllowed"]),

                                        // Permission can be NULL for module-only modules
                                        PermissionId = r["PermissionId"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(r["PermissionId"]),

                                        PermissionCode = r["PermissionCode"] == DBNull.Value
                            ? null
                            : r["PermissionCode"]?.ToString(),

                                        PermissionName = r["PermissionName"] == DBNull.Value
                            ? null
                            : r["PermissionName"]?.ToString(),

                                        Description = r["Description"] == DBNull.Value
                            ? null
                            : r["Description"]?.ToString(),

                                        // Permission-level access
                                        IsAllowed = r["IsAllowed"] != DBNull.Value
                            && Convert.ToBoolean(r["IsAllowed"])

                                    }).ToList();

                return new ApiResponse { Status = true, Message = "Role permissions fetched successfully.", Data = result };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error fetching role permissions for role {RoleId}", roleId);
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
        }
        #endregion

        #region SAVE_ROLE_PERMISSIONS
        //public async Task<ApiResponse> SaveRolePermissions(SaveRolePermissionsRequest request)
        //{
        //    try
        //    {
        //        if (request.Permissions == null || request.Permissions.Count == 0)
        //            return new ApiResponse { Status = false, Message = "No permission changes provided." };

        //        var json = JsonConvert.SerializeObject(request.Permissions);

        //        var dt = await _database.ExecuteDataTableAsync(SP_NAME,
        //            new SqlParameter("@Status", "SAVE_ROLE_PERMISSIONS"),
        //            new SqlParameter("@Ref_RoleId", request.Ref_RoleId),
        //            new SqlParameter("@RolePermissionsJson", json),
        //            new SqlParameter("@CreatedBy", (object?)request.CreatedBy ?? DBNull.Value)
        //        );

        //        var row = dt.Rows[0];
        //        return new ApiResponse
        //        {
        //            Status = Convert.ToBoolean(row["Success"]),
        //            Message = row["Message"]?.ToString()
        //        };
        //    }
        //    catch (SqlException ex)
        //    {
        //        _logger.LogError(ex, "Error saving role permissions for role {RoleId}", request.Ref_RoleId);
        //        return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
        //    }
        //}

        public async Task<ApiResponse> SaveRolePermissions(
    SaveRolePermissionsRequest request)
        {
            try
            {
                // At least one type of change must be provided
                if ((request.Modules == null || request.Modules.Count == 0) &&
                    (request.Permissions == null || request.Permissions.Count == 0))
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "No module or permission changes provided."
                    };
                }

                var permissionsJson = JsonConvert.SerializeObject(
                    request.Permissions ?? new List<SaveRolePermissionItem>()
                );

                var modulesJson = JsonConvert.SerializeObject(
                    request.Modules ?? new List<SaveRoleModuleAccessItem>()
                );

                var dt = await _database.ExecuteDataTableAsync(
                    SP_NAME,

                    // IMPORTANT:
                    // One transaction handles BOTH module + permission access
                    new SqlParameter(
                        "@Status",
                        "SAVE_ROLE_ACCESS"
                    ),

                    new SqlParameter(
                        "@Ref_RoleId",
                        request.Ref_RoleId
                    ),

                    new SqlParameter(
                        "@RolePermissionsJson",
                        permissionsJson
                    ),

                    new SqlParameter(
                        "@RoleModuleAccessJson",
                        modulesJson
                    ),

                    new SqlParameter(
                        "@CreatedBy",
                        (object?)request.CreatedBy ?? DBNull.Value
                    )
                );

                if (dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "No response received from database."
                    };
                }

                var row = dt.Rows[0];
                return new ApiResponse
                {
                    Status = Convert.ToBoolean(row["Success"]),
                    Message = row["Message"]?.ToString()
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Error saving role access for role {RoleId}",
                    request.Ref_RoleId
                );

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region GET_ROLE_MODULE_ACCESS
        public async Task<ApiResponse> GetRoleModuleAccess(int roleId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "GET_ROLE_MODULE_ACCESS"),
                    new SqlParameter("@Ref_RoleId", roleId)
                );

                var result = dt.Rows.Cast<DataRow>().Select(r => new RoleModuleAccessItem
                {
                    ModuleId = Convert.ToInt32(r["ModuleId"]),
                    ModuleName = r["ModuleName"]?.ToString(),
                    Description = r["Description"] == DBNull.Value ? null : r["Description"].ToString(),
                    IconName = r["IconName"] == DBNull.Value ? null : r["IconName"].ToString(),
                    IsEnabled = Convert.ToBoolean(r["IsEnabled"])
                }).ToList();

                return new ApiResponse { Status = true, Message = "Module access fetched successfully.", Data = result };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error fetching module access for role {RoleId}", roleId);
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
        }
        #endregion

        #region SAVE_ROLE_MODULE_ACCESS
        public async Task<ApiResponse> SaveRoleModuleAccess(SaveRoleModuleAccessRequest request)
        {
            try
            {
                if (request.Modules == null || request.Modules.Count == 0)
                    return new ApiResponse { Status = false, Message = "No module access changes provided." };

                var json = JsonConvert.SerializeObject(request.Modules);

                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "SAVE_ROLE_MODULE_ACCESS"),
                    new SqlParameter("@Ref_RoleId", request.Ref_RoleId),
                    new SqlParameter("@RoleModuleAccessJson", json),
                    new SqlParameter("@CreatedBy", (object?)request.CreatedBy ?? DBNull.Value)
                );

                var row = dt.Rows[0];
                return new ApiResponse
                {
                    Status = Convert.ToBoolean(row["Success"]),
                    Message = row["Message"]?.ToString()
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error saving module access for role {RoleId}", request.Ref_RoleId);
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
        }
        #endregion
    }
}
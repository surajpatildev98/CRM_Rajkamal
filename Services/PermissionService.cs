using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PermissionService> _logger;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(15);

        public PermissionService(
            IPermissionRepository permissionRepository,
            IMemoryCache cache,
            ILogger<PermissionService> logger)
        {
            _permissionRepository = permissionRepository;
            _cache = cache;
            _logger = logger;
        }

        #region HasPermission used for checking single permission
        public async Task<bool> HasPermission(ClaimsPrincipal user, string permissionCode)
        {
            if (user == null || !user.Identity.IsAuthenticated)
                return false;

            var employeeId = user.EmployeeId(); // Returns int

            var cacheKey = $"Permission_{employeeId}_{permissionCode}";
            if (_cache.TryGetValue(cacheKey, out bool hasPermission))
                return hasPermission;

            try
            {
                hasPermission = await _permissionRepository.UserHasPermission(employeeId, permissionCode);
                _cache.Set(cacheKey, hasPermission, _cacheDuration);
                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {PermissionCode} for EmployeeId: {EmployeeId}",
                    permissionCode, employeeId);
                return false;
            }
        }
        #endregion

        #region HasAnyPermission and HasAllPermissions used for checking multiple permissions
        public async Task<bool> HasAnyPermission(ClaimsPrincipal user, params string[] permissionCodes)
        {
            if (permissionCodes == null || permissionCodes.Length == 0)
                return false;

            foreach (var code in permissionCodes)
            {
                if (await HasPermission(user, code))
                    return true;
            }
            return false;
        }
        #endregion

        #region HasAllPermissions used for checking multiple permissions
        public async Task<bool> HasAllPermissions(ClaimsPrincipal user, params string[] permissionCodes)
        {
            if (permissionCodes == null || permissionCodes.Length == 0)
                return true;

            foreach (var code in permissionCodes)
            {
                if (!await HasPermission(user, code))
                    return false;
            }
            return true;
        }
        #endregion

        #region CheckMultiplePermissions used for checking multiple permissions and returning a dictionary of results
        public async Task<Dictionary<string, bool>> CheckMultiplePermissions(ClaimsPrincipal user, string[] permissionCodes)
        {
            var employeeId = user.EmployeeId(); // Returns int

            var result = new Dictionary<string, bool>();
            var uncachedCodes = new List<string>();

            foreach (var code in permissionCodes)
            {
                var cacheKey = $"Permission_{employeeId}_{code}";
                if (_cache.TryGetValue(cacheKey, out bool hasPermission))
                {
                    result[code] = hasPermission;
                }
                else
                {
                    uncachedCodes.Add(code);
                }
            }

            if (uncachedCodes.Any())
            {
                try
                {
                    var dbResults = await _permissionRepository.UserHasMultiplePermissions(employeeId, uncachedCodes.ToArray());
                    foreach (var kvp in dbResults)
                    {
                        result[kvp.Key] = kvp.Value;
                        var cacheKey = $"Permission_{employeeId}_{kvp.Key}";
                        _cache.Set(cacheKey, kvp.Value, _cacheDuration);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking multiple permissions for EmployeeId: {EmployeeId}", employeeId);
                }
            }

            return result;
        }
        #endregion

        #region GetUserPermissions and GetUserPermissionDictionary used for retrieving all permissions for a user
        public async Task<List<UserPermission>> GetUserPermissions(ClaimsPrincipal user)
        {
            var employeeId = user.EmployeeId(); // Returns int

            var cacheKey = $"UserPermissions_{employeeId}";
            if (_cache.TryGetValue(cacheKey, out List<UserPermission> permissions))
                return permissions;

            try
            {
                permissions = await _permissionRepository.GetUserPermissions(employeeId);
                _cache.Set(cacheKey, permissions, _cacheDuration);
                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user permissions for EmployeeId: {EmployeeId}", employeeId);
                return new List<UserPermission>();
            }
        }
        #endregion

        #region GetUserPermissionDictionary used for retrieving all permissions for a user as a dictionary
        public async Task<Dictionary<string, bool>> GetUserPermissionDictionary(ClaimsPrincipal user)
        {
            var employeeId = user.EmployeeId(); // Returns int

            var cacheKey = $"UserPermissionsDict_{employeeId}";
            if (_cache.TryGetValue(cacheKey, out Dictionary<string, bool> permissionsDict))
                return permissionsDict;

            try
            {
                var permissions = await GetUserPermissions(user);
                permissionsDict = permissions.ToDictionary(p => p.PermissionCode, p => p.IsGranted);
                _cache.Set(cacheKey, permissionsDict, _cacheDuration);
                return permissionsDict;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission dictionary for EmployeeId: {EmployeeId}", employeeId);
                return new Dictionary<string, bool>();
            }
        }
        #endregion

        #region GetResourceOwner used for retrieving the owner of a resource
        public async Task<ResourceOwner> GetResourceOwner(string resourceType, int resourceId)
        {
            return await _permissionRepository.GetResourceOwner(resourceType, resourceId);
        }
        #endregion

        #region GetAccessFilter used for determining the access filter for a user on a specific resource type
        public async Task<PermissionFilter> GetAccessFilter(ClaimsPrincipal user, string resourceType)
        {
            var filter = new PermissionFilter();
            var employeeId = user.EmployeeId(); // Returns int

            // Check ViewAll
            if (await HasPermission(user, $"{resourceType}_ViewAll"))
            {
                filter.CanViewAll = true;
                filter.EmployeeId = employeeId;   // ADDED — was missing
                filter.FilterType = "All";
                return filter;
            }

            // Check View permission
            if (await HasPermission(user, $"{resourceType}_View"))
            {
                filter.CanViewAll = false;
                filter.EmployeeId = employeeId;

                var hasCreate = await HasPermission(user, $"{resourceType}_Create");
                if (hasCreate)
                {
                    filter.ShowCreatedAndAssigned = true;
                    filter.FilterType = "CreatedAndAssigned";
                }
                else
                {
                    filter.FilterType = "Assigned";
                }
            }
            else
            {
                filter.FilterType = "None";
            }

            return filter;
        }
        #endregion

        #region GetUserModules used for retrieving the modules a user has access to specifically for the modules they have permissions for
        public async Task<List<ModulePermission>> GetUserModules(ClaimsPrincipal user)
        {
            var employeeId = user.EmployeeId(); // Returns int
            return await _permissionRepository.GetUserModules(employeeId);
        }
        #endregion

        #region InvalidateCache used for clearing cached permissions for a user
        public void InvalidateCache(int employeeId)
        {
            _cache.Remove($"UserPermissions_{employeeId}");
            _cache.Remove($"UserPermissionsDict_{employeeId}");
            _logger.LogInformation("Cache invalidated for user {EmployeeId}", employeeId);
        }
        #endregion

    }
}
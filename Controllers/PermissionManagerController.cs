using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PermissionManagerController : ControllerBase
    {
        private readonly IPermissionManagerRepository _repository;

        public PermissionManagerController(IPermissionManagerRepository repository)
        {
            _repository = repository;
        }

        // TEMP: Admin-only gate until a bootstrapped permission code exists for this module.
        // Replace with _authorization.CanPerformAction(User, "PermissionManager", "Manage")
        // once seeded.
        //private bool IsAdmin() => User.IsInRole("1") || User.HasClaim("RoleId", "1");
        private bool IsAdmin()
        {
            var roleIdsClaim = User.FindFirst("RoleIds")?.Value;   // e.g. "1" or "1,5" if multi-role later
            if (string.IsNullOrEmpty(roleIdsClaim))
                return false;

            return roleIdsClaim
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .Contains("1");
        }

        #region POST api/PermissionManager/module/create
        [HttpPost("module/create")]
        public async Task<IActionResult> CreateModule([FromBody] CreateModuleRequest request)
        {
            if (!IsAdmin())
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse { Status = false, Message = "Not authorized." });

            request.CreatedBy = User.EmployeeId();
            var response = await _repository.CreateModule(request);
            return response.Status ? Ok(response) : BadRequest(response);
        }
        #endregion

        #region POST api/PermissionManager/module/update
        [HttpPost("module/update")]
        public async Task<IActionResult> UpdateModule([FromBody] UpdateModuleRequest request)
        {
            if (!IsAdmin())
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse { Status = false, Message = "Not authorized." });

            request.CreatedBy = User.EmployeeId();
            var response = await _repository.UpdateModule(request);
            return response.Status ? Ok(response) : BadRequest(response);
        }
        #endregion

        #region POST api/PermissionManager/module/deactivate
        [HttpPost("module/deactivate")]
        public async Task<IActionResult> DeactivateModule([FromBody] DeactivateModuleRequest request)
        {
            if (!IsAdmin())
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse { Status = false, Message = "Not authorized." });

            request.CreatedBy = User.EmployeeId();
            var response = await _repository.DeactivateModule(request);
            return response.Status ? Ok(response) : BadRequest(response);
        }
        #endregion

        #region GET api/PermissionManager/modules
        [HttpGet("modules")]
        public async Task<IActionResult> GetModules()
        {
            if (!IsAdmin())
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse { Status = false, Message = "Not authorized." });

            var response = await _repository.GetModules();
            return response.Status ? Ok(response) : BadRequest(response);
        }
        #endregion

        #region POST api/PermissionManager/permission/create
        [HttpPost("permission/create")]
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
        {
            if (!IsAdmin())
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse { Status = false, Message = "Not authorized." });

            request.CreatedBy = User.EmployeeId();
            var response = await _repository.CreatePermission(request);
            return response.Status ? Ok(response) : BadRequest(response);
        }
        #endregion

        #region POST api/PermissionManager/permission/update
        [HttpPost("permission/update")]
        public async Task<IActionResult> UpdatePermission([FromBody] UpdatePermissionRequest request)
        {
            if (!IsAdmin())
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse { Status = false, Message = "Not authorized." });

            request.CreatedBy = User.EmployeeId();
            var response = await _repository.UpdatePermission(request);
            return response.Status ? Ok(response) : BadRequest(response);
        }
        #endregion

        #region POST api/PermissionManager/permission/deactivate
        [HttpPost("permission/deactivate")]
        public async Task<IActionResult> DeactivatePermission([FromBody] DeactivatePermissionRequest request)
        {
            if (!IsAdmin())
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse { Status = false, Message = "Not authorized." });

            request.CreatedBy = User.EmployeeId();
            var response = await _repository.DeactivatePermission(request);
            return response.Status ? Ok(response) : BadRequest(response);
        }
        #endregion

        #region GET api/PermissionManager/module/{moduleId}/permissions
        [HttpGet("module/{moduleId}/permissions")]
        public async Task<IActionResult> GetModulePermissions(int moduleId)
        {
            if (!IsAdmin())
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse { Status = false, Message = "Not authorized." });

            var response = await _repository.GetModulePermissions(moduleId);
            return response.Status ? Ok(response) : BadRequest(response);
        }
        #endregion

        #region GET api/PermissionManager/role/{roleId}/permissions
        [HttpGet("role/{roleId}/permissions")]
        public async Task<IActionResult> GetRolePermissions(int roleId, [FromQuery] int? moduleId)
        {
            if (!IsAdmin())
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse { Status = false, Message = "Not authorized." });

            var response = await _repository.GetRolePermissions(roleId, moduleId);
            return response.Status ? Ok(response) : BadRequest(response);
        }
        #endregion

        #region POST api/PermissionManager/role/permissions/save
        [HttpPost("role/permissions/save")]
        public async Task<IActionResult> SaveRolePermissions([FromBody] SaveRolePermissionsRequest request)
        {
            if (!IsAdmin())
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse { Status = false, Message = "Not authorized." });

            request.CreatedBy = User.EmployeeId();
            var response = await _repository.SaveRolePermissions(request);

            // TODO: invalidate cache for every employee holding this role — see cache staleness note
            return response.Status ? Ok(response) : BadRequest(response);
        }
        #endregion

        #region GET api/PermissionManager/role/{roleId}/module-access
        [HttpGet("role/{roleId}/module-access")]
        public async Task<IActionResult> GetRoleModuleAccess(int roleId)
        {
            if (!IsAdmin())
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse { Status = false, Message = "Not authorized." });

            var response = await _repository.GetRoleModuleAccess(roleId);
            return response.Status ? Ok(response) : BadRequest(response);
        }
        #endregion

        #region POST api/PermissionManager/role/module-access/save
        [HttpPost("role/module-access/save")]
        public async Task<IActionResult> SaveRoleModuleAccess([FromBody] SaveRoleModuleAccessRequest request)
        {
            if (!IsAdmin())
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse { Status = false, Message = "Not authorized." });

            request.CreatedBy = User.EmployeeId();
            var response = await _repository.SaveRoleModuleAccess(request);

            // TODO: invalidate cache for every employee holding this role — see cache staleness note
            return response.Status ? Ok(response) : BadRequest(response);
        }
        #endregion
    }
}
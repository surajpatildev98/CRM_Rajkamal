using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRMWebServices.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/attendance")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceRepository _attendanceRepository;

        public AttendanceController(IAttendanceRepository attendanceRepository)
        {
            _attendanceRepository = attendanceRepository;
        }

        // ---------- helpers ----------

        private int? GetLoggedInEmployeeId()
        {
            var v = User.FindFirst("EmployeeId")?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }

        // ORG_Role ids that see every employee:
        // 1 = Administrator, 6 = Super Administrator, 8 = HR Manager
        private static readonly string[] AllScopeRoleIds = { "1", "6", "8" };

        // ORG_Role ids that see only the employees reporting to them:
        // 5 = Sales Manager, 9 = Support Manager, 11 = Project Manager,
        // 15 = Warehouse Manager, 18 = Purchase Manager, 20 = Finance Manager
        private static readonly string[] TeamScopeRoleIds = { "5", "9", "11", "15", "18", "20" };

        // scope from the RoleIds claim ("8" or "8,12")
        private string GetRoleScope()
        {
            var roleIds = (User.FindFirst("RoleIds")?.Value ?? "")
               .Split(',', StringSplitOptions.RemoveEmptyEntries)
               .Select(x => x.Trim())
               .ToList();

            if (roleIds.Any(AllScopeRoleIds.Contains))
                return "ALL";

            if (roleIds.Any(TeamScopeRoleIds.Contains))
                return "TEAM";

            // ResolveViewScope still upgrades SELF -> TEAM when people report to him
            return "SELF";
        }

        // role scope + data check:
        // anybody who has employees reporting to him is a manager -> TEAM
        private Task<string> GetViewScopeAsync(int employeeId)
            => _attendanceRepository.ResolveViewScope(employeeId, GetRoleScope());

        // ---------- endpoints ----------

        [HttpPost("GetAttendance")]
        public async Task<IActionResult> GetAttendance([FromBody] AttendanceRequest request)
        {
            try
            {
                var loggedInId = GetLoggedInEmployeeId();
                if (loggedInId == null)
                    return Unauthorized(new ApiResponse { Status = false, Message = "Invalid login token." });

                var scope = await GetViewScopeAsync(loggedInId.Value);
                var response = await _attendanceRepository.GetAttendance(request, loggedInId.Value, scope);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpPost("GetPunches")]
        public async Task<IActionResult> GetPunches([FromBody] AttendancePunchRequest request)
        {
            try
            {
                var loggedInId = GetLoggedInEmployeeId();
                if (loggedInId == null)
                    return Unauthorized(new ApiResponse { Status = false, Message = "Invalid login token." });

                var scope = await GetViewScopeAsync(loggedInId.Value);
                var response = await _attendanceRepository.GetPunches(request, loggedInId.Value, scope);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpPost("ManualPunch")]
        public async Task<IActionResult> ManualPunch([FromBody] ManualPunchRequest request)
        {
            try
            {
                var response = await _attendanceRepository.ManualPunch(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpPost("GetDashboardCount")]
        public async Task<IActionResult> GetDashboardCount([FromBody] AttendanceDashboardRequest request)
        {
            try
            {
                var response = await _attendanceRepository.GetDashboardCount(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }


        [HttpPost("GetReportEmployees")]
        public async Task<IActionResult> GetReportEmployees([FromBody] AttendanceReportEmployeesRequest request)
        {
            try
            {
                var loggedInId = GetLoggedInEmployeeId();
                if (loggedInId == null)
                    return Unauthorized(new ApiResponse { Status = false, Message = "Invalid login token." });

                var scope = await GetViewScopeAsync(loggedInId.Value);
                var response = await _attendanceRepository.GetReportEmployees(request, loggedInId.Value, scope);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpPost("GetEmployeeReport")]
        public async Task<IActionResult> GetEmployeeReport([FromBody] AttendanceEmployeeReportRequest request)
        {
            try
            {
                var loggedInId = GetLoggedInEmployeeId();
                if (loggedInId == null)
                    return Unauthorized(new ApiResponse { Status = false, Message = "Invalid login token." });

                var scope = await GetViewScopeAsync(loggedInId.Value);
                var response = await _attendanceRepository.GetEmployeeReport(request, loggedInId.Value, scope);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        // ---------- regularization (employee request -> manager approval) ----------
        // The employee always comes from the login token; the employeeId the
        // app sends is not trusted.

        [HttpGet("GetMyRegularizations")]
        public async Task<IActionResult> GetMyRegularizations()
        {
            try
            {
                var loggedInId = GetLoggedInEmployeeId();
                if (loggedInId == null)
                    return Unauthorized(new ApiResponse { Status = false, Message = "Invalid login token." });

                var response = await _attendanceRepository.GetMyRegularizations(loggedInId.Value);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpGet("GetTeamRegularizations")]
        public async Task<IActionResult> GetTeamRegularizations()
        {
            try
            {
                var loggedInId = GetLoggedInEmployeeId();
                if (loggedInId == null)
                    return Unauthorized(new ApiResponse { Status = false, Message = "Invalid login token." });

                var response = await _attendanceRepository.GetTeamRegularizations(loggedInId.Value);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpPost("ApplyRegularization")]
        public async Task<IActionResult> ApplyRegularization([FromBody] ApplyRegularizationRequest request)
        {
            try
            {
                var loggedInId = GetLoggedInEmployeeId();
                if (loggedInId == null)
                    return Unauthorized(new ApiResponse { Status = false, Message = "Invalid login token." });

                if (request == null)
                    return BadRequest(new ApiResponse { Status = false, Message = "Request is required." });

                var response = await _attendanceRepository.ApplyRegularization(request, loggedInId.Value);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpPost("CancelRegularization")]
        public Task<IActionResult> CancelRegularization([FromBody] RegularizationActionRequest request)
            => RegularizationAction(request, requireReason: true, _attendanceRepository.CancelRegularization);

        [HttpPost("ApproveRegularization")]
        public Task<IActionResult> ApproveRegularization([FromBody] RegularizationActionRequest request)
            => RegularizationAction(request, requireReason: false, _attendanceRepository.ApproveRegularization);

        [HttpPost("RejectRegularization")]
        public Task<IActionResult> RejectRegularization([FromBody] RegularizationActionRequest request)
            => RegularizationAction(request, requireReason: true, _attendanceRepository.RejectRegularization);

        private async Task<IActionResult> RegularizationAction(
            RegularizationActionRequest request,
            bool requireReason,
            Func<RegularizationActionRequest, int, Task<RegularizationActionResponse>> action)
        {
            try
            {
                var loggedInId = GetLoggedInEmployeeId();
                if (loggedInId == null)
                    return Unauthorized(new ApiResponse { Status = false, Message = "Invalid login token." });

                if (request == null || request.RegularizationId <= 0)
                    return BadRequest(new ApiResponse { Status = false, Message = "Invalid RegularizationId." });

                if (requireReason && string.IsNullOrWhiteSpace(request.Reason))
                    return BadRequest(new ApiResponse { Status = false, Message = "Reason is required." });

                var response = await action(request, loggedInId.Value);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }
    }
}

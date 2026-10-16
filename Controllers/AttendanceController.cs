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

        // scope from the ROLE claims (HR roles + explicit Manager role)
        private string GetRoleScope()
        {
            var roles = User.FindAll(ClaimTypes.Role)
                .Select(r => r.Value.Replace(" ", "").Replace("_", "").ToUpperInvariant())
                .ToList();

            if (roles.Contains("HRMANAGER")) return "ALL";
            if (roles.Contains("HREXECUTIVE")) return "MAPPED";
            if (roles.Contains("MANAGER")) return "TEAM";
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
    }
}

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

        [HttpPost("GetAttendance")]
        public async Task<IActionResult> GetAttendance([FromBody] AttendanceRequest request)
        {
            try
            {
                var response = await _attendanceRepository.GetAttendance(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPost("GetPunches")]
        public async Task<IActionResult> GetPunches([FromBody] AttendancePunchRequest request)
        {
            try
            {
                var response =await _attendanceRepository.GetPunches(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                });
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
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPost("GetDashboardCount")]
        public async Task<IActionResult> GetDashboardCount([FromBody] AttendanceDashboardRequest request)
        {
            try
            {
                var response =await _attendanceRepository.GetDashboardCount(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                });
            }
        }

        private int? GetLoggedInEmployeeId()
        {
            var v = User.FindFirst("EmployeeId")?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }

        private string GetViewScope()
        {
            var roles = User.FindAll(ClaimTypes.Role)
                .Select(r => r.Value.Replace(" ", "").Replace("_", "").ToUpperInvariant())
                .ToList();

            if (roles.Contains("HRMANAGER")) return "ALL";     // HR Manager   -> all employees
            if (roles.Contains("HREXECUTIVE")) return "MAPPED";  // HR Executive -> mapped employees
            if (roles.Contains("MANAGER")) return "TEAM";    // Manager      -> his team
            return "SELF";                                       // everyone else -> only himself
        }

        [HttpPost("GetReportEmployees")]
        public async Task<IActionResult> GetReportEmployees([FromBody] AttendanceReportEmployeesRequest request)
        {
            try
            {
                var loggedInId = GetLoggedInEmployeeId();
                if (loggedInId == null)
                    return Unauthorized(new ApiResponse { Status = false, Message = "Invalid login token." });

                var response = await _attendanceRepository.GetReportEmployees(request, loggedInId.Value, GetViewScope());
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

                var response = await _attendanceRepository.GetEmployeeReport(request, loggedInId.Value, GetViewScope());
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }
    }
}

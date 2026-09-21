using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    }
}

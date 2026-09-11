using CRMWebServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRMWebServices.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/leave")]
    public class LeaveController : ControllerBase
    {
        private readonly ILeaveRepository _leaveRepository;

        public LeaveController(ILeaveRepository leaveRepository)
        {
            _leaveRepository = leaveRepository;
        }

        // GET: api/Leave/GetLeaveBalance?employeeId=3034
        [HttpGet("GetLeaveBalance")]
        public async Task<IActionResult> GetLeaveBalance(
            [FromQuery] int employeeId)
        {
            if (employeeId <= 0)
            {
                return BadRequest(new
                {
                    Status = false,
                    Message = "Invalid EmployeeId."
                });
            }

            var response =
                await _leaveRepository.GetLeaveBalance(employeeId);

            return Ok(response);
        }

        // GET: api/Leave/GetMyLeaves?employeeId=3034
        [HttpGet("GetMyLeaves")]
        public async Task<IActionResult> GetMyLeaves(
            [FromQuery] int employeeId)
        {
            if (employeeId <= 0)
            {
                return BadRequest(new
                {
                    Status = false,
                    Message = "Invalid EmployeeId."
                });
            }

            var response =
                await _leaveRepository.GetMyLeaves(employeeId);

            return Ok(response);
        }
    }
}

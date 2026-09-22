using CRMWebServices.Interfaces;
using CRMWebServices.Models;
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

        #region GetLeaveBalance
        [HttpGet("GetLeaveBalance")]
        public async Task<IActionResult> GetLeaveBalance([FromQuery] int employeeId)
        {
            if (employeeId <= 0)
            {
                return BadRequest(new
                {
                    Status = false,
                    Message = "Invalid EmployeeId."
                });
            }

            var response =await _leaveRepository.GetLeaveBalance(employeeId);
            return Ok(response);
        }
        #endregion

        #region GetMyLeaves
        [HttpGet("GetMyLeaves")]
        public async Task<IActionResult> GetMyLeaves([FromQuery] int employeeId)
        {
            if (employeeId <= 0)
            {
                return BadRequest(new
                {
                    Status = false,
                    Message = "Invalid EmployeeId."
                });
            }

            var response =await _leaveRepository.GetMyLeaves(employeeId);

            return Ok(response);
        }
        #endregion

        [HttpGet("GetApplyLeaveTypes")]
        public async Task<IActionResult> GetApplyLeaveTypes(int employeeId)
        {
            try
            {
                var result = await _leaveRepository.GetApplyLeaveTypes(employeeId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    status = false,
                    message = ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpPost("ApplyLeave")]
        public async Task<IActionResult> ApplyLeave([FromBody] ApplyLeaveRequest model)
        {
            try
            {
                var result = await _leaveRepository.ApplyLeave(model);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    status = false,
                    message = ex.Message,
                    data = (object)null
                });
            }
        }
    }
}

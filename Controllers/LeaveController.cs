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

        #region Leave Types
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
#endregion

        #region Apply leave
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
#endregion

        #region CancelLeave

        [HttpPost("CancelLeave")]
        public async Task<IActionResult> CancelLeave(
            [FromBody] CancelLeaveRequest model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Request is required.",
                        data = (object)null
                    });
                }

                if (model.EmployeeId <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid EmployeeId.",
                        data = (object)null
                    });
                }

                if (model.EmployeeLeaveId <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid EmployeeLeaveId.",
                        data = (object)null
                    });
                }

                var result =
                    await _leaveRepository.CancelLeave(model);

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

        #endregion

        #region GetTeamLeaves

        [HttpGet("GetTeamLeaves")]
        public async Task<IActionResult> GetTeamLeaves([FromQuery] int employeeId)
        {
            try
            {
                if (employeeId <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid EmployeeId.",
                        data = (object)null
                    });
                }

                var result =
                    await _leaveRepository.GetTeamLeaves(employeeId);

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

        #endregion

        #region ApproveLeave

        [HttpPost("ApproveLeave")]
        public async Task<IActionResult> ApproveLeave([FromBody] ApproveLeaveRequest model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Request is required.",
                        data = (object)null
                    });
                }

                if (model.EmployeeId <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid EmployeeId.",
                        data = (object)null
                    });
                }

                if (model.EmployeeLeaveId <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid EmployeeLeaveId.",
                        data = (object)null
                    });
                }

                var result =
                    await _leaveRepository.ApproveLeave(model);

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

        #endregion

        #region RejectLeave

        [HttpPost("RejectLeave")]
        public async Task<IActionResult> RejectLeave([FromBody] RejectLeaveRequest model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Request is required.",
                        data = (object)null
                    });
                }

                if (model.EmployeeId <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid EmployeeId.",
                        data = (object)null
                    });
                }

                if (model.EmployeeLeaveId <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid EmployeeLeaveId.",
                        data = (object)null
                    });
                }

                if (string.IsNullOrWhiteSpace(model.Reason))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Rejection reason is required.",
                        data = (object)null
                    });
                }

                var result =
                    await _leaveRepository.RejectLeave(model);

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

#endregion


    }
}

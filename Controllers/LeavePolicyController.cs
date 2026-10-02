using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LeavePolicyController : ControllerBase
{
    private readonly ILeavePolicyRepository _leavePolicyRepository;

    public LeavePolicyController(ILeavePolicyRepository leavePolicyRepository)
    {
        _leavePolicyRepository = leavePolicyRepository;
    }

    [HttpPost("SaveLeavePolicy")]
    public async Task<IActionResult> SaveLeavePolicy([FromBody] LeavePolicyModels.LeavePolicySaveRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = "Request is required."
                });
            }

            var employeeId = User.EmployeeId();

            if (employeeId <= 0)
            {
                return Unauthorized(new ApiResponse
                {
                    Status = false,
                    Message = "Logged in employee is required."
                });
            }

            var response = await _leavePolicyRepository.SaveLeavePolicy(request, employeeId);
            return response.Status? Ok(response): BadRequest(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse
            {
                Status = false,
                Message = $"Error: {ex.Message}"
            });
        }
    }

    [HttpPost("UpdateLeavePolicy")]
    public async Task<IActionResult> UpdateLeavePolicy([FromBody] LeavePolicyModels.LeavePolicyUpdateRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = "Request is required."
                });
            }

            var employeeId = User.EmployeeId();

            if (employeeId <= 0)
            {
                return Unauthorized(new ApiResponse
                {
                    Status = false,
                    Message = "Logged in employee is required."
                });
            }

            var response = await _leavePolicyRepository.UpdateLeavePolicy(request, employeeId);
            return response.Status? Ok(response): BadRequest(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse
            {
                Status = false,
                Message = $"Error: {ex.Message}"
            });
        }
    }

    [HttpGet("GetLeavePolicyList")]
    public async Task<IActionResult> GetLeavePolicyList([FromQuery] LeavePolicyModels.LeavePolicyListRequest request)
    {
        try
        {
            var response = await _leavePolicyRepository.GetLeavePolicyList(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse
            {
                Status = false,
                Message = $"Error: {ex.Message}"
            });
        }
    }

    [HttpGet("GetLeavePolicyDetails/{leavePolicyId}")]
    public async Task<IActionResult> GetLeavePolicyDetails(int leavePolicyId)
    {
        try
        {
            var response = await _leavePolicyRepository.GetLeavePolicyDetails(leavePolicyId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse
            {
                Status = false,
                Message = $"Error: {ex.Message}"
            });
        }
    }

    [HttpGet("GetLeaveTypes")]
    public async Task<IActionResult> GetLeaveTypes()
    {
        try
        {
            var response = await _leavePolicyRepository.GetLeaveTypes();
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse
            {
                Status = false,
                Message = $"Error: {ex.Message}"
            });
        }
    }
}
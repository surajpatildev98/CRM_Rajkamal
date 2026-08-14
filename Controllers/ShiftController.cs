using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRMWebServices.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/shift")]
    public class ShiftController : ControllerBase
    {
        private readonly IShiftRepository _shiftRepository;

        public ShiftController(IShiftRepository shiftRepository)
        {
            _shiftRepository = shiftRepository;
        }

        [HttpGet("GetShifts")]
        public async Task<IActionResult> GetShifts()
        {
            try
            {
                var shifts = await _shiftRepository.GetShifts();

                return Ok(new ApiResponse
                {
                    Status = shifts.Status,
                    Message = shifts.Message,
                    Data = shifts.Data
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpPost("AddShift")]
        public async Task<IActionResult> AddShift([FromBody] ShiftModel model)
        {
            try
            {
                var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

                if (!int.TryParse(employeeIdClaim, out int employeeId))
                {
                    return Unauthorized(new ApiResponse
                    {
                        Status = false,
                        Message = "Invalid user.",
                        Data = null
                    });
                }

                model.CreatedBy = employeeId;

                var response = await _shiftRepository.AddShift(model);

                return Ok(new ApiResponse
                {
                    Status = response.Status,
                    Message = response.Message,
                    Data = response.Data
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpPost("UpdateShift")]
        public async Task<IActionResult> UpdateShift([FromBody] ShiftModel shift)
        {
            try
            {
                var response = await _shiftRepository.UpdateShift(shift);

                return Ok(new ApiResponse
                {
                    Status = response.Status,
                    Message = response.Message,
                    Data = response.Data
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpPost("delete/{shiftId}")]
        public async Task<IActionResult> DeleteShift(int shiftId)
        {
            try
            {
                if (shiftId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Invalid shift id."
                    });
                }

                var response = await _shiftRepository.DeleteShift(shiftId);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                });
            }
        }

    }
}

using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRMWebServices.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/payslip")]
    public class PayslipController : ControllerBase
    {
        private readonly IPayslipRepository _payslipRepository;

        public PayslipController(IPayslipRepository payslipRepository)
        {
            _payslipRepository = payslipRepository;
        }

        // ---------- helpers ----------

        private int? GetLoggedInEmployeeId()
        {
            var v = User.FindFirst("EmployeeId")?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }

        /// HR = role id 8 (HR Manager) in the token, or any role name starting with "HR".
        private bool IsHr()
        {
            var roles = User.FindAll(ClaimTypes.Role)
                .Select(r => r.Value.Replace(" ", "").Replace("_", "").ToUpperInvariant());

            var roleIds = (User.FindFirst("RoleIds")?.Value ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim());

            return roleIds.Contains("8") || roles.Any(r => r.StartsWith("HR"));
        }

        private IActionResult NotHr() =>
            StatusCode(StatusCodes.Status403Forbidden,
                new ApiResponse { Status = false, Message = "Only HR can perform this action." });

        // ================= EMPLOYEE (own payslips) =================
        // The employee id always comes from the token, never from the request.

        [HttpPost("GetMyPayslips")]
        public async Task<IActionResult> GetMyPayslips()
        {
            try
            {
                var loggedInId = GetLoggedInEmployeeId();
                if (loggedInId == null)
                    return Unauthorized(new ApiResponse { Status = false, Message = "Invalid login token." });

                return Ok(await _payslipRepository.GetMyPayslips(loggedInId.Value));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpPost("GetPayslip")]
        public async Task<IActionResult> GetPayslip([FromBody] PayslipDetailRequest request)
        {
            try
            {
                var loggedInId = GetLoggedInEmployeeId();
                if (loggedInId == null)
                    return Unauthorized(new ApiResponse { Status = false, Message = "Invalid login token." });

                return Ok(await _payslipRepository.GetPayslip(loggedInId.Value, request.PayslipId));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        // ======================== HR ========================

        [HttpPost("GetHrPayslips")]
        public async Task<IActionResult> GetHrPayslips([FromBody] PayslipMonthRequest request)
        {
            try
            {
                if (!IsHr()) return NotHr();
                return Ok(await _payslipRepository.GetHrPayslips(request));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpPost("SavePayslip")]
        public async Task<IActionResult> SavePayslip([FromBody] SavePayslipRequest request)
        {
            try
            {
                if (!IsHr()) return NotHr();
                return Ok(await _payslipRepository.SavePayslip(request));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpPost("PublishPayslips")]
        public async Task<IActionResult> PublishPayslips([FromBody] PayslipMonthRequest request)
        {
            try
            {
                if (!IsHr()) return NotHr();
                return Ok(await _payslipRepository.PublishPayslips(request));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpPost("UnpublishPayslip")]
        public async Task<IActionResult> UnpublishPayslip([FromBody] PayslipDetailRequest request)
        {
            try
            {
                if (!IsHr()) return NotHr();
                return Ok(await _payslipRepository.UnpublishPayslip(request.PayslipId));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpPost("DeletePayslip")]
        public async Task<IActionResult> DeletePayslip([FromBody] PayslipDetailRequest request)
        {
            try
            {
                if (!IsHr()) return NotHr();
                return Ok(await _payslipRepository.DeletePayslip(request.PayslipId));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }
    }
}

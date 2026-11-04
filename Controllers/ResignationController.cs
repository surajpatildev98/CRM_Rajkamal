using CRMWebServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ResignationApi.Helpers;
using ResignationApi.Models;

namespace CRMWebServices.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ResignationController : ControllerBase
    {
        private readonly IResignationRepository _repo;
        private readonly IConfiguration _config;
        private readonly ILogger<ResignationController> _logger;

        public ResignationController(
            IResignationRepository repo,
            IConfiguration config,
            ILogger<ResignationController> logger)
        {
            _repo = repo;
            _config = config;
            _logger = logger;
        }

        // Employee ID comes from the request.
        private bool IsHr => User.IsHr(_config);

        /// <summary>
        /// Validates employee ID supplied by the API request.
        /// </summary>
        private IActionResult? ValidateEmployeeId(int employeeId)
        {
            if (employeeId > 0)
                return null;

            return BadRequest(new ApiResponse<object>
            {
                Status = false,
                Message = "Valid employeeId is required.",
                Data = null
            });
        }

        /// <summary>
        /// Wraps every call in the { status, message, data } envelope.
        /// </summary>
        private async Task<IActionResult> Run<T>(
            Func<Task<T>> action,
            string message = "Success")
        {
            try
            {
                var data = await action();

                return Ok(new ApiResponse<T>
                {
                    Status = true,
                    Message = message,
                    Data = data
                });
            }
            catch (SqlException ex) when (ex.Number >= 50000)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Status = false,
                    Message = ex.Message,
                    Data = null
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Status = false,
                    Message = ex.Message,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resignation API error");

                return StatusCode(500, new ApiResponse<object>
                {
                    Status = false,
                    Message = "Something went wrong. Please try again.",
                    Data = null
                });
            }
        }

        /* ======================= Employee ======================= */

        [HttpGet("Reasons")]
        public Task<IActionResult> Reasons()
            => Run(() => _repo.GetReasonsAsync());

        [HttpGet("NoticePolicy")]
        public Task<IActionResult> NoticePolicy(
            [FromQuery] int employeeId)
        {
            var error = ValidateEmployeeId(employeeId);
            if (error != null)
                return Task.FromResult(error);

            return Run(() => _repo.GetNoticePolicyAsync(employeeId));
        }

        [HttpGet("My")]
        public Task<IActionResult> My(
            [FromQuery] int employeeId)
        {
            var error = ValidateEmployeeId(employeeId);
            if (error != null)
                return Task.FromResult(error);

            return Run(() => _repo.GetMyAsync(employeeId));
        }

        [HttpPost("Apply")]
        public Task<IActionResult> Apply(
     [FromBody] ApplyResignationRequest req)
        {
            var error = ValidateEmployeeId(req.EmployeeId);
            if (error != null)
                return Task.FromResult(error);

            return Run(async () =>
            {
                int employeeId = req.EmployeeId;

                // Calculate notice period and approvers on the server.
                var policy = await _repo.GetNoticePolicyAsync(employeeId);

                var (managerId, hrId) =
                    await _repo.GetApproversAsync(employeeId);

                return await _repo.ApplyAsync(
                    employeeId,
                    req,
                    policy.NoticePeriodDays,
                    managerId,
                    hrId);
            }, "Resignation submitted.");
        }

        [HttpPost("Withdraw")]
        public Task<IActionResult> Withdraw(
            [FromBody] WithdrawRequest req)
        {
            var error = ValidateEmployeeId(req.EmployeeId);
            if (error != null)
                return Task.FromResult(error);

            return Run<object?>(async () =>
            {
                await _repo.WithdrawAsync(req.EmployeeId, req);
                return null;
            }, "Resignation withdrawn.");
        }

        /* ======================= Approver (Manager / HR) ======================= */

        [HttpGet("Team")]
        public Task<IActionResult> Team(
            [FromQuery] int employeeId)
        {
            var error = ValidateEmployeeId(employeeId);
            if (error != null)
                return Task.FromResult(error);

            return Run(() => _repo.GetTeamAsync(employeeId, IsHr));
        }

        [HttpGet("Details")]
        public Task<IActionResult> Details(
            [FromQuery] int employeeId,
            [FromQuery] int resignationId)
        {
            var error = ValidateEmployeeId(employeeId);
            if (error != null)
                return Task.FromResult(error);

            if (resignationId <= 0)
            {
                return Task.FromResult<IActionResult>(
                    BadRequest(new ApiResponse<object>
                    {
                        Status = false,
                        Message = "Valid resignationId is required.",
                        Data = null
                    }));
            }

            return Run(() =>
                _repo.GetDetailsAsync(employeeId, resignationId, IsHr));
        }
        [HttpPost("Approve")]
        public Task<IActionResult> Approve(
            [FromBody] ApproveRequest req)
        {
            var error = ValidateEmployeeId(req.EmployeeId);
            if (error != null)
                return Task.FromResult(error);

            return Run<object?>(async () =>
            {
                await _repo.ApproveAsync(req.EmployeeId, req);
                return null;
            }, "Resignation approved.");
        }

        [HttpPost("Reject")]
        public Task<IActionResult> Reject(
            [FromBody] RejectRequest req)
        {
            var error = ValidateEmployeeId(req.EmployeeId);
            if (error != null)
                return Task.FromResult(error);

            return Run<object?>(async () =>
            {
                await _repo.RejectAsync(req.EmployeeId, req);
                return null;
            }, "Resignation rejected.");
        }

        [HttpPost("UpdateClearance")]
        public Task<IActionResult> UpdateClearance(
            [FromBody] UpdateClearanceRequest req)
        {
            var error = ValidateEmployeeId(req.EmployeeId);
            if (error != null)
                return Task.FromResult(error);

            return Run(
                () => _repo.UpdateClearanceAsync(
                    req.EmployeeId,
                    IsHr,
                    req),
                "Clearance updated.");
        }
    }
}


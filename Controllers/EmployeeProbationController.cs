using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CRMWebServices.Models.Employee_Probation.EmployeeProbationModels;

namespace CRMWebServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeeProbationController : ControllerBase
    {
        private readonly IEmployeeProbationRepository _repository;

        public EmployeeProbationController(
            IEmployeeProbationRepository repository)
        {
            _repository = repository;
        }

        // ============================================================
        // GET EMPLOYEES
        // ============================================================

        [HttpGet("employees")]
        public async Task<IActionResult> GetEmployees()
        {
            try
            {
                var response = await _repository.GetProbationEmployees();

                return response.Status
                    ? Ok(response)
                    : BadRequest(response);
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

        // ============================================================
        // GET EMPLOYEE PROBATION DETAIL
        // ============================================================

        [HttpGet("detail/{employeeId:int}")]
        public async Task<IActionResult> GetEmployeeProbation(
            int employeeId)
        {
            try
            {
                var response =
                    await _repository.GetEmployeeProbation(employeeId);

                return response.Status
                    ? Ok(response)
                    : NotFound(response);
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

        // ============================================================
        // GET PROBATION HISTORY
        // ============================================================

        [HttpGet("history/{employeeId:int}")]
        public async Task<IActionResult> GetProbationHistory(
            int employeeId)
        {
            try
            {
                var response =
                    await _repository.GetProbationHistory(employeeId);

                return response.Status
                    ? Ok(response)
                    : BadRequest(response);
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

        // ============================================================
        // EXTEND PROBATION
        // ============================================================

        [HttpPost("extend")]
        public async Task<IActionResult> ExtendProbation(
            [FromBody] EmployeeProbationManagementRequest request)
        {
            if (request == null)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = "Request is required."
                });
            }

            try
            {
                var employeeId = User.EmployeeId();

                var response =
                    await _repository.ExtendProbation(
                        request,
                        employeeId);

                return response.Status
                    ? Ok(response)
                    : BadRequest(response);
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

        // ============================================================
        // CONFIRM EMPLOYEE
        // ============================================================

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmProbation(
            [FromBody] EmployeeProbationManagementRequest request)
        {
            if (request == null)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = "Request is required."
                });
            }

            try
            {
                var employeeId = User.EmployeeId();

                var response =
                    await _repository.ConfirmProbation(
                        request,
                        employeeId);

                return response.Status
                    ? Ok(response)
                    : BadRequest(response);
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

        // ============================================================
        // FAIL PROBATION
        // ============================================================

        [HttpPost("fail")]
        public async Task<IActionResult> FailProbation(
            [FromBody] EmployeeProbationManagementRequest request)
        {
            if (request == null)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = "Request is required."
                });
            }

            try
            {
                var employeeId = User.EmployeeId();

                var response =
                    await _repository.FailProbation(
                        request,
                        employeeId);

                return response.Status
                    ? Ok(response)
                    : BadRequest(response);
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

        // ============================================================
        // CANCEL PROBATION
        // ============================================================

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelProbation(
            [FromBody] EmployeeProbationManagementRequest request)
        {
            if (request == null)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = "Request is required."
                });
            }

            try
            {
                var employeeId = User.EmployeeId();

                var response =
                    await _repository.CancelProbation(
                        request,
                        employeeId);

                return response.Status
                    ? Ok(response)
                    : BadRequest(response);
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
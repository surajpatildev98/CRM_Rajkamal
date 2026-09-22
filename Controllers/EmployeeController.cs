using CRMWebServices.Helpers;
using CRMWebServices.Models;
using CRMWebServices.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRMWebServices.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly EmployeeRepository _repository;

        public EmployeeController(EmployeeRepository repository)
        {
            _repository = repository;
        }

        #region  API : api/Employee/Register
        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] EmployeeRegisterRequest request)
        {
            try
            {
                // Trust the token, not the request body, for who created this record
                var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                var response = await _repository.Register(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }
        #endregion

        #region API : api/Employee/NewRegister
        [HttpPost]
        [Route("NewRegister")]
        public async Task<IActionResult> NewRegister([FromBody] EmployeeRegisterRequest request)
        {
            try
            {
                // Trust the token, not the request body, for who created this record
                var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                var response = await _repository.NewRegister(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }
        #endregion

        #region API : api/Employee/GetEmployeeMaster
        [HttpGet]
        [Route("GetEmployeeMaster")]
        public async Task<IActionResult> GetEmployeeMaster()
        {
            try
            {
                var employees = await _repository.GetEmployeeMaster();

                return Ok(new ApiResponse
                {
                    Status = true,
                    Message = "Employees retrieved successfully.",
                    Data = employees
                });
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
        #endregion

        #region API : api/Employee/GetEmployeeDetails/{employeeId}
        [HttpGet]
        [Route("GetEmployeeDetails/{employeeId}")]
        public async Task<IActionResult> GetEmployeeDetails(int employeeId)
        {
            try
            {
                var employee = await _repository.GetEmployeeDetails(employeeId);

                if (employee == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Status = false,
                        Message = "Employee details not found."
                    });
                }

                return Ok(new ApiResponse
                {
                    Status = true,
                    Message = "Employee details retrieved successfully.",
                    Data = employee
                });
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
        #endregion

        #region API : api/Employee/UpdateEmployee
        [HttpPost]
        [Route("UpdateEmployee")]
        public async Task<IActionResult> UpdateEmployee([FromBody] UpdateEmployeeRequest request)
        {
            try
            {
                // Get UpdatedBy from logged-in user's token
                var employeeIdClaim =User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                request.UpdatedBy =employeeIdClaim != null? int.Parse(employeeIdClaim): null;

                var response = await _repository.UpdateEmployee(request);

                return response.Status? Ok(response): BadRequest(response);
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
        #endregion
    }
}
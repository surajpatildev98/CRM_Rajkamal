using CRMWebServices.Helpers;
using CRMWebServices.Models;
using CRMWebServices.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

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
                var loggedInEmployeeId = User.EmployeeId();
                var employees = await _repository.GetEmployeeMaster(loggedInEmployeeId);

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
                var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                request.UpdatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                var response = await _repository.UpdateEmployee(request);

                return response.Status ? Ok(response) : BadRequest(response);
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

        [HttpGet("probation-status")]
        public async Task<IActionResult> GetProbationStatus()
        {
            try
            {
                var dt = await _repository.GetProbationStatus();

                if (dt == null || dt.Rows.Count == 0)
                {
                    return Ok(new ApiResponse
                    {
                        Status = false,
                        Message = "No probation status found.",
                        Data = null
                    });
                }

                var data = new List<object>();

                foreach (DataRow row in dt.Rows)
                {
                    data.Add(new
                    {
                        ProbationStatusId = Convert.ToInt32(row["ProbationStatusId"]),
                        StatusName = row["StatusName"]?.ToString()
                    });
                }

                return Ok(new ApiResponse
                {
                    Status = true,
                    Message = "Probation status loaded successfully.",
                    Data = data
                });
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

        [HttpGet("hr-managers")]
        public async Task<IActionResult> GetHRManagersByOffice([FromQuery] int officeId)
        {
            try
            {
                var result = await _repository.GetHRManagersByOffice(officeId);

                var data = result.AsEnumerable().Select(row => new
                {
                    employeeId = Convert.ToInt32(row["EmployeeId"]),
                    employeeCode = row["EmployeeCode"]?.ToString(),
                    firstName = row["FirstName"]?.ToString(),
                    lastName = row["LastName"] == DBNull.Value
                        ? null : row["LastName"]?.ToString()
                }).ToList();

                return Ok(new ApiResponse
                {
                    Status = true,
                    Message = "HR managers fetched successfully.",
                    Data = data
                });
            }
            catch (Exception ex)
            {
                // Log the original exception; do not expose internal details in production.
                HttpContext.RequestServices.GetRequiredService<ILogger<EmployeeController>>()
                    .LogError(ex, "Failed to fetch HR managers for office {OfficeId}",
                        officeId);

                return StatusCode(500, new ApiResponse
                {
                    Status = false,
                    Message = "Failed to fetch HR managers."
                });
            }
        }

    }
}
using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRMWebServices.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardRepository _repository;

        public DashboardController(IDashboardRepository repository)
        {
            _repository = repository;
        }

        //[HttpGet]
        //[Route("Details")]
        //public async Task<IActionResult> Details([FromQuery] int? employeeId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        //{
        //    var response = await _repository.GetDashboardFull(employeeId, fromDate, toDate);
        //    return response.Status ? Ok(response) : BadRequest(response);
        //}

        [HttpGet]
        [Route("Details")]
        public async Task<IActionResult> Details(
            [FromQuery] int? employeeId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                // Non-admin/manager can only view their own dashboard
                if (!User.HasRole(RoleIds.Admin, RoleIds.Manager))
                {
                    employeeId = User.EmployeeId();
                }

                var response = await _repository.GetDashboardFull(employeeId, fromDate, toDate);

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

        //[HttpGet]
        //[Route("employee-performance")]
        //public async Task<IActionResult> Performance()
        //{
        //    var response = await _repository.GetEmployeePerformance();
        //    return response.Status ? Ok(response) : BadRequest(response);
        //}

        [HttpGet]
        [Route("employee-performance")]
        public async Task<IActionResult> Performance()
        {
            try
            {
                if (!User.HasRole(RoleIds.Admin, RoleIds.Manager))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ApiResponse
                        {
                            Status = false,
                            Message = "You are not authorized to view employee performance."
                        });
                }

                var response = await _repository.GetEmployeePerformance();

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
    }
}
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
        private readonly ICRMAuthorizationService _authorization;

        public DashboardController(
            IDashboardRepository repository,
            ICRMAuthorizationService authorization)
        {
            _repository = repository;
            _authorization = authorization;
        }

        // ============================================
        // GET DASHBOARD DETAILS
        // - Admin/Manager: Can view all (employeeId = null means all data)
        // - Sales/Others: Can only view their own dashboard
        // ============================================
        [HttpGet]
        [Route("Details")]
        public async Task<IActionResult> Details(
            [FromQuery] int? employeeId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                var loggedInEmployeeId = User.EmployeeId();

                // ✅ Check if user can view all (Admin/Manager)
                bool canViewAll = await _authorization.CanViewAll(User, "Dashboard");

                if (!canViewAll)
                {
                    // ✅ Non-admin/manager can only view their own dashboard
                    employeeId = loggedInEmployeeId;
                }
                else
                {
                    // ✅ Admin/Manager: Show ALL data by default
                    // If Admin provides their own ID, treat as null to show all
                    if (!employeeId.HasValue || employeeId.Value == loggedInEmployeeId)
                    {
                        employeeId = null; // Show ALL data
                    }
                    // Otherwise, show data for the specified employee
                }

                var response = await _repository.GetDashboardFull(employeeId, fromDate, toDate);

                // ✅ If non-admin and no data, show friendly message
                if (!canViewAll && response.Status && response.Data == null)
                {
                    response.Message = "No dashboard data available for you.";
                }

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

        // ============================================
        // GET MY DASHBOARD (Shortcut for current user)
        // - Always returns data for logged-in user only
        // ============================================
        [HttpGet]
        [Route("my-dashboard")]
        public async Task<IActionResult> MyDashboard(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                var employeeId = User.EmployeeId();

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

        // ============================================
        // GET EMPLOYEE PERFORMANCE
        // - Admin/Manager: Can view all employee performance
        // - Others: Not authorized (403)
        // ============================================
        [HttpGet]
        [Route("employee-performance")]
        public async Task<IActionResult> Performance()
        {
            try
            {
                // ✅ Check if user has permission to view employee performance
                bool canViewAll = await _authorization.CanViewAll(User, "Dashboard");

                if (!canViewAll)
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ApiResponse
                        {
                            Status = false,
                            Message = "You are not authorized to view employee performance. Only Admin and Manager can access this."
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
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

        #region  API : api/dashboard/details
        [HttpGet]
        [Route("Details")]
        public async Task<IActionResult> Details([FromQuery] int? employeeId,[FromQuery] DateTime? fromDate,[FromQuery] DateTime? toDate)
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
        #endregion

        #region  API : api/dashboard/my-dashboard
        [HttpGet]
        [Route("my-dashboard")]
        public async Task<IActionResult> MyDashboard([FromQuery] DateTime? fromDate,[FromQuery] DateTime? toDate)
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
        #endregion

        #region  API : api/dashboard/employee-performance
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
        #endregion

        #region  API : api/dashboard/hrms-details
        [HttpGet]
        [Route("hrms-details")]
        public async Task<IActionResult> HrmsDetails()
        {
            try
            {
                var loggedInEmployeeId = User.EmployeeId();

                // Admin/HR -> org-wide. Everyone else -> SP decides TEAM (has reportees) or SELF
                bool isAdmin = await _authorization.CanViewAll(User, "HRMSDashboard");

                var response = await _repository.GetHrmsDashboardFull(loggedInEmployeeId, isAdmin);

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
    }
}
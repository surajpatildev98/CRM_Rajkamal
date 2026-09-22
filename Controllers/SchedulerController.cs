using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CRMWebServices.Models.SchedulerModels;

namespace CRMWebServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SchedulerController : ControllerBase
    {
        private readonly ISchedulerRepository _repository;

        public SchedulerController(
            ISchedulerRepository repository)
        {
            _repository = repository;
        }
 
        // ADMIN CHECK
        private bool IsAdmin()
        {
            var roleIdsClaim =User.FindFirst("RoleIds")?.Value;
            if (string.IsNullOrEmpty(roleIdsClaim))
                return false;

            return roleIdsClaim.Split(',',StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).Contains("1");
        }
 
        #region GET Entity Types
        [HttpGet("entity-types")]
        public async Task<IActionResult> GetSchedulerEntityTypes()
        {
            if (!IsAdmin())
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new ApiResponse
                    {
                        Status = false,
                        Message = "Not authorized."
                    });
            }

            var response =
                await _repository.GetSchedulerEntityTypes();

            return response.Status
                ? Ok(response)
                : BadRequest(response);
        }
        #endregion

        #region GET Scheduler Scopes
        [HttpGet("scopes")]
        public async Task<IActionResult> GetSchedulerScopes([FromQuery] string entityType)
        {
            if (!IsAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ApiResponse
                    {
                        Status = false,
                        Message = "Not authorized."
                    });
            }

            if (string.IsNullOrWhiteSpace(entityType))
            {
                return BadRequest(
                    new ApiResponse
                    {
                        Status = false,
                        Message = "EntityType is required."
                    });
            }

            var response =await _repository.GetSchedulerScopes(entityType);
            return response.Status? Ok(response): BadRequest(response);
        }
        #endregion

        #region GET Jobs
        [HttpGet("jobsList")]
        public async Task<IActionResult> GetJobs()
        {
            if (!IsAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ApiResponse
                    {
                        Status = false,
                        Message = "Not authorized."
                    });
            }

            var response =await _repository.GetJobs();
            return response.Status? Ok(response): BadRequest(response);
        }
        #endregion

        #region GET Job Details
        [HttpGet("jobDetails/{schedulerJobId}")]
        public async Task<IActionResult> GetJob(int schedulerJobId)
        {
            if (!IsAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ApiResponse
                    {
                        Status = false,
                        Message = "Not authorized."
                    });
            }

            var response =await _repository.GetJob(schedulerJobId);
            return response.Status? Ok(response): NotFound(response);
        }

        #endregion

        #region CREATE Job
        [HttpPost("job/create")]
        public async Task<IActionResult> CreateJob([FromBody] CreateSchedulerJobRequest request)
        {
            if (!IsAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ApiResponse
                    {
                        Status = false,
                        Message = "Not authorized."
                    });
            }

            try
            {
                var employeeId =User.EmployeeId();
                var response =await _repository.CreateJob(request,employeeId);
                return response.Status? Ok(response): BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new ApiResponse
                    {
                        Status = false,
                        Message = $"Error: {ex.Message}"
                    });
            }
        }
        #endregion

        #region UPDATE Job
        [HttpPost("job/update")]
        public async Task<IActionResult> UpdateJob([FromBody] UpdateSchedulerJobRequest request)
        {
            if (!IsAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ApiResponse
                    {
                        Status = false,
                        Message = "Not authorized."
                    });
            }

            try
            {
                var employeeId = User.EmployeeId();
                var response =await _repository.UpdateJob(request,employeeId);
                return response.Status? Ok(response): BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new ApiResponse
                    {
                        Status = false,
                        Message = ex.Message
                    });
            }
        }
        #endregion

        #region ENABLE Job
        [HttpPost("job/enable")]
        public async Task<IActionResult> EnableJob(int schedulerJobId)
        {
            if (!IsAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ApiResponse
                    {
                        Status = false,
                        Message = "Not authorized."
                    });
            }

            try
            {
                var employeeId =User.EmployeeId();
                var response =await _repository.EnableJob(schedulerJobId,employeeId);
                return response.Status? Ok(response): BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new ApiResponse
                    {
                        Status = false,
                        Message = $"Error: {ex.Message}"
                    });
            }
        }
        #endregion

        #region DISABLE Job
        [HttpPost("job/disable")]
        public async Task<IActionResult> DisableJob(int schedulerJobId)
        {
            if (!IsAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ApiResponse
                    {
                        Status = false,
                        Message = "Not authorized."
                    });
            }

            try
            {
                var employeeId =User.EmployeeId();
                var response =await _repository.DisableJob(schedulerJobId,employeeId);
                return response.Status? Ok(response): BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new ApiResponse
                    {
                        Status = false,
                        Message = $"Error: {ex.Message}"
                    });
            }
        }
        #endregion
    }
}
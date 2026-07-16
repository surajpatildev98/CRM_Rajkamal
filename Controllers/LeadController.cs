using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRMWebServices.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LeadController : ControllerBase
    {
        private readonly ILeadRepository _repository;

        public LeadController(ILeadRepository repository)
        {
            _repository = repository;
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] LeadRegisterRequest request)
        {
            try
            {
                var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                var response = await _repository.CreateLead(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpPost]
        [Route("Transfer")]
        public async Task<IActionResult> Transfer([FromBody] LeadTransferRequest request)
        {
            try
            {
                var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                var response = await _repository.TransferLead(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpPost]
        [Route("UpdateStatus")]
        public async Task<IActionResult> UpdateStatus([FromBody] LeadStatusUpdateRequest request)
        {
            try
            {
                var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                var response = await _repository.UpdateLeadStatus(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }


        //[HttpPost]
        //[Route("CreateActivity")]
        //public async Task<IActionResult> Create([FromBody] LeadActivityRequest request)
        //{
        //    try
        //    {
        //        var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
        //        request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

        //        var response = await _repository.CreateActivity(request);
        //        return response.Status ? Ok(response) : BadRequest(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
        //    }
        //}

        [HttpPost]
        [Route("CreateActivity")]
        public async Task<IActionResult> CreateActivity([FromBody] LeadActivityRequest request)
        {
            try
            {
                var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                var response = await _repository.CreateActivity(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpPost]
        [Route("CreateFollowUp")]
        public async Task<IActionResult> CreateFollowUp([FromBody] LeadFollowUpRequest request)
        {
            try
            {
                var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                var response = await _repository.CreateFollowUp(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpGet]
        [Route("Details")]
        public async Task<IActionResult> Details([FromQuery] int? leadId, [FromQuery] LeadListFilterRequest filter)
        {
            if (leadId == null)
            {
                // No leadId provided -> return the list of all leads (with optional filters)
                var listResponse = await _repository.GetLeadList(filter);
                return Ok(listResponse);
            }
            else
            {
                // leadId provided -> return full details for that specific lead
                var detailsResponse = await _repository.GetLeadFullDetails(leadId.Value);
                return detailsResponse.Status ? Ok(detailsResponse) : NotFound(detailsResponse);
            }
        }


    }
}
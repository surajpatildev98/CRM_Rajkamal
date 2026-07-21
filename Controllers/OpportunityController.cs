using CRMWebServices.Helpers;
using CRMWebServices.Models;
using CRMWebServices.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using static CRMWebServices.Models.OpportunityModels;

namespace CRMWebServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OpportunityController : ControllerBase
    {
        private readonly IOpportunityRepository _repository;

        public OpportunityController(IOpportunityRepository repository)
        {
            _repository = repository;
        }

       
        // GET: api/opportunity/wonleads
        // - No leadId: Returns list of WON leads (conversion page)
        // - With leadId: Returns full READ-ONLY lead details
       
        [HttpGet]
        [Route("wonleads")]
        public async Task<IActionResult> GetWonLeads([FromQuery] int? leadId, [FromQuery] string? searchText)
        {
            try
            {
                if (leadId == null)
                {
                    // No leadId -> return list of all WON leads
                    var response = await _repository.GetWonLeads(searchText);
                    return response.Status ? Ok(response) : BadRequest(response);
                }
                else
                {
                    // leadId provided -> return full details (READ-ONLY)
                    var response = await _repository.GetWonLeadDetails(leadId.Value);
                    return response.Status ? Ok(response) : NotFound(response);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

       
        // POST: api/opportunity/convert
        // Converts WON lead to Opportunity
       
        [HttpPost]
        [Route("convert")]
        public async Task<IActionResult> ConvertLead([FromBody] ConvertLeadRequest request)
        {
            try
            {
                // Get EmployeeId from JWT token
                var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                var response = await _repository.ConvertLeadToOpportunity(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        // POST: api/opportunity/transfer
        // Transfers an opportunity to another employee

        [HttpPost]
        [Route("Transfer")]
        public async Task<IActionResult> Transfer([FromBody] OpportunityTransferRequest request)
        {
            try
            {
                var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                var response = await _repository.TransferOpportunity(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        // GET: api/opportunity/list
        // Returns list of all opportunities with filters

        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> GetOpportunityList([FromQuery] OpportunityListRequest request)
        {
            try
            {
                var response = await _repository.GetOpportunityList(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

       
        // GET: api/opportunity/details/{id}
        // Returns full details of a specific opportunity
       
        [HttpGet]
        [Route("details/{opportunityId}")]
        public async Task<IActionResult> GetOpportunityDetails(int opportunityId)
        {
            try
            {
                var response = await _repository.GetOpportunityDetails(opportunityId);
                return response.Status ? Ok(response) : NotFound(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

       
        // PUT: api/opportunity/update
        // Updates an existing opportunity
       
        [HttpPut]
        [Route("update")]
        public async Task<IActionResult> UpdateOpportunity([FromBody] UpdateOpportunityRequest request)
        {
            try
            {
                // Get EmployeeId from JWT token
                var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                var response = await _repository.UpdateOpportunity(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

       
        // DELETE: api/opportunity/delete
        // Soft deletes an opportunity
       
        [HttpDelete]
        [Route("delete")]
        public async Task<IActionResult> DeleteOpportunity([FromBody] DeleteOpportunityRequest request)
        {
            try
            {
                // Get EmployeeId from JWT token
                var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                var response = await _repository.DeleteOpportunity(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

       
        // DROPDOWN ENDPOINTS
       

        [HttpGet]
        [Route("types")]
        public async Task<IActionResult> GetOpportunityTypes()
        {
            try
            {
                var response = await _repository.GetOpportunityTypes();
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpGet]
        [Route("stages")]
        public async Task<IActionResult> GetOpportunityStages()
        {
            try
            {
                var response = await _repository.GetOpportunityStages();
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpGet]
        [Route("priorities")]
        public async Task<IActionResult> GetPriorities()
        {
            try
            {
                var response = await _repository.GetPriorities();
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }
    }
}
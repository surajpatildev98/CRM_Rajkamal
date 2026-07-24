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
        private readonly ICRMAuthorizationService _authorization;

        public LeadController(ILeadRepository repository, ICRMAuthorizationService authorization)
        {
            _repository = repository;
            _authorization = authorization;
        }
     
        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] LeadRegisterRequest request)
        {
            try
            {
                //var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                //request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                //var response = await _repository.CreateLead(request);
                if (!_authorization.CanCreateLead(User, request.AssignedToEmployeeId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to create this lead."
                    });
                }

                // Logged-in Employee
                request.CreatedBy = User.EmployeeId();

                // Save Lead
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
                //var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                //request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                //var response = await _repository.TransferLead(request);

                if (!await _authorization.CanModifyLead(User, request.Ref_LeadId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to transfer this lead."
                    });
                }

                request.CreatedBy = User.EmployeeId();

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
                //var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                //request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                //var response = await _repository.UpdateLeadStatus(request);

                if (!await _authorization.CanModifyLead(User, request.Ref_LeadId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to update this lead."
                    });
                }

                request.CreatedBy = User.EmployeeId();

                var response = await _repository.UpdateLeadStatus(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }



        [HttpPost]
        [Route("CreateActivity")]
        public async Task<IActionResult> CreateActivity([FromBody] LeadActivityRequest request)
        {
            try
            {
                //var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                //request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                //var response = await _repository.CreateActivity(request);

                if (!await _authorization.CanModifyLead(User, request.Ref_LeadId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to create activity."
                    });
                }

                request.CreatedBy = User.EmployeeId();

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
                //var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                //request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                //var response = await _repository.CreateFollowUp(request);
                if (!await _authorization.CanModifyLead(User, request.Ref_LeadId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to create follow-up."
                    });
                }

                request.CreatedBy = User.EmployeeId();

                var response = await _repository.CreateFollowUp(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }


        [HttpPost]
        [Route("UpdateFollowUpStatus")]
        public async Task<IActionResult> UpdateFollowUpStatus([FromBody] FollowUpStatusUpdateRequest request)
        {
            try
            {
                //var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                //request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                if (!await _authorization.CanModifyLead(User, request.Ref_LeadId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to update this follow-up."
                    });
                }

                request.CreatedBy = User.EmployeeId();

                var response = await _repository.UpdateFollowUpStatus(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }       


        [HttpPost]
        [Route("CloseLead")]
        public async Task<IActionResult> Close([FromBody] LeadCloseRequest request)
        {
            try
            {
                //var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
                //request.CreatedBy = employeeIdClaim != null ? int.Parse(employeeIdClaim) : null;

                //var response = await _repository.CloseLead(request);
                if (!await _authorization.CanModifyLead(User, request.Ref_LeadId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to close this lead."
                    });
                }

                request.CreatedBy = User.EmployeeId();

                var response = await _repository.CloseLead(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        //[HttpGet]
        //[Route("Details")]
        //public async Task<IActionResult> Details([FromQuery] int? leadId, [FromQuery] LeadListFilterRequest filter)
        //{
        //    if (leadId == null)
        //    {
        //        // No leadId provided -> return the list of all leads (with optional filters)
        //        var listResponse = await _repository.GetLeadList(filter);
        //        return Ok(listResponse);
        //    }
        //    else
        //    {   
        //        // leadId provided -> return full details for that specific lead
        //        var detailsResponse = await _repository.GetLeadFullDetails(leadId.Value);
        //        return detailsResponse.Status ? Ok(detailsResponse) : NotFound(detailsResponse);
        //    }
        //}

        [HttpGet]
        [Route("Details")]
        public async Task<IActionResult> Details([FromQuery] int? leadId, [FromQuery] LeadListFilterRequest filter)
        {
            try
            {
                // Lead List
                if (leadId == null)
                {
                    // Non-admin users should only see their own leads
                    if (!User.HasRole(RoleIds.Admin, RoleIds.Manager))
                    {
                        //filter.FilterAssignedToId = User.EmployeeId();
                        filter.LoggedInEmployeeId = User.EmployeeId();
                    }

                    var listResponse = await _repository.GetLeadList(filter);

                    return Ok(listResponse);
                }

                // Single Lead Authorization
                if (!await _authorization.CanAccessLead(User, leadId.Value))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to view this lead."
                    });
                }

                var detailsResponse = await _repository.GetLeadFullDetails(leadId.Value);

                return detailsResponse.Status
                    ? Ok(detailsResponse)
                    : NotFound(detailsResponse);
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
using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
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
        private readonly ICRMAuthorizationService _authorization;

        public OpportunityController(IOpportunityRepository repository, ICRMAuthorizationService authorization)
        {
            _repository = repository;
            _authorization = authorization;
        }

        #region API : api/opportunity/wonleads?leadId={leadId}&searchText={searchText}
        [HttpGet]
        [Route("wonleads")]
        public async Task<IActionResult> GetWonLeads([FromQuery] int? leadId, [FromQuery] string? searchText)
        {
            try
            {
                

                if (leadId == null)
                {
                    var accessFilter = await _authorization.GetAccessFilter(User, "Lead");

                    int? assignedEmployeeId = accessFilter.CanViewAll ? null : User.EmployeeId();

                    var response = await _repository.GetWonLeads(searchText, assignedEmployeeId);
                    return response.Status ? Ok(response) : BadRequest(response);
                }

                // ✅ Use "Lead" for Lead resource
                if (!await _authorization.CanPerformAction(User,"Lead","View",leadId.Value))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ApiResponse
                        {
                            Status = false,
                            Message = "You are not authorized to view this lead."
                        });
                }

                var details = await _repository.GetWonLeadDetails(leadId.Value);
                return details.Status ? Ok(details) : NotFound(details);
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

        #region API : api/opportunity/details/{opportunityId}
        [HttpGet]
        [Route("details/{opportunityId}")]
        public async Task<IActionResult> GetOpportunityDetails(int opportunityId)
        {
            try
            {
                // ✅ Use "Opp" for Opportunity resource
                if (!await _authorization.CanPerformAction(
                    User,
                    "Opp",
                    "View",
                    opportunityId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ApiResponse
                        {
                            Status = false,
                            Message = "You are not authorized to view this opportunity."
                        });
                }

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
        #endregion

        #region API : api/opportunity/list?searchText=
        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> GetOpportunityList([FromQuery] OpportunityListRequest request)
        {
            try
            {
                // ✅ Use "Opp" for Opportunity resource
                var accessFilter = await _authorization.GetAccessFilter(User, "Opp");

                if (!accessFilter.CanViewAll)
                {
                    request.FilterOwnerId = accessFilter.EmployeeId;
                    request.ShowCreatedAndAssigned = accessFilter.ShowCreatedAndAssigned;
                }

                var response = await _repository.GetOpportunityList(request);

                if (response.Status &&
                    !accessFilter.CanViewAll &&
                    response.Data is List<OpportunityListResponse> list &&
                    list.Count == 0)
                {
                    response.Message = "No opportunities assigned to you.";
                }

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
        #endregion

        #region API : api/opportunity/create
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
        #endregion

        #region API : api/opportunity/stages
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
        #endregion

        #region API : api/opportunity/priorities
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
        #endregion

        #region API : api/opportunity/close-stages
        [HttpGet]
        [Route("closestages")]
        public async Task<IActionResult> GetOpportunityCloseStages()
        {
            try
            {
                var response = await _repository.GetOpportunityCloseStages();
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = $"Error: {ex.Message}" });
            }
        }
        #endregion

        #region API : api/opportunity/convert
        [HttpPost]
        [Route("convert")]
        public async Task<IActionResult> ConvertLead([FromBody] ConvertLeadRequest request)
        {
            try
            {
                // ✅ Use "Lead" for Lead resource
                if (!await _authorization.CanPerformAction(
                    User,
                    "Lead",
                    "Convert",
                    request.Ref_LeadId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ApiResponse
                        {
                            Status = false,
                            Message = "Only the Lead owner, Manager or Administrator can convert this Lead."
                        });
                }

                request.CreatedBy = User.EmployeeId();
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
        #endregion

        #region API : api/opportunity/transfer
        [HttpPost]
        [Route("Transfer")]
        public async Task<IActionResult> Transfer([FromBody] OpportunityTransferRequest request)
        {
            try
            {
                // ✅ Use "Opp" for Opportunity resource
                if (!await _authorization.CanPerformAction(
                    User,
                    "Opp",
                    "Transfer",
                    request.Ref_OpportunityId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ApiResponse
                        {
                            Status = false,
                            Message = "You are not authorized to transfer this opportunity."
                        });
                }

                request.CreatedBy = User.EmployeeId();
                var response = await _repository.TransferOpportunity(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }
        #endregion

        #region API : api/opportunity/requirement/{opportunityId}
        [HttpPost]
        [Route("add_requirement")]
        public async Task<IActionResult> AddOpportunityRequirement([FromBody] AddOpportunityRequirementRequest request)
        {
            try
            {
                if (!await _authorization.CanPerformAction(
                    User,
                    "Opp",
                    "Edit",
                    request.Ref_OpportunityId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ApiResponse
                        {
                            Status = false,
                            Message = "You are not authorized to add requirements to this opportunity."
                        });
                }

                request.CreatedBy = User.EmployeeId();
                var response = await _repository.AddOpportunityRequirement(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = $"Error: {ex.Message}" });
            }
        }
        #endregion

        #region API : api/opportunity/update_requirement
        [HttpPost]
        [Route("update_requirement")]
        public async Task<IActionResult> UpdateOpportunityRequirement([FromBody] UpdateOpportunityRequirementRequest request)
        {
            try
            {
                if (!await _authorization.CanPerformAction(
                    User,
                    "Opp",
                    "Edit",
                    request.Ref_OpportunityId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ApiResponse
                        {
                            Status = false,
                            Message = "You are not authorized to update requirements on this opportunity."
                        });
                }

                request.CreatedBy = User.EmployeeId();
                var response = await _repository.UpdateOpportunityRequirement(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = $"Error: {ex.Message}" });
            }
        }
        #endregion

        #region API : api/opportunity/add_activity
        [HttpPost]
        [Route("add_activity")]
        public async Task<IActionResult> CreateOpportunityActivity([FromBody] CreateOpportunityActivityRequest request)
        {
            try
            {
                if (!await _authorization.CanPerformAction(User,"Opp","Activity",request.Ref_OpportunityId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to log activity on this opportunity."
                    });
                }

                request.CreatedBy = User.EmployeeId();
                var response = await _repository.CreateOpportunityActivity(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = $"Error: {ex.Message}" });
            }
        }
        #endregion

        #region API : api/opportunity/add_followup
        [HttpPost]
        [Route("add_followup")]
        public async Task<IActionResult> CreateOpportunityFollowUp([FromBody] CreateOpportunityFollowUpRequest request)
        {
            try
            {
                if (!await _authorization.CanPerformAction(
                    User,
                    "Opp",
                    "FollowUp",
                    request.Ref_OpportunityId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ApiResponse
                        {
                            Status = false,
                            Message = "You are not authorized to schedule follow-ups on this opportunity."
                        });
                }

                request.CreatedBy = User.EmployeeId();
                var response = await _repository.CreateOpportunityFollowUp(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = $"Error: {ex.Message}" });
            }
        }
        #endregion

        #region API : api/opportunity/close 
        [HttpPost]
        [Route("close")]
        public async Task<IActionResult> CloseOpportunity([FromBody] CloseOpportunityRequest request)
        {
            try
            {
                if (!await _authorization.CanPerformAction(User,"Opp","Close",request.Ref_OpportunityId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to close this opportunity."
                    });
                }

                request.CreatedBy = User.EmployeeId();
                var response = await _repository.CloseOpportunity(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = $"Error: {ex.Message}" });
            }
        }
        #endregion

        #region API : api/opportunity/Update-FollowUp-Status
        [HttpPost]
        [Route("Update-FollowUp-Status")]
        public async Task<IActionResult> UpdateOpportunityFollowUpStatus([FromBody] UpdateOpportunityFollowUpStatusRequest request)
        {
            try
            {
                if (!await _authorization.CanPerformAction(
                    User,
                    "Opp",
                    "FollowUp",
                    request.Ref_OpportunityId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ApiResponse
                        {
                            Status = false,
                            Message = "You are not authorized to update follow-up status on this opportunity."
                        });
                }

                request.CreatedBy = User.EmployeeId();
                var response = await _repository.UpdateOpportunityFollowUpStatus(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = $"Error: {ex.Message}" });
            }
        }
        #endregion

        #region API : api/opportunity/update
        [HttpPut]
        [Route("update")]
        public async Task<IActionResult> UpdateOpportunity([FromBody] UpdateOpportunityRequest request)
        {
            try
            {
                // ✅ Use "Opp" for Opportunity resource
                if (!await _authorization.CanPerformAction(
                    User,
                    "Opp",
                    "Edit",
                    request.Ref_OpportunityId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ApiResponse
                        {
                            Status = false,
                            Message = "You are not authorized to update this opportunity."
                        });
                }

                request.CreatedBy = User.EmployeeId();
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
        #endregion

        #region API : api/opportunity/delete
        [HttpDelete]
        [Route("delete")]
        public async Task<IActionResult> DeleteOpportunity([FromBody] DeleteOpportunityRequest request)
        {
            try
            {
                // ✅ Use "Opp" for Opportunity resource
                if (!await _authorization.CanPerformAction(
                    User,
                    "Opp",
                    "Delete",
                    request.Ref_OpportunityId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ApiResponse
                        {
                            Status = false,
                            Message = "You are not authorized to delete this opportunity."
                        });
                }

                request.CreatedBy = User.EmployeeId();
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
        #endregion

        #region API : api/opportunity/delete-requirement
        [HttpPost]
        [Route("delete-requirement")]
        public async Task<IActionResult> DeleteOpportunityRequirement([FromBody] DeleteOpportunityRequirementRequest request)
        {
            try
            {
                if (!await _authorization.CanPerformAction(User, "Opp", "Edit", request.Ref_OpportunityId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ApiResponse
                        {
                            Status = false,
                            Message = "You are not authorized to delete this requirement."
                        });
                }

                request.CreatedBy = User.EmployeeId();

                var response = await _repository.DeleteOpportunityRequirement(request);

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
        #endregion

        #region API : api/opportunity/ActivityExpenseMasters

        [HttpPost]
        [Route("ActivityExpenseMasters")]
        public async Task<IActionResult> AddOpportunityActivityExpense(
            [FromBody] AddOpportunityActivityExpenseRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Request is required."
                    });
                }

                if (!await _authorization.CanPerformAction(
                    User,
                    "Opp",
                    "Activity",
                    request.Ref_OpportunityId))
                {
                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        new ApiResponse
                        {
                            Status = false,
                            Message =
                                "You are not authorized to add expense to this opportunity activity."
                        });
                }

                request.CreatedBy = User.EmployeeId();

                var response =
                    await _repository.AddOpportunityActivityExpenses(request);

                return response.Status
                    ? Ok(response)
                    : BadRequest(response);
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

        #endregion
    }
}
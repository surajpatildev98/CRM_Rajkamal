
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

        #region // API : api/Lead/Register

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] LeadRegisterRequest request)
        {
            try
            {
                // Check if user can create lead with assignment
                if (!await _authorization.CanPerformActionWithAssignment(
                    User,
                    "Lead",
                    "Create",
                    request.AssignedToEmployeeId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to create this lead."
                    });
                }

                request.CreatedBy = User.EmployeeId();
                var response = await _repository.CreateLead(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }
        #endregion

        #region // API : api/Lead/Transfer
        [HttpPost]
        [Route("Transfer")]
        public async Task<IActionResult> Transfer([FromBody] LeadTransferRequest request)
        {
            try
            {
                // Check if user can assign this lead
                if (!await _authorization.CanPerformAction(
                    User,
                    "Lead",
                    "Transfer",
                    request.Ref_LeadId))
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
        #endregion

        #region // API : api/Lead/UpdateStatus

        [HttpPost]
        [Route("UpdateStatus")]
        public async Task<IActionResult> UpdateStatus([FromBody] LeadStatusUpdateRequest request)
        {
            try
            {
                // Check if user can edit this lead
                if (!await _authorization.CanPerformAction(
                    User,
                    "Lead",
                    "Edit",
                    request.Ref_LeadId))
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
        #endregion

        #region // API : api/Lead/CreateActivity
        [HttpPost]
        [Route("CreateActivity")]
        public async Task<IActionResult> CreateActivity([FromBody] LeadActivityRequest request)
        {
            try
            {
                // Check if user can manage activity on this lead
                if (!await _authorization.CanPerformAction(
                    User,
                    "Lead",
                    "Activity",
                    request.Ref_LeadId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to create activity on this lead."
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
        #endregion

        #region // API : api/Lead/CreateFollowUp
        [HttpPost]
        [Route("CreateFollowUp")]
        public async Task<IActionResult> CreateFollowUp([FromBody] LeadFollowUpRequest request)
        {
            try
            {
                // Check if user can manage follow-up on this lead
                if (!await _authorization.CanPerformAction(
                    User,
                    "Lead",
                    "FollowUp",
                    request.Ref_LeadId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to create follow-up on this lead."
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
        #endregion

        #region // API : api/Lead/UpdateFollowUpStatus
        [HttpPost]
        [Route("UpdateFollowUpStatus")]
        public async Task<IActionResult> UpdateFollowUpStatus([FromBody] FollowUpStatusUpdateRequest request)
        {
            try
            {
                // Check if user can manage follow-up on this lead
                if (!await _authorization.CanPerformAction(
                    User,
                    "Lead",
                    "FollowUpStatus",
                    request.Ref_LeadId))
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
        #endregion

        #region // API : api/Lead/Close
        [HttpPost]
        [Route("CloseLead")]
        public async Task<IActionResult> Close([FromBody] LeadCloseRequest request)
        {
            try
            {
                // Check if user can close this lead
                if (!await _authorization.CanPerformAction(
                    User,
                    "Lead",
                    "Close",
                    request.Ref_LeadId))
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
        #endregion

        #region // API : api/Lead/Details
        [HttpGet]
        [Route("Details")]
        public async Task<IActionResult> Details([FromQuery] int? leadId, [FromQuery] LeadListFilterRequest filter)
        {
            try
            {
                // Case 1: List of leads (no leadId provided)
                if (leadId == null)
                {
                    // Get access filter based on user's permissions
                    //var accessFilter = await _authorization.GetAccessFilter(User, "Lead");

                    //// Apply filter based on user's access level
                    //if (!accessFilter.CanViewAll)
                    //{
                    //    // For Sales: Show only assigned leads
                    //    // For PreSales: Show created AND assigned leads
                    //    filter.LoggedInEmployeeId = accessFilter.EmployeeId;
                    //    filter.ShowCreatedAndAssigned = accessFilter.ShowCreatedAndAssigned;
                    //}
                    //// If CanViewAll = true (Admin/Manager), no filter applied
                    ///
                    filter.LoggedInEmployeeId = User.EmployeeId();

                    var listResponse = await _repository.GetLeadList(filter);
                    return Ok(listResponse);
                }

                // Case 2: Single lead details (leadId provided)
                // Check if user can view this specific lead
                if (!await _authorization.CanPerformAction(
                    User,
                    "Lead",
                    "View",
                    leadId.Value))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to view this lead."
                    });
                }

                var detailsResponse = await _repository.GetLeadFullDetails(leadId.Value);
                return detailsResponse.Status ? Ok(detailsResponse) : NotFound(detailsResponse);
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

        #region // API : api/Lead/Assign
        [HttpPost]
        [Route("Assign")]
        public async Task<IActionResult> Assign([FromBody] LeadAssignRequest request)
        {
            try
            {
                // Check if user can assign this lead (same permission as Transfer — Lead_Assign)
                if (!await _authorization.CanPerformAction(
                    User,
                    "Lead",
                    "Assign",
                    request.Ref_LeadId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to assign this lead."
                    });
                }

                request.CreatedBy = User.EmployeeId();
                var response = await _repository.AssignLead(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }
        #endregion

        #region // API : api/Lead/CheckCustomerExists
        [HttpGet]
        [Route("check-customer-exists")]
        public async Task<IActionResult> CheckCustomer(string? gst, string? customerName, string? pan)
        {
            try
            {
                var response = await _repository.CheckCustomerExists(gst, customerName, pan);

                return response.Status
                    ? Ok(response)
                    : NotFound(response);
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

        #region // API : api/Lead/SearchCustomer
        [HttpGet]
        [Route("search-customer")]
        public async Task<IActionResult> SearchCustomer(string searchText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Search text is required."
                    });
                }

                var response = await _repository.SearchCustomer(searchText);

                return Ok(response);
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

        #region // API : api/Lead/AddActivityExpenses

        [HttpPost]
        [Route("AddActivityExpenses")]
        public async Task<IActionResult> AddActivityExpenses(
            [FromBody] LeadActivityExpenseRequest request)
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

                // Check whether user can manage activity on this lead
                if (!await _authorization.CanPerformAction(
                    User,
                    "Lead",
                    "Activity",
                    request.Ref_LeadId))
                {
                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        new ApiResponse
                        {
                            Status = false,
                            Message =
                                "You are not authorized to add expense on this lead."
                        });
                }

                request.CreatedBy = User.EmployeeId();

                var response =
                    await _repository.AddActivityExpenses(request);

                return response.Status
                    ? Ok(response)
                    : BadRequest(response);
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

        #region  // API : api/Lead/ActivityExpenseMasters
        [HttpGet]
        [Route("ActivityExpenseMasters")]
        public async Task<IActionResult> GetActivityExpenseMasters()
        {
            try
            {
                var response =await _repository.GetActivityExpenseMasters();

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
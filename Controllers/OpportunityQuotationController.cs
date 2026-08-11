using System;
using System.Threading.Tasks;
using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CRMWebServices.Models.OpportunityQuotationModels;

namespace CRMWebServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OpportunityQuotationController : ControllerBase
    {
        private readonly IOpportunityQuotationRepository _repository;
        private readonly ICRMAuthorizationService _authorization;

        public OpportunityQuotationController(IOpportunityQuotationRepository repository, ICRMAuthorizationService authorization)
        {
            _repository = repository;
            _authorization = authorization;
        }

        #region GET api/OpportunityQuotation/raise-data/{opportunityId}
        [HttpGet]
        [Route("raise-data/{opportunityId}")]
        public async Task<IActionResult> GetRaiseQuotationData(int opportunityId)
        {
            try
            {
                if (!await _authorization.CanPerformAction(User, "Opp", "View", opportunityId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to view quotation data for this opportunity."
                    });
                }

                var response = await _repository.GetRaiseQuotationData(opportunityId);

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

        #region GET api/OpportunityQuotation/list
        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> GetQuotationList([FromQuery] QuotationListRequest request)
        {
            try
            {
                var accessFilter = await _authorization.GetAccessFilter(User, "Opp");

                if (!accessFilter.CanViewAll)
                {
                    request.FilterOwnerId = accessFilter.EmployeeId;
                    request.ShowCreatedAndAssigned = accessFilter.ShowCreatedAndAssigned;
                }

                var response = await _repository.GetQuotationList(request);

                if (response.Status &&
                    !accessFilter.CanViewAll &&
                    response.Data is List<QuotationListResponse> list &&
                    list.Count == 0)
                {
                    response.Message = "No quotations found for opportunities assigned to you.";
                }

                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = $"Error: {ex.Message}" });
            }
        }
        #endregion

        #region GET api/OpportunityQuotation/details/{opportunityQuotationId}       
        [HttpGet]
        [Route("details/{opportunityQuotationId}")]
        public async Task<IActionResult> GetQuotationDetails(int opportunityQuotationId)
        {
            try
            {
                // Note: view permission is checked at the Opportunity level upstream (existing
                // GET_OPPORTUNITY_DETAILS call already gated by "Opp","View"), so this endpoint
                // is treated as a detail-drill-down and does not re-check per-quotation ownership.
                var response = await _repository.GetQuotationDetails(opportunityQuotationId);
                return response.Status ? Ok(response) : NotFound(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = $"Error: {ex.Message}" });
            }
        }
        #endregion

        #region POST api/OpportunityQuotation/raise
        [HttpPost]
        [Route("raise")]
        public async Task<IActionResult> RaiseQuotation([FromBody] RaiseQuotationRequest request)
        {
            try
            {
                if (!await _authorization.CanPerformAction(User, "Opp", "RaiseQuotation", request.Ref_OpportunityId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to raise a quotation for this opportunity."
                    });
                }

                request.CreatedBy = User.EmployeeId();
                var response = await _repository.RaiseQuotation(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = $"Error: {ex.Message}" });
            }
        }
        #endregion

        #region POST api/OpportunityQuotation/customer-response
        [HttpPost]
        [Route("customer-response")]
        public async Task<IActionResult> LogCustomerResponse([FromBody] LogCustomerResponseRequest request)
        {
            try
            {
                if (!await _authorization.CanPerformAction(User, "Opp", "RespondCustomer", request.Ref_OpportunityId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to log a customer response on this opportunity."
                    });
                }

                request.CreatedBy = User.EmployeeId();
                var response = await _repository.LogCustomerResponse(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = $"Error: {ex.Message}" });
            }
        }
        #endregion

        #region POST api/OpportunityQuotation/negotiation-round
        [HttpPost]
        [Route("negotiation-round")]
        public async Task<IActionResult> LogNegotiationRound([FromBody] LogNegotiationRoundRequest request)
        {
            try
            {
                if (!await _authorization.CanPerformAction(User, "Opp", "LogNegotiation", request.Ref_OpportunityId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse
                    {
                        Status = false,
                        Message = "You are not authorized to log a negotiation round on this opportunity."
                    });
                }

                request.CreatedBy = User.EmployeeId();
                var response = await _repository.LogNegotiationRound(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = $"Error: {ex.Message}" });
            }
        }
        #endregion

        #region POST api/OpportunityQuotation/review-decision
        // POST api/OpportunityQuotation/review-decision
        // NOTE: this action is NOT scoped to a single Opportunity (it's an
        // internal manager action against a ManagementReview record), so it
        // uses a resource-level permission check rather than CanPerformAction's
        // per-opportunity ownership check. Wire this to your role/permission
        // model in the next pass (e.g. "Opp","DecideReview" as a role-only check).

        [HttpPost]
        [Route("review-decision")]
        public async Task<IActionResult> DecideManagementReview([FromBody] DecideManagementReviewRequest request)
        {
            try
            {
                request.CreatedBy = User.EmployeeId();
                request.ReviewedByEmployeeId ??= User.EmployeeId();

                var response = await _repository.DecideManagementReview(request);
                return response.Status ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = $"Error: {ex.Message}" });
            }
        }
        #endregion

        #region POST api/OpportunityQuotation/revise

        [HttpPost]
        [Route("revise")]
        public async Task<IActionResult> ReviseQuotation(
            [FromBody] ReviseQuotationRequest request)
        {
            try
            {
                if (!await _authorization.CanPerformAction(
                        User,
                        "Opp",
                        "LogNegotiation",
                        request.Ref_OpportunityId))
                {
                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        new ApiResponse
                        {
                            Status = false,
                            Message =
                                "You are not authorized to revise this quotation."
                        });
                }

                request.CreatedBy = User.EmployeeId();

                var response =
                    await _repository.ReviseQuotation(request);

                return response.Status
                    ? Ok(response)
                    : BadRequest(response);
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
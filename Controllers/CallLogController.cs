using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CRMWebServices.Models.Call_Log.CallLogModels;

namespace CRMWebServices.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CallLogController : ControllerBase
    {
        private readonly ICallLogRepository _callLogRepository;
        public CallLogController(ICallLogRepository callLogRepository)
        {
            _callLogRepository = callLogRepository;
        }

        [HttpPost("Create")]
        public async Task<IActionResult> CreateCallLog([FromBody] CallLogCreateRequest model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Invalid request."
                    });
                }

                model.CreatedBy = User.EmployeeId();

                var response = await _callLogRepository.CreateCallLog(model);

                if (!response.Status)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("Lead/{leadId}")]
        public async Task<IActionResult> GetLeadCallLogs(int leadId)
        {
            try
            {
                if (leadId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Invalid Lead Id."
                    });
                }

                var response = await _callLogRepository.GetLeadCallLogs(leadId);

                if (!response.Status)
                {
                    return NotFound(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("Opportunity/{opportunityId}")]
        public async Task<IActionResult> GetOpportunityCallLogs(int opportunityId)
        {
            try
            {
                if (opportunityId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Invalid Opportunity Id."
                    });
                }

                var response = await _callLogRepository.GetOpportunityCallLogs(opportunityId);

                if (!response.Status)
                {
                    return NotFound(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                });
            }
        }
    }
}
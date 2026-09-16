using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CRMWebServices.Models.TicketModels;

namespace CRMWebServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TicketController : ControllerBase
    {
        private readonly ITicketRepository _repository;

        public TicketController(ITicketRepository repository)
        {
            _repository = repository;
        }

        #region API : api/ticket/create
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
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

                var employeeId = User.EmployeeId();

                var response = await _repository.CreateTicket(request, employeeId);

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

        #region API : api/ticket/my-tickets
        [HttpGet]
        [Route("my-tickets")]
        public async Task<IActionResult> GetMyTickets()
        {
            try
            {
                int employeeId = User.EmployeeId();

                var response = await _repository.GetMyTickets(employeeId);

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

        #region API : api/ticket/detail/{ticketId}
        [HttpGet]
        [Route("detail/{ticketId}")]
        public async Task<IActionResult> GetTicketDetail(int ticketId)
        {
            try
            {
                var response = await _repository.GetTicketDetail(ticketId);

                return response.Status
                    ? Ok(response)
                    : NotFound(response);
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

        #region API : api/ticket/UpdateStatus
        [HttpPost]
        [Route("UpdateStatus")]
        public async Task<IActionResult> UpdateTicketStatus([FromBody] UpdateTicketStatusRequest request)
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

                request.EmployeeId = User.EmployeeId();

                var response = await _repository.UpdateTicketStatus(request);

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

        #region API : api/ticket/UpdateActivity
        [HttpPost]
        [Route("UpdateActivity")]
        public async Task<IActionResult> UpdateActivity([FromBody] UpdateTicketActivityRequest request)
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

                request.EmployeeId = User.EmployeeId();

                var response = await _repository.UpdateActivity(request);

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

        #region API : api/ticket/activities/{ticketId}
        [HttpGet("statuses")]
        public async Task<IActionResult> GetTicketStatuses()
        {
            try
            {
                var response = await _repository.GetTicketStatuses();
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
    }
}
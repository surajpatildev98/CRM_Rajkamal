using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CRMWebServices.Models.TicketSupportModels;

namespace CRMWebServices.Controllers
{
    /// <summary>
    /// Ticket Support module (ModuleCode Ticket_support).
    /// Support Manager = ORG_Role 9; only the Support Manager of the assigned
    /// employee approves Visit tickets, travel and expenses (checked in the SP).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TicketSupportController : ControllerBase
    {
        private const string SupportManagerRoleId = "9";
        private readonly ITicketSupportRepository _repository;

        public TicketSupportController(ITicketSupportRepository repository)
        {
            _repository = repository;
        }

        private int EmployeeId => User.EmployeeId();

        /// <summary>RoleIds claim ("9" or "4,9").</summary>
        private bool IsSupportManager =>
            (User.FindFirst("RoleIds")?.Value ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Any(r => r.Trim() == SupportManagerRoleId);

        private IActionResult Result(ApiResponse response) => response.Status ? Ok(response) : BadRequest(response);

        /// <summary>Raise a ticket. Support Manager may set assignedTo (mapped employee).</summary>
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateSupportTicketRequest model) =>
            Result(await _repository.CreateTicket(model, EmployeeId, IsSupportManager));

        /// <summary>listType: ASSIGNED | RAISED | TEAM_OPEN | TEAM_CLOSED</summary>
        [HttpGet("list")]
        public async Task<IActionResult> List(string listType = "ASSIGNED") =>
            Result(await _repository.GetTickets(listType, EmployeeId, IsSupportManager));

        /// <summary>Open / awaiting approval / completed counts. listType: ASSIGNED | RAISED</summary>
        [HttpGet("counts")]
        public async Task<IActionResult> Counts(string listType = "ASSIGNED") =>
            Result(await _repository.GetMyCounts(listType, EmployeeId, IsSupportManager));

        [HttpGet("details/{ticketId:int}")]
        public async Task<IActionResult> Details(int ticketId) =>
            Result(await _repository.GetTicketDetails(ticketId, EmployeeId, IsSupportManager));

        /// <summary>Assignee sends travel mode(s) for a Visit; Personal Car / Stay / Other go for approval.</summary>
        [HttpPost("send-for-approval")]
        public async Task<IActionResult> SendForApproval([FromBody] TicketTravelRequest model) =>
            Result(await _repository.SendForApproval(model, EmployeeId, IsSupportManager));

        /// <summary>Support Manager approves/rejects the ticket (all pending items) or one expense.</summary>
        [HttpPost("decide")]
        public async Task<IActionResult> Decide([FromBody] TicketDecisionRequest model) =>
            Result(await _repository.Decide(model, EmployeeId, IsSupportManager));

        [HttpPost("check-in")]
        public async Task<IActionResult> CheckIn([FromBody] TicketCheckInRequest model) =>
            Result(await _repository.CheckIn(model, EmployeeId, IsSupportManager));

        [HttpPost("check-out")]
        public async Task<IActionResult> CheckOut([FromBody] TicketCheckInRequest model) =>
            Result(await _repository.CheckOut(model, EmployeeId, IsSupportManager));

        /// <summary>Assignee marks a PLANNED activity as completed.</summary>
        [HttpPost("activity-complete")]
        public async Task<IActionResult> CompleteActivity([FromBody] TicketCompleteActivityRequest model) =>
            Result(await _repository.CompleteActivity(model, EmployeeId, IsSupportManager));

        /// <summary>Raiser approves / rejects the assignee's close request.</summary>
        [HttpPost("close-decision")]
        public async Task<IActionResult> CloseDecision([FromBody] TicketCloseDecisionRequest model) =>
            Result(await _repository.DecideClose(model, EmployeeId, IsSupportManager));

        /// <summary>Raiser (Support Manager) reassigns a ticket that has a close request.</summary>
        [HttpPost("reassign")]
        public async Task<IActionResult> Reassign([FromBody] TicketReassignRequest model) =>
            Result(await _repository.Reassign(model, EmployeeId, IsSupportManager));

        [HttpPost("activity")]
        public async Task<IActionResult> AddActivity([FromBody] TicketActivityRequest model) =>
            Result(await _repository.AddActivity(model, EmployeeId, IsSupportManager));

        [HttpPost("expense")]
        public async Task<IActionResult> AddExpense([FromBody] TicketExpenseRequest model) =>
            Result(await _repository.AddExpenses(model, EmployeeId, IsSupportManager));

        [HttpPost("complete")]
        public async Task<IActionResult> Complete([FromBody] TicketCompleteRequest model) =>
            Result(await _repository.CompleteTicket(model, EmployeeId, IsSupportManager));

        [HttpPost("attachments")]
        public async Task<IActionResult> Attachments([FromBody] TicketAttachmentRequest model) =>
            Result(await _repository.AddAttachments(model, EmployeeId, IsSupportManager));

        /// <summary>Assignee chooses Online / Visit before work starts.</summary>
        [HttpPost("service-mode")]
        public async Task<IActionResult> ServiceMode([FromBody] TicketServiceModeRequest model) =>
            Result(await _repository.SetServiceMode(model, EmployeeId, IsSupportManager));

        [HttpPost("cancel")]
        public async Task<IActionResult> Cancel([FromBody] TicketCancelRequest model) =>
            Result(await _repository.CancelTicket(model, EmployeeId, IsSupportManager));

        [HttpGet("manager-dashboard")]
        public async Task<IActionResult> ManagerDashboard() =>
            Result(await _repository.GetManagerDashboard(EmployeeId, IsSupportManager));

        [HttpGet("team")]
        public async Task<IActionResult> Team() =>
            Result(await _repository.GetTeam(EmployeeId, IsSupportManager));

        [HttpGet("approvals")]
        public async Task<IActionResult> Approvals() =>
            Result(await _repository.GetPendingApprovals(EmployeeId, IsSupportManager));
    }
}

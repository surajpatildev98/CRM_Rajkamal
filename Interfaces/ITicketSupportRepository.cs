using CRMWebServices.Helpers;
using static CRMWebServices.Models.TicketSupportModels;

namespace CRMWebServices.Interfaces
{
    /// <summary>
    /// Ticket Support module. <paramref name="employeeId"/> is always the acting employee from the JWT;
    /// <paramref name="isManager"/> = acting employee has ORG_Role 9 (Support Manager).
    /// </summary>
    public interface ITicketSupportRepository
    {
        Task<ApiResponse> CreateTicket(CreateSupportTicketRequest model, int employeeId, bool isManager);
        Task<ApiResponse> GetTickets(string listType, int employeeId, bool isManager);
        Task<ApiResponse> GetMyCounts(string listType, int employeeId, bool isManager);
        Task<ApiResponse> GetTicketDetails(int ticketId, int employeeId, bool isManager);
        Task<ApiResponse> SendForApproval(TicketTravelRequest model, int employeeId, bool isManager);
        Task<ApiResponse> Decide(TicketDecisionRequest model, int employeeId, bool isManager);
        Task<ApiResponse> CheckIn(TicketCheckInRequest model, int employeeId, bool isManager);
        Task<ApiResponse> CheckOut(TicketCheckInRequest model, int employeeId, bool isManager);
        Task<ApiResponse> CompleteActivity(TicketCompleteActivityRequest model, int employeeId, bool isManager);
        Task<ApiResponse> DecideClose(TicketCloseDecisionRequest model, int employeeId, bool isManager);
        Task<ApiResponse> Reassign(TicketReassignRequest model, int employeeId, bool isManager);
        Task<ApiResponse> AddActivity(TicketActivityRequest model, int employeeId, bool isManager);
        Task<ApiResponse> AddExpenses(TicketExpenseRequest model, int employeeId, bool isManager);
        Task<ApiResponse> CompleteTicket(TicketCompleteRequest model, int employeeId, bool isManager);
        Task<ApiResponse> AddAttachments(TicketAttachmentRequest model, int employeeId, bool isManager);
        Task<ApiResponse> SetServiceMode(TicketServiceModeRequest model, int employeeId, bool isManager);
        Task<ApiResponse> CancelTicket(TicketCancelRequest model, int employeeId, bool isManager);
        Task<ApiResponse> GetManagerDashboard(int employeeId, bool isManager);
        Task<ApiResponse> GetTeam(int employeeId, bool isManager);
        Task<ApiResponse> GetPendingApprovals(int employeeId, bool isManager);
    }
}

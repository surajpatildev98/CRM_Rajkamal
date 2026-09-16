using CRMWebServices.Helpers;
using static CRMWebServices.Models.TicketModels;

namespace CRMWebServices.Interfaces
{
    public interface ITicketRepository
    {
        Task<ApiResponse> GetMyTickets(int employeeId);
        Task<ApiResponse> GetTicketDetail(int ticketId);
        Task<ApiResponse> CreateTicket(CreateTicketRequest model, long createdBy);
        Task<ApiResponse> UpdateTicketStatus(UpdateTicketStatusRequest model);
        Task<ApiResponse> UpdateActivity(UpdateTicketActivityRequest model);
        Task<ApiResponse> GetTicketStatuses();
    }
}

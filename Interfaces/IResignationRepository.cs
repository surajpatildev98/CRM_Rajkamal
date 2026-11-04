using ResignationApi.Models;

namespace CRMWebServices.Interfaces
{
    public interface IResignationRepository
    {
        Task<List<ReasonDto>> GetReasonsAsync();
        Task<NoticePolicyDto> GetNoticePolicyAsync(int employeeId);
        Task<(int? ManagerId, int? HrManagerId)> GetApproversAsync(int employeeId);

        Task<ResignationDto?> GetMyAsync(int employeeId);
        Task<ResignationDto> ApplyAsync(int employeeId, ApplyResignationRequest req, int noticeDays, int? managerId, int? hrManagerId);
        Task WithdrawAsync(int employeeId, WithdrawRequest req);

        Task<List<ResignationDto>> GetTeamAsync(int employeeId, bool isHr);
        Task<ResignationDto?> GetDetailsAsync(int employeeId, int resignationId, bool isHr);
        Task ApproveAsync(int employeeId, ApproveRequest req);
        Task RejectAsync(int employeeId, RejectRequest req);
        Task<ResignationDto> UpdateClearanceAsync(int employeeId, bool isHr, UpdateClearanceRequest req);

    }
}

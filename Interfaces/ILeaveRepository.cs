using CRMWebServices.Models;

namespace CRMWebServices.Interfaces
{
    public interface ILeaveRepository
    {
       
            Task<LeaveResponse<List<LeaveBalanceModel>>> GetLeaveBalance(int employeeId);
            Task<LeaveResponse<List<LeaveEntryModel>>> GetMyLeaves(int employeeId);
            Task<LeaveResponse<List<ApplyLeaveTypeModel>>> GetApplyLeaveTypes(int employeeId);
            Task<LeaveResponse<object>> ApplyLeave(ApplyLeaveRequest model);
            Task<LeaveResponse<object>> CancelLeave(CancelLeaveRequest model);
            Task<LeaveResponse<List<TeamLeaveModel>>> GetTeamLeaves(int employeeId);
            Task<LeaveResponse<object>> ApproveLeave(ApproveLeaveRequest model);
            Task<LeaveResponse<object>> RejectLeave(RejectLeaveRequest model);


    }
}

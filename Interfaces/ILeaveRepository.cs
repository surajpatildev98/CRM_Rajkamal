using CRMWebServices.Models;

namespace CRMWebServices.Interfaces
{
    public interface ILeaveRepository
    {
       
            Task<LeaveResponse<List<LeaveBalanceModel>>> GetLeaveBalance(int employeeId);
            Task<LeaveResponse<List<LeaveEntryModel>>> GetMyLeaves(int employeeId);
            Task<LeaveResponse<List<ApplyLeaveTypeModel>>> GetApplyLeaveTypes(int employeeId);
            Task<LeaveResponse<object>> ApplyLeave(ApplyLeaveRequest model);

    }
}

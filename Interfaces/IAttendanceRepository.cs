using CRMWebServices.Models.Attendance;

namespace CRMWebServices.Interfaces
{
    public interface IAttendanceRepository
    {
        Task<AttendanceResponse> GetAttendance(AttendanceRequest request);
        Task<AttendancePunchResponse> GetPunches(AttendancePunchRequest request);
        Task<ManualPunchResponse> ManualPunch(ManualPunchRequest request);
    }
}

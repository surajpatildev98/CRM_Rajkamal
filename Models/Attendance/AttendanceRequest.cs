namespace CRMWebServices.Models.Attendance
{
    public class AttendanceRequest
    {
        public DateTime? AttendanceDate { get; set; }

        public int? OfficeId { get; set; }

        public int? DepartmentId { get; set; }

        public string? Search { get; set; }
    }
}

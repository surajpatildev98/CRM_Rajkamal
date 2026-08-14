namespace CRMWebServices.Models.Attendance
{
    public class AttendancePunchModel
    {
        public int AttendancePunchId { get; set; }

        public int AttendanceId { get; set; }

        public int EmployeeId { get; set; }

        public DateTime PunchDateTime { get; set; }

        public string? PunchType { get; set; }

        public string? PunchSource { get; set; }

        public string? WorkMode { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public string? Remarks { get; set; }

        public DateTime? CreatedOn { get; set; }

        public int? CreatedBy { get; set; }
    }
}

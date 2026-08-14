namespace CRMWebServices.Models.Attendance
{
    public class ManualPunchRequest
    {
        public int EmployeeId { get; set; }

        public string PunchType { get; set; } = string.Empty;

        public string WorkMode { get; set; } = string.Empty;

        public decimal Latitude { get; set; }

        public decimal Longitude { get; set; }

        public string? Remarks { get; set; }

        public int CreatedBy { get; set; }
    }
}

namespace CRMWebServices.Models.Attendance
{
    public class ManualPunchResponse
    {
        public bool Status { get; set; }

        public string Message { get; set; } = string.Empty;

        public int? AttendanceId { get; set; }

        public string? PunchType { get; set; }

        public string? WorkMode { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public decimal? DistanceKm { get; set; }
    }
}

namespace CRMWebServices.Models.Attendance
{
    public class AttendanceResponse
    {
        public bool Status { get; set; }

        public string Message { get; set; } = string.Empty;

        public List<AttendanceModel> Data { get; set; }
            = new List<AttendanceModel>();
    }
}

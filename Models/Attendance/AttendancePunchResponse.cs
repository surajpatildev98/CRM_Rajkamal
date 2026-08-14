namespace CRMWebServices.Models.Attendance
{
    public class AttendancePunchResponse
    {
        public bool Status { get; set; }

        public string Message { get; set; } = string.Empty;

        public List<AttendancePunchModel> Data { get; set; }
            = new List<AttendancePunchModel>();
    }
}

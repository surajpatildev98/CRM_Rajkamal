namespace CRMWebServices.Models.Shift
{
    public class ShiftResponse
    {
        public bool Status { get; set; }

        public string Message { get; set; } = string.Empty;

        public List<ShiftModel> Data { get; set; } = new();
    }

     public class ShiftModel
    {
        public int ShiftId { get; set; }

        public string ShiftCode { get; set; } = "";

        public string ShiftName { get; set; } = "";

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public TimeSpan BreakStartTime { get; set; }

        public TimeSpan BreakEndTime { get; set; }

        public int GraceInMinutes { get; set; }

        public int GraceOutMinutes { get; set; }

        public decimal MinimumWorkingHours { get; set; }

        public bool IsNightShift { get; set; }

        public bool IsActive { get; set; }

        public int? CreatedBy { get; set; }
    }
}

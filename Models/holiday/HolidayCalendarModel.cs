namespace CRMWebServices.Models.holiday
{
    public class HolidayCalendarModel
    {
        public int HolidayCalendarId { get; set; }

        public string CalendarName { get; set; } = string.Empty;

        public int CalendarYear { get; set; }

        public int CompanyId { get; set; }

        public int? OfficeId { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreatedOn { get; set; }

        public int CreatedBy { get; set; }
    }

    public class HolidayCalendarResponse
    {
        public bool Status { get; set; }

        public string Message { get; set; } = string.Empty;

        public List<HolidayCalendarModel> Data { get; set; }
            = new List<HolidayCalendarModel>();
    }

    public class HolidayCalendarSaveRequest
    {
        public int? HolidayCalendarId { get; set; }
        public string CalendarName { get; set; }

        public int CalendarYear { get; set; }

        public int CompanyId { get; set; }

        public int? OfficeId { get; set; }

        public bool IsActive { get; set; }

        public int CreatedBy { get; set; }

        public List<HolidaySaveRequest> Holidays { get; set; }
            = new List<HolidaySaveRequest>();
    }

    public class HolidayModel
    {
        public int HolidayId { get; set; }

        public int Ref_HolidayCalendarId { get; set; }

        public string HolidayName { get; set; } = string.Empty;

        public DateTime HolidayDate { get; set; }

        public bool IsOptional { get; set; }

        public string? Remarks { get; set; }

        public DateTime? CreatedOn { get; set; }

        public int CreatedBy { get; set; }
    }

      public class HolidayResponse
    {
        public bool Status { get; set; }

        public string Message { get; set; } = string.Empty;

        public List<HolidayModel> Data { get; set; } = new();
    }

    public class HolidaySaveRequest
    {
        public int? HolidayId { get; set; }
        public string HolidayName { get; set; }

        public DateTime HolidayDate { get; set; }

        public bool IsOptional { get; set; }

        public string? Remarks { get; set; }
    }
}

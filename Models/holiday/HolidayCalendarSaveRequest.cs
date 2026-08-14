namespace CRMWebServices.Models.holiday
{
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
}

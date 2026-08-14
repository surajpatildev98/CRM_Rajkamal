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
}

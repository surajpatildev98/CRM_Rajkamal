namespace CRMWebServices.Models.holiday
{
    public class HolidayCalendarResponse
    {
        public bool Status { get; set; }

        public string Message { get; set; } = string.Empty;

        public List<HolidayCalendarModel> Data { get; set; }
            = new List<HolidayCalendarModel>();
    }
}

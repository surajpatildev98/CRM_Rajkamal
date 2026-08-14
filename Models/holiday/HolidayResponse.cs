namespace CRMWebServices.Models.holiday
{
    public class HolidayResponse
    {
        public bool Status { get; set; }

        public string Message { get; set; } = string.Empty;

        public List<HolidayModel> Data { get; set; } = new();
    }
}

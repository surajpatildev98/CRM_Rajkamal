namespace CRMWebServices.Models.holiday
{
    public class HolidaySaveRequest
    {
        public int? HolidayId { get; set; }
        public string HolidayName { get; set; }

        public DateTime HolidayDate { get; set; }

        public bool IsOptional { get; set; }

        public string? Remarks { get; set; }
    }
}

namespace CRMWebServices.Models.holiday
{
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
}

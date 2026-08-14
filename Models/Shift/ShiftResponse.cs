namespace CRMWebServices.Models.Shift
{
    public class ShiftResponse
    {
        public bool Status { get; set; }

        public string Message { get; set; } = string.Empty;

        public List<ShiftModel> Data { get; set; } = new();
    }
}

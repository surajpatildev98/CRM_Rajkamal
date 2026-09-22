namespace CRMWebServices.Models
{
    public class LoginRequest
    {
        public string LoginId { get; set; }
        public string Password { get; set; }
        public string? DeviceId { get; set; }
        public string? DeviceToken { get; set; }
        public string? DeviceType { get; set; }
        public string? AppVersion { get; set; }
    }

}

namespace CRMWebServices.Models
{
    public class LogoutModels
    {
        public class ForceLogoutRequest
        {
            public int EmployeeId { get; set; }
            public string DeviceId { get; set; }
        }

        public class LogoutRequest
        {
            public int EmployeeId { get; set; }
            public string DeviceId { get; set; }
        }

        public class ActiveDeviceInfo
        {
            public int EmployeeDeviceId { get; set; }
            public int Ref_EmployeeId { get; set; }

            public string? ActiveDeviceId { get; set; }
            public string? ActiveDeviceToken { get; set; }

            public string? DeviceType { get; set; }
            public string? AppVersion { get; set; }

            public bool IsActive { get; set; }

            public DateTime? LastSeenOn { get; set; }

            public bool IsSameDevice { get; set; }
        }
    }
}

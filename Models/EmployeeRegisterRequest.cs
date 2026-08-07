namespace CRMWebServices.Models
{
    public class EmployeeRegisterRequest
    {
        public string FirstName { get; set; }

        public string? MiddleName { get; set; }

        public string? LastName { get; set; }

        public string OfficialEmail { get; set; }

        public string? PersonalEmail { get; set; }

        public string Mobile { get; set; }

        public string? AlternateMobile { get; set; }

        public string? Gender { get; set; }

        public DateTime? DOB { get; set; }

        public DateTime? DOJ { get; set; }


        public string Password { get; set; }

        public int? RefDesignationId { get; set; }

        public int? ReportingManagerId { get; set; }

        public int RefOfficeId { get; set; }

        public int RefDepartmentId { get; set; }

        public int RefRoleId { get; set; }

        public int? CreatedBy { get; set; }
    }
}

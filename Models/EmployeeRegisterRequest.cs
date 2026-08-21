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
        public List<EmployeeEducationRequest>? EducationDetails { get; set; }

        public List<EmployeeBankRequest>? BankDetails { get; set; }

        public List<EmployeeFamilyRequest>? FamilyDetails { get; set; }

        public List<EmployeeDocumentRequest>? Documents { get; set; }
    }

    public class EmployeeEducationRequest
    {
        public int QualificationId { get; set; }
        public string? InstituteName { get; set; }
        public string? UniversityName { get; set; }
        public int? PassingYear { get; set; }
        public decimal? Percentage { get; set; }
        public string? Grade { get; set; }
    }
    public class EmployeeBankRequest
    {
        public int BankId { get; set; }
        public int AccountTypeId { get; set; }
        public string? AccountHolderName { get; set; }
        public string? AccountNumber { get; set; }
        public string? IFSCCode { get; set; }
        public string? BranchName { get; set; }
    }

    public class EmployeeFamilyRequest
    {
        public int RelationshipId { get; set; }
        public string? FamilyMemberName { get; set; }
        public string? Occupation { get; set; }
        public string? ContactNumber { get; set; }
        public bool IsDependent { get; set; }
    }

    public class EmployeeDocumentRequest
    {
        public int DocumentTypeId { get; set; }
        public string? DocumentName { get; set; }
        public string? DocumentPath { get; set; }
    }


}

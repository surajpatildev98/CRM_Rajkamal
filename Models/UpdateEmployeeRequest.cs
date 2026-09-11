namespace CRMWebServices.Models
{
    public class UpdateEmployeeRequest
    {
        public int EmployeeId { get; set; }

        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }

        public string? OfficialEmail { get; set; }
        public string? PersonalEmail { get; set; }

        public string? Mobile { get; set; }
        public string? AlternateMobile { get; set; }

        public string? Gender { get; set; }
        public DateTime? DOB { get; set; }
        public DateTime? DOJ { get; set; }
        public string? MaritalStatus { get; set; }
        public string? BloodGroup { get; set; }

        public int? RefDesignationId { get; set; }
        public int? ReportingManagerId { get; set; }

        public int? RefOfficeId { get; set; }
        public int? RefDepartmentId { get; set; }
        public int? RefRoleId { get; set; }
        public int? ShiftId { get; set; }
        public int? RefLeavePolicyId { get; set; }

        public int? UpdatedBy { get; set; }

        // Multiple Education records
        public List<EducationModel>? Education { get; set; }

        // Bank details
        public List<BankModel>? Bank { get; set; }

        // Multiple Family records
        public List<FamilyModel>? FamilyDetails { get; set; }

        // Multiple Document records
        public List<DocumentModel>? Document { get; set; }
    }

    public class EducationModel
    {
        public int? EducationId { get; set; }
        public int QualificationId { get; set; }

        public string? UniversityName { get; set; }

        public string? InstituteName { get; set; }

        public int? PassingYear { get; set; }

        public decimal? Percentage { get; set; }

        public string? Grade { get; set; }

        public string? Specialization { get; set; }

        public string? CertificatePath { get; set; }
    }

    public class BankModel
    {
        public int BankId { get; set; }

        public int AccountTypeId { get; set; }

        public string? AccountHolderName { get; set; }

        public string? AccountNumber { get; set; }

        public string? IFSCCode { get; set; }

        public bool? IsPrimary { get; set; }
    }

    public class FamilyModel
    {
        public int RelationshipId { get; set; }

        public string? FamilyMemberName { get; set; }

        public string? Occupation { get; set; }

        public string? ContactNumber { get; set; }

        public bool? IsDependent { get; set; }

        public DateTime? DOB { get; set; }

        public bool? IsEmergencyContact { get; set; }
    }

    public class DocumentModel
    {
        public int? DocumentId { get; set; }
        public int DocumentTypeId { get; set; }

        public string? DocumentName { get; set; }

        public string? DocumentPath { get; set; }
        public string? DocumentNumber { get; set; }
        public DateTime? IssueDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool? Verified { get; set; }

        public int? VerifiedBy { get; set; }

        public DateTime? VerifiedOn { get; set; }

        public string? Remarks { get; set; }
        public string? Base64Content { get; set; }
    }
}

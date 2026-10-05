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

        public bool ProbationRequired { get; set; } = true;
        public int? ProbationPeriodValue { get; set; }
        public string? ProbationPeriodUnit { get; set; }

        public string? MaritalStatus { get; set; }
        public string? BloodGroup { get; set; }
        public string Password { get; set; }
        public int? RefDesignationId { get; set; }
        public int? ReportingManagerId { get; set; }
        public int? ReportingHRManagerId { get; set; }
        public int RefOfficeId { get; set; }
        public int RefDepartmentId { get; set; }
        public int RefRoleId { get; set; }      
        public int ShiftId { get; set; }
        public int? RefLeavePolicyId { get; set; }
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
        public string? Specialization { get; set; }
        public string? CertificatePath { get; set; }
    }
    public class EmployeeBankRequest
    {
        public int BankId { get; set; }
        public int AccountTypeId { get; set; }
        public string? AccountHolderName { get; set; }
        public string? AccountNumber { get; set; }
        public string? IFSCCode { get; set; }
        public string? BranchName { get; set; }
        public bool? IsPrimary { get; set; }
    }

    public class EmployeeFamilyRequest
    {
        public int RelationshipId { get; set; }
        public string? FamilyMemberName { get; set; }
        public string? Occupation { get; set; }
        public string? ContactNumber { get; set; }
        public bool? IsDependent { get; set; }
        public bool? IsEmergencyContact { get; set; }
    }

    public class EmployeeDocumentRequest
    {
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
        public string Base64Content { get; set; }
    }

    public class EmployeeMasterModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string OfficialEmail { get; set; } = string.Empty;
        public string? PersonalEmail { get; set; }
        public string Mobile { get; set; } = string.Empty;
        public string? AlternateMobile { get; set; }
        public string? Gender { get; set; }
        public DateTime? DOB { get; set; }
        public DateTime? DOJ { get; set; }
        public string? MaritalStatus { get; set; }
        public string? BloodGroup { get; set; }
        public string? LoginId { get; set; }
        public bool IsActive { get; set; }
        public int? RefOfficeId { get; set; }
        public string? OfficeName { get; set; }
        public int? RefDepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int? RefRoleId { get; set; }
        public string? RoleName { get; set; }
        public int? RefDesignationId { get; set; }
        public string? DesignationName { get; set; }
        public int? RefShiftId { get; set; }
        public string? ShiftName { get; set; }
        public int? RefLeavePolicyId { get; set; }
        public string? LeavePolicyName { get; set; }
        public int? RefReportingHRManagerId { get; set; }
        public string? ReportingManagerID { get; set; }
    }

    public class EmployeeDetailsModel
    {
        public EmployeeBasicDetailsModel Employee { get; set; } = new();
        public List<EmployeeEducationModel> Education { get; set; } = new();
        public List<EmployeeBankModel> BankDetails { get; set; } = new();
        public List<EmployeeFamilyModel> FamilyDetails { get; set; } = new();
        public List<EmployeeDocumentModel> Documents { get; set; } = new();
    }

    public class EmployeeBasicDetailsModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
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

        // PROBATION
        public bool ProbationRequired { get; set; }
        public int? RefProbationStatusId { get; set; }
        public int? EmployeeProbationId { get; set; }
        public int? ProbationPeriodValue { get; set; }
        public string? ProbationPeriodUnit { get; set; }
        public DateTime? ProbationStartDate { get; set; }
        public DateTime? ProbationEndDate { get; set; }
        public string? ProbationStatusName { get; set; }
        public DateTime? ConfirmationDate { get; set; }
        public int? ConfirmedBy { get; set; }
        public string? ConfirmationRemark { get; set; }

        public string? LoginId { get; set; }
        public int? RefDesignationId { get; set; }
        public string? DesignationName { get; set; }
        public int? RefReportingManagerId { get; set; }
        public string? ReportingManagerCode { get; set; }
        public string? ReportingManagerName { get; set; }
        public int? RefReportingHRManagerId { get; set; }
        public string? ReportingHRManagerCode { get; set; }
        public string? ReportingHRManagerName { get; set; }
        public int? RefCompanyId { get; set; }
        public string? CompanyName { get; set; }
        public int? RefRegionId { get; set; }
        public string? RegionName { get; set; }    
        public int? RefStateId { get; set; }
        public string? StateName { get; set; }
        public int? RefOfficeId { get; set; }
        public string? OfficeName { get; set; }
        public int? RefDepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int? RefRoleId { get; set; }
        public string? RoleName { get; set; }
        public int? RefShiftId { get; set; }
        public string? ShiftName { get; set; }
        public int? RefLeavePolicyId { get; set; }
        public string? LeavePolicyName { get; set; }
        public int? RefReportingHRManagerId { get; set; }
        public string? ReportingHRManagerCode { get; set; }
        public string? ReportingHRManagerName { get; set; }
    }

    public class EmployeeEducationModel
    {
        public int ? EducationId { get; set; }
        public int QualificationId { get; set; }
        public string? QualificationName { get; set; }
        public string? UniversityName { get; set; }
        public string? InstituteName { get; set; }
        public int? PassingYear { get; set; }
        public decimal? Percentage { get; set; }
        public string? Grade { get; set; }
        public string? Specialization { get; set; }
        public string? CertificatePath { get; set; }
    }

    public class EmployeeBankModel
    {
        public int BankId { get; set; }
        public string? BankName { get; set; }
        public int AccountTypeId { get; set; }
        public string? AccountTypeName { get; set; }
        public string? AccountHolderName { get; set; }
        public string? AccountNumber { get; set; }
        public string? IFSCCode { get; set; }
    }

    public class EmployeeFamilyModel
    {
        public int RelationshipId { get; set; }
        public string? RelationshipName { get; set; }
        public string? FamilyMemberName { get; set; }
        public string? Occupation { get; set; }
        public string? ContactNumber { get; set; }
        public bool? IsDependent { get; set; }
        public DateTime? DOB { get; set; }
        public bool? IsEmergencyContact { get; set; }
    }

    public class EmployeeDocumentModel
    {
        public int DocumentTypeId { get; set; }
        public string? DocumentTypeName { get; set; }
        public bool IsMandatory { get; set; }
        public string? DocumentName { get; set; }
        public string? DocumentPath { get; set; }
        public string? DocumentNumber { get; set; }
        public string? FieldKind { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool? Verified { get; set; }
        public int? VerifiedBy { get; set; }
        public DateTime? VerifiedOn { get; set; }
        public string? Remarks { get; set; }
    }


}

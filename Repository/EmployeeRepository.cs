using BCrypt.Net;
using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;

namespace CRMWebServices.Repository
{
    public class EmployeeRepository
    {
        private readonly IDatabaseService _database;
        private readonly ILogger _logger;

        public EmployeeRepository(IDatabaseService database, ILogger<EmployeeRepository> logger)
        {
            _database = database;
            _logger = logger;
          
        }

        #region Attachment Handling 
        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
        private string SaveBase64Attachment(string base64, string originalFileName, string folderName)
        {
            if (string.IsNullOrWhiteSpace(base64) || string.IsNullOrWhiteSpace(originalFileName))
                return null;

            // Remove data URI prefix if Android sends it
            if (base64.Contains(","))base64 = base64.Substring(base64.IndexOf(",") + 1);

            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
                throw new Exception("Unsupported file type.");

            byte[] fileBytes;

            try
            {
                fileBytes = Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                throw new Exception("Attachment data is not valid Base64.");
            }

            if (fileBytes.Length > MaxFileSizeBytes)
                throw new Exception("File exceeds 10MB limit.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(),"Uploads",folderName);

            Directory.CreateDirectory(uploadsFolder);

            // Remove invalid characters from filename
            var fileName = Path.GetFileNameWithoutExtension(originalFileName);
            fileName = string.Concat(fileName.Split(Path.GetInvalidFileNameChars()));

            // Generate unique filename
            var uniqueFileName = $"{fileName}_{DateTime.Now:yyyyMMddHHmmssfff}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            File.WriteAllBytes(filePath, fileBytes);
            return Path.Combine("Uploads", folderName, uniqueFileName).Replace("\\", "/");
        }
        private string? BuildFileUrl(string? filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return null;

                var relative = filePath.Replace("Uploads/", "").Replace("Uploads\\", "");
                var segments = relative.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString);

                return "/Uploads/" + string.Join("/", segments);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while building document URL.", ex);
            }
        }
        #endregion

        #region Employee Registration
        public async Task<ApiResponse> Register(EmployeeRegisterRequest model)
        {
            try
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

                var dt = await _database.ExecuteDataTableAsync("USP_ORG_EmployeeManagement",
                    new SqlParameter("@Status", "REGISTER"),
                    new SqlParameter("@FirstName", model.FirstName),
                    new SqlParameter("@MiddleName", (object?)model.MiddleName ?? DBNull.Value),
                    new SqlParameter("@LastName", (object?)model.LastName ?? DBNull.Value),
                    new SqlParameter("@OfficialEmail", model.OfficialEmail),
                    new SqlParameter("@PersonalEmail", (object?)model.PersonalEmail ?? DBNull.Value),
                    new SqlParameter("@Mobile", model.Mobile),
                    new SqlParameter("@AlternateMobile", (object?)model.AlternateMobile ?? DBNull.Value),
                    new SqlParameter("@Gender", (object?)model.Gender ?? DBNull.Value),
                    new SqlParameter("@DOB", (object?)model.DOB ?? DBNull.Value),
                    new SqlParameter("@DOJ", (object?)model.DOJ ?? DBNull.Value),                  
                    new SqlParameter("@Password", passwordHash),
                    new SqlParameter("@RefDesignationId", (object?)model.RefDesignationId ?? DBNull.Value),
                    new SqlParameter("@ReportingManagerId", (object?)model.ReportingManagerId ?? DBNull.Value),
                    new SqlParameter("@ReportingHRManagerId", (object?)model.ReportingHRManagerId ?? DBNull.Value),
                    new SqlParameter("@RefOfficeId", model.RefOfficeId),
                    new SqlParameter("@RefDepartmentId", model.RefDepartmentId),
                    new SqlParameter("@RefRoleId", model.RefRoleId),
                    new SqlParameter("@CreatedBy", (object?)model.CreatedBy ?? DBNull.Value)
                );

                var response = new ApiResponse();

                if (dt == null || dt.Rows.Count == 0)
                {
                    response.Status = false;
                    response.Message = "No response returned from database.";
                    return response;
                }

                var row = dt.Rows[0];
                bool status = Convert.ToInt32(row["Status"]) == 1;

                response.Status = status;
                response.Message = row["Message"]?.ToString();
                response.Data = status? new { EmployeeId = Convert.ToInt32(row["EmployeeId"]), EmployeeCode = row["EmployeeCode"]?.ToString(), LoginId = row["LoginId"]?.ToString() }: null;

                return response;
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region New Employee Registration with Child Collections [Pratiksha]
        public async Task<ApiResponse> NewRegister(EmployeeRegisterRequest model)
        {
            try
            {
                // Serialize Child Collections
                string educationJson = model.EducationDetails != null && model.EducationDetails.Count > 0 ? JsonConvert.SerializeObject(model.EducationDetails): null;
                string bankJson = model.BankDetails != null && model.BankDetails.Count > 0 ? JsonConvert.SerializeObject(model.BankDetails): null;
                string familyJson = model.FamilyDetails != null && model.FamilyDetails.Count > 0 ? JsonConvert.SerializeObject(model.FamilyDetails): null;              

                // Process EMPLOYEE DOCUMENTS - save files, build metadata list
                List<object> savedDocuments = new List<object>();

                if (model.Documents != null && model.Documents.Count > 0)
                {
                    foreach (var doc in model.Documents)
                    {
                        if (string.IsNullOrWhiteSpace(doc.Base64Content) || string.IsNullOrWhiteSpace(doc.DocumentName))
                            continue;

                        try
                        {
                            string savedPath = SaveBase64Attachment(doc.Base64Content, doc.DocumentName, "EmployeeDocuments");

                            savedDocuments.Add(new
                            {
                                DocumentTypeId = doc.DocumentTypeId,
                                OtherDocumentType = doc.OtherDocumentType,
                                DocumentName = doc.DocumentName,
                                DocumentPath = savedPath,
                                DocumentNumber = doc.DocumentNumber,
                                IssueDate = doc.IssueDate,
                                ExpiryDate = doc.ExpiryDate
                            });
                        }
                        catch (Exception ex)
                        {
                            return new ApiResponse
                            {
                                Status = false,
                                Message = $"Error processing document '{doc.DocumentName}': {ex.Message}"
                            };
                        }
                    }
                }

                string documentJson = savedDocuments.Count > 0 ? JsonConvert.SerializeObject(savedDocuments) : null;

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

                var dt = await _database.ExecuteDataTableAsync("USP_ORG_NewEmployeeManagement",
                    new SqlParameter("@Status", "REGISTER"),
                    new SqlParameter("@FirstName", model.FirstName),
                    new SqlParameter("@MiddleName", (object?)model.MiddleName ?? DBNull.Value),
                    new SqlParameter("@LastName", (object?)model.LastName ?? DBNull.Value),
                    new SqlParameter("@OfficialEmail", model.OfficialEmail),
                    new SqlParameter("@PersonalEmail", (object?)model.PersonalEmail ?? DBNull.Value),
                    new SqlParameter("@Mobile", model.Mobile),
                    new SqlParameter("@AlternateMobile", (object?)model.AlternateMobile ?? DBNull.Value),
                    new SqlParameter("@Gender", (object?)model.Gender ?? DBNull.Value),
                    new SqlParameter("@DOB", (object?)model.DOB ?? DBNull.Value),
                    new SqlParameter("@DOJ", (object?)model.DOJ ?? DBNull.Value),
                    // Added BY suraj 240926
                    new SqlParameter("@ProbationRequired", model.ProbationRequired),
                    new SqlParameter("@ProbationPeriodValue", (object?)model.ProbationPeriodValue ?? DBNull.Value),
                    new SqlParameter("@ProbationPeriodUnit", (object?)model.ProbationPeriodUnit ?? DBNull.Value),

                    new SqlParameter("@MaritalStatus", (object?)model.MaritalStatus ?? DBNull.Value),
                    new SqlParameter("@BloodGroup", (object?)model.BloodGroup ?? DBNull.Value),
                    //new SqlParameter("@LoginId", model.UserName),
                    new SqlParameter("@Password", passwordHash),
                    new SqlParameter("@RefDesignationId", (object?)model.RefDesignationId ?? DBNull.Value),
                    new SqlParameter("@ReportingManagerId", (object?)model.ReportingManagerId ?? DBNull.Value),
                    new SqlParameter("@ReportingHRManagerId", (object?)model.ReportingHRManagerId ?? DBNull.Value),
                    new SqlParameter("@RefOfficeId", model.RefOfficeId),
                    new SqlParameter("@RefDepartmentId", model.RefDepartmentId),
                    new SqlParameter("@RefRoleId", model.RefRoleId),
                    new SqlParameter("@ShiftId", model.ShiftId),
                    new SqlParameter("@CreatedBy", (object?)model.CreatedBy ?? DBNull.Value),
                    new SqlParameter("@LeavePolicyId",(object?)model.RefLeavePolicyId ?? DBNull.Value),
                    new SqlParameter("@Education", (object?)educationJson ?? DBNull.Value),
                    new SqlParameter("@Bank", (object?)bankJson ?? DBNull.Value),
                    new SqlParameter("@FamilyDetails", (object?)familyJson ?? DBNull.Value),
                    new SqlParameter("@Document", (object?)documentJson ?? DBNull.Value)
                );

                var response = new ApiResponse();

                if (dt == null || dt.Rows.Count == 0)
                {
                    response.Status = false;
                    response.Message = "No response returned from database.";
                    return response;
                }

                var row = dt.Rows[0];
                bool status = Convert.ToInt32(row["Status"]) == 1;

                response.Status = status;
                response.Message = row["Message"]?.ToString();
                response.Data = status ? new { EmployeeId = Convert.ToInt32(row["EmployeeId"]), EmployeeCode = row["EmployeeCode"]?.ToString(), LoginId = row["LoginId"]?.ToString() }: null;

                return response;
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region Get Employee Master List
        public async Task<List<EmployeeMasterModel>> GetEmployeeMaster(int loggedInEmployeeId)
        {
            var employees = new List<EmployeeMasterModel>();

            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_ORG_NewEmployeeManagement",
                    new SqlParameter("@Status", "GET_EMPLOYEE_MASTER"),
                    new SqlParameter("@LoggedInEmployeeId", loggedInEmployeeId)
                );

                if (dt == null || dt.Rows.Count == 0)
                    return employees;

                foreach (DataRow row in dt.Rows)
                {
                    employees.Add(new EmployeeMasterModel
                    {
                        EmployeeId = Convert.ToInt32(row["EmployeeId"]),
                        EmployeeCode = row["EmployeeCode"]?.ToString() ?? "",
                        FirstName = row["FirstName"]?.ToString() ?? "",
                        MiddleName = row["MiddleName"] == DBNull.Value ? null : row["MiddleName"]?.ToString(),
                        LastName = row["LastName"] == DBNull.Value ? null : row["LastName"]?.ToString(),
                        EmployeeName = row["EmployeeName"]?.ToString() ?? "",
                        OfficialEmail = row["OfficialEmail"]?.ToString() ?? "",
                        PersonalEmail = row["PersonalEmail"] == DBNull.Value ? null : row["PersonalEmail"]?.ToString(),
                        Mobile = row["Mobile"]?.ToString() ?? "",
                        AlternateMobile = row["AlternateMobile"] == DBNull.Value ? null : row["AlternateMobile"]?.ToString(),
                        Gender = row["Gender"] == DBNull.Value ? null : row["Gender"]?.ToString(),
                        DOB = row["DOB"] == DBNull.Value ? null : Convert.ToDateTime(row["DOB"]),
                        DOJ = row["DOJ"] == DBNull.Value ? null : Convert.ToDateTime(row["DOJ"]),
                        MaritalStatus = row.Table.Columns.Contains("MaritalStatus") && row["MaritalStatus"] != DBNull.Value? row["MaritalStatus"]?.ToString() : null,
                        BloodGroup = row.Table.Columns.Contains("BloodGroup") && row["BloodGroup"] != DBNull.Value? row["BloodGroup"]?.ToString() : null,
                        LoginId = row["LoginId"] == DBNull.Value ? null : row["LoginId"]?.ToString(),
                        IsActive = Convert.ToBoolean(row["IsActive"]),
                        RefOfficeId = row["Ref_OfficeId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_OfficeId"]),
                        OfficeName = row["OfficeName"] == DBNull.Value ? null : row["OfficeName"]?.ToString(),
                        RefDepartmentId = row["Ref_DepartmentId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_DepartmentId"]),
                        DepartmentName = row["DepartmentName"] == DBNull.Value ? null : row["DepartmentName"]?.ToString(),
                        RefRoleId = row["Ref_RoleId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_RoleId"]),
                        RoleName = row["RoleName"] == DBNull.Value ? null : row["RoleName"]?.ToString(),
                        RefDesignationId = row["Ref_DesignationId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_DesignationId"]),
                        DesignationName = row["DesignationName"] == DBNull.Value ? null : row["DesignationName"]?.ToString(),
                        RefShiftId = row["Ref_ShiftId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_ShiftId"]),
                        ShiftName = row["ShiftName"] == DBNull.Value ? null : row["ShiftName"]?.ToString(),                      
                        RefLeavePolicyId = row["RefLeavePolicyId"] == DBNull.Value ? null : Convert.ToInt32(row["RefLeavePolicyId"]),
                        LeavePolicyName = row["LeavePolicyName"] == DBNull.Value ? null : row["LeavePolicyName"]?.ToString(),
                        RefReportingHRManagerId = row["Ref_ReportingHRManagerId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_ReportingHRManagerId"]),
                        ReportingManagerID = row["Ref_ReportingManagerId"] == DBNull.Value ? null : row["Ref_ReportingManagerId"]?.ToString()


                    });
                }

                return employees;
            }
            catch
            {
                throw;
            }
        }
        #endregion

        #region Get Employee Details by EmployeeId
        public async Task<EmployeeDetailsModel?> GetEmployeeDetails(int employeeId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync("USP_ORG_NewEmployeeManagement",
                    new SqlParameter("@Status", "GET_EMPLOYEE_DETAILS"),
                    new SqlParameter("@EmployeeId", employeeId)
                );

                if (ds == null || ds.Tables.Count == 0)
                    return null;

                var result = new EmployeeDetailsModel();

                // ==========================================
                // TABLE 0 - BASIC EMPLOYEE DETAILS
                // ==========================================

                if (ds.Tables[0].Rows.Count > 0)
                {
                    DataRow row = ds.Tables[0].Rows[0];

                    result.Employee = new EmployeeBasicDetailsModel
                    {
                        EmployeeId = Convert.ToInt32(row["EmployeeId"]),
                        EmployeeCode = row["EmployeeCode"]?.ToString() ?? "",
                        FirstName = row["FirstName"]?.ToString() ?? "",
                        MiddleName = row["MiddleName"] == DBNull.Value ? null : row["MiddleName"]?.ToString(),
                        LastName = row["LastName"] == DBNull.Value ? null : row["LastName"]?.ToString(),
                        OfficialEmail = row["OfficialEmail"] == DBNull.Value ? null : row["OfficialEmail"]?.ToString(),
                        PersonalEmail = row["PersonalEmail"] == DBNull.Value ? null : row["PersonalEmail"]?.ToString(),
                        Mobile = row["Mobile"] == DBNull.Value? null: row["Mobile"]?.ToString(),
                        AlternateMobile = row["AlternateMobile"] == DBNull.Value? null: row["AlternateMobile"]?.ToString(),
                        Gender = row["Gender"] == DBNull.Value? null: row["Gender"]?.ToString(),
                        DOB = row["DOB"] == DBNull.Value? null: Convert.ToDateTime(row["DOB"]),
                        DOJ = row["DOJ"] == DBNull.Value? null: Convert.ToDateTime(row["DOJ"]),
                        MaritalStatus = row.Table.Columns.Contains("MaritalStatus") && row["MaritalStatus"] != DBNull.Value? row["MaritalStatus"]?.ToString(): null,
                        BloodGroup = row.Table.Columns.Contains("BloodGroup") && row["BloodGroup"] != DBNull.Value ? row["BloodGroup"]?.ToString(): null,


                        ProbationRequired = row["ProbationRequired"] != DBNull.Value&& Convert.ToBoolean(row["ProbationRequired"]),
                        RefProbationStatusId = row["Ref_ProbationStatusId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_ProbationStatusId"]),
                        EmployeeProbationId = row["EmployeeProbationId"] == DBNull.Value? null: Convert.ToInt32(row["EmployeeProbationId"]),
                        ProbationPeriodValue = row["ProbationPeriodValue"] == DBNull.Value? null: Convert.ToInt32(row["ProbationPeriodValue"]),
                        ProbationPeriodUnit = row["ProbationPeriodUnit"] == DBNull.Value? null: row["ProbationPeriodUnit"]?.ToString(),
                        ProbationStartDate = row["ProbationStartDate"] == DBNull.Value? null: Convert.ToDateTime(row["ProbationStartDate"]),
                        ProbationEndDate = row["ProbationEndDate"] == DBNull.Value? null: Convert.ToDateTime(row["ProbationEndDate"]),
                        ProbationStatusName = row["ProbationStatusName"] == DBNull.Value? null: row["ProbationStatusName"]?.ToString(),
                        ConfirmationDate = row["ConfirmationDate"] == DBNull.Value? null: Convert.ToDateTime(row["ConfirmationDate"]),
                        ConfirmedBy = row["ConfirmedBy"] == DBNull.Value? null: Convert.ToInt32(row["ConfirmedBy"]),
                        ConfirmationRemark = row["ConfirmationRemark"] == DBNull.Value? null: row["ConfirmationRemark"]?.ToString(),

                        LoginId = row["LoginId"] == DBNull.Value? null: row["LoginId"]?.ToString(),
                        RefDesignationId = row["Ref_DesignationId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_DesignationId"]),
                        DesignationName = row["DesignationName"] == DBNull.Value? null: row["DesignationName"]?.ToString(),
                        RefReportingManagerId = row["Ref_ReportingManagerId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_ReportingManagerId"]),
                        ReportingManagerCode = row["ReportingManagerCode"] == DBNull.Value? null: row["ReportingManagerCode"]?.ToString(),
                        ReportingManagerName = row["ReportingManagerName"] == DBNull.Value? null: row["ReportingManagerName"]?.ToString(),

                        RefReportingHRManagerId = row["Ref_ReportingHRManagerId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_ReportingHRManagerId"]),
                        ReportingHRManagerCode = row["ReportingHRManagerCode"] == DBNull.Value ? null : row["ReportingHRManagerCode"]?.ToString(),
                        ReportingHRManagerName = row["ReportingHRManagerName"] == DBNull.Value ? null : row["ReportingHRManagerName"]?.ToString(),

                        RefCompanyId = row["Ref_CompanyId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_CompanyId"]),
                        CompanyName = row["CompanyName"] == DBNull.Value ? null : row["CompanyName"]?.ToString(),
                        RefRegionId = row["Ref_RegionId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_RegionId"]),
                        RegionName = row["RegionName"] == DBNull.Value? null: row["RegionName"]?.ToString(),
                        RefStateId = row["Ref_StateId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_StateId"]),
                        StateName = row["StateName"] == DBNull.Value? null: row["StateName"]?.ToString(),
                        RefOfficeId = row["Ref_OfficeId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_OfficeId"]),
                        OfficeName = row["OfficeName"] == DBNull.Value? null: row["OfficeName"]?.ToString(),
                        RefDepartmentId = row["Ref_DepartmentId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_DepartmentId"]),
                        DepartmentName = row["DepartmentName"] == DBNull.Value? null: row["DepartmentName"]?.ToString(),
                        RefRoleId = row["Ref_RoleId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_RoleId"]),
                        RoleName = row["RoleName"] == DBNull.Value? null: row["RoleName"]?.ToString(),
                        RefShiftId = row["Ref_ShiftId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_ShiftId"]),
                        ShiftName = row["ShiftName"] == DBNull.Value? null: row["ShiftName"]?.ToString(),
                        RefLeavePolicyId = row["RefLeavePolicyId"] == DBNull.Value  ? null : Convert.ToInt32(row["RefLeavePolicyId"]),
                        LeavePolicyName = row["LeavePolicyName"] == DBNull.Value ? null : row["LeavePolicyName"]?.ToString()
                    };
                }

                // TABLE 1 - EDUCATION

                if (ds.Tables.Count > 1)
                {
                    foreach (DataRow row in ds.Tables[1].Rows)
                    {
                        result.Education.Add(new EmployeeEducationModel
                        {
                            EducationId = row["EducationId"] == DBNull.Value ? null : Convert.ToInt32(row["EducationId"]),
                            QualificationId = Convert.ToInt32(row["QualificationId"]),
                            OtherQualification = row["OtherQualification"] == DBNull.Value ? null : row["OtherQualification"]?.ToString(),
                            QualificationName = row["QualificationName"] == DBNull.Value? null: row["QualificationName"]?.ToString(),
                            UniversityName = row["UniversityName"] == DBNull.Value? null: row["UniversityName"]?.ToString(),
                            InstituteName = row["InstituteName"] == DBNull.Value? null: row["InstituteName"]?.ToString(),
                            PassingYear = row["PassingYear"] == DBNull.Value? null: Convert.ToInt32(row["PassingYear"]),
                            Percentage = row["Percentage"] == DBNull.Value? null: Convert.ToDecimal(row["Percentage"]),
                            Grade = row["Grade"] == DBNull.Value ? null: row["Grade"]?.ToString(),
                            Specialization = row["Specialization"] == DBNull.Value? null: row["Specialization"]?.ToString(),
                            CertificatePath = row["CertificatePath"] == DBNull.Value? null: row["CertificatePath"]?.ToString()
                        });
                    }
                }

                // TABLE 2 - BANK

                if (ds.Tables.Count > 2)
                {
                    foreach (DataRow row in ds.Tables[2].Rows)
                    {
                        result.BankDetails.Add(new EmployeeBankModel
                        {
                            BankId = Convert.ToInt32(row["BankId"]),
                            OtherBankName = row["OtherBankName"] == DBNull.Value ? null : row["OtherBankName"]?.ToString(),
                            BankName = row["BankName"] == DBNull.Value ? null: row["BankName"]?.ToString(),
                            AccountTypeId = Convert.ToInt32(row["AccountTypeId"]),
                            AccountTypeName = row["AccountTypeName"] == DBNull.Value? null: row["AccountTypeName"]?.ToString(),
                            AccountHolderName = row["AccountHolderName"] == DBNull.Value ? null: row["AccountHolderName"]?.ToString(),
                            AccountNumber = row["AccountNumber"] == DBNull.Value ? null: row["AccountNumber"]?.ToString(),
                            IFSCCode = row["IFSCCode"] == DBNull.Value? null: row["IFSCCode"]?.ToString()
                        });
                    }
                }

                // TABLE 3 - FAMILY

                if (ds.Tables.Count > 3)
                {
                    foreach (DataRow row in ds.Tables[3].Rows)
                    {
                        result.FamilyDetails.Add(new EmployeeFamilyModel
                        {
                            RelationshipId = Convert.ToInt32(row["RelationshipId"]),
                            RelationshipName = row["RelationshipName"] == DBNull.Value? null: row["RelationshipName"]?.ToString(),
                            OtherRelationship = row["OtherRelationship"] == DBNull.Value ? null : row["OtherRelationship"]?.ToString(),
                            FamilyMemberName = row["FamilyMemberName"] == DBNull.Value? null: row["FamilyMemberName"]?.ToString(),
                            Occupation = row["Occupation"] == DBNull.Value? null: row["Occupation"]?.ToString(),
                            ContactNumber = row["ContactNumber"] == DBNull.Value? null: row["ContactNumber"]?.ToString(),
                            IsDependent = row["IsDependent"] == DBNull.Value? null: Convert.ToBoolean(row["IsDependent"]),
                            DOB = row["DOB"] == DBNull.Value? null: Convert.ToDateTime(row["DOB"]),
                            IsEmergencyContact = row["IsEmergencyContact"] == DBNull.Value ? null : Convert.ToBoolean(row["IsEmergencyContact"])
                        });
                    }
                }

                // TABLE 4 - DOCUMENTS

                if (ds.Tables.Count > 4)
                {
                    foreach (DataRow row in ds.Tables[4].Rows)
                    {
                        result.Documents.Add(new EmployeeDocumentModel
                        {
                          
                            DocumentTypeId = Convert.ToInt32(row["DocumentTypeId"]),
                            OtherDocumentType = row["OtherDocumentType"] == DBNull.Value ? null : row["OtherDocumentType"]?.ToString(),
                            DocumentTypeName = row["DocumentTypeName"] == DBNull.Value ? null: row["DocumentTypeName"]?.ToString(),
                            IsMandatory = row["IsMandatory"] != DBNull.Value && Convert.ToBoolean(row["IsMandatory"]),
                            FieldKind = row.Table.Columns.Contains("FieldKind") && row["FieldKind"] != DBNull.Value? row["FieldKind"]?.ToString(): null,
                            DocumentName = row["DocumentName"] == DBNull.Value? null: row["DocumentName"]?.ToString(),
                            DocumentPath = row["DocumentPath"] == DBNull.Value? null: BuildFileUrl(row["DocumentPath"]?.ToString()),
                            IssueDate = row["IssueDate"] == DBNull.Value ? null : Convert.ToDateTime(row["IssueDate"]),
                            DocumentNumber = row.Table.Columns.Contains("DocumentNumber") && row["DocumentNumber"] != DBNull.Value? row["DocumentNumber"]?.ToString(): null,
                            ExpiryDate = row["ExpiryDate"] == DBNull.Value? null: Convert.ToDateTime(row["ExpiryDate"]),
                            Verified = row["Verified"] == DBNull.Value? null: Convert.ToBoolean(row["Verified"]),
                            VerifiedBy = row["VerifiedBy"] == DBNull.Value? null: Convert.ToInt32(row["VerifiedBy"]),
                            VerifiedOn = row["VerifiedOn"] == DBNull.Value? null: Convert.ToDateTime(row["VerifiedOn"]),
                            Remarks = row["Remarks"] == DBNull.Value? null: row["Remarks"]?.ToString()
                        });
                    }
                }

                return result;
            }
            catch
            {
                throw;
            }
        }
        #endregion

        #region Update Employee Details
        public async Task<ApiResponse> UpdateEmployee(UpdateEmployeeRequest model)
        {
            try
            {
                string educationJson =model.Education != null && model.Education.Count > 0 ? JsonConvert.SerializeObject(model.Education): null;
                string bankJson =model.Bank != null && model.Bank.Count > 0 ? JsonConvert.SerializeObject(model.Bank) : null;
                Console.WriteLine("BANK JSON:");
                Console.WriteLine(bankJson);
                string familyJson = model.FamilyDetails != null && model.FamilyDetails.Count > 0 ? JsonConvert.SerializeObject(model.FamilyDetails): null;
            

                List<object> documentList = new List<object>();

                _logger.LogInformation("========== UPDATE EMPLOYEE DOCUMENTS START | EmployeeId: {EmployeeId} ==========",model.EmployeeId);

                _logger.LogInformation("Document count received from request: {DocumentCount}",model.Document?.Count ?? 0);

                if (model.Document != null)
                {
                    foreach (var doc in model.Document)
                    {
                        _logger.LogInformation("Document received | TypeId: {DocumentTypeId}, Name: {DocumentName}, HasBase64: {HasBase64}, Path: {DocumentPath}",
                            doc.DocumentTypeId,
                            doc.DocumentName,
                            !string.IsNullOrWhiteSpace(doc.Base64Content),
                            doc.DocumentPath
                        );

                        string? documentPath = doc.DocumentPath;

                        // =====================================================
                        // NEW DOCUMENT
                        // =====================================================
                        if (!string.IsNullOrWhiteSpace(doc.Base64Content))
                        {
                            if (string.IsNullOrWhiteSpace(doc.DocumentName))
                            {
                                _logger.LogWarning("Skipping document because DocumentName is empty | EmployeeId: {EmployeeId}",model.EmployeeId);
                                continue;
                            }

                            try
                            {
                                documentPath = SaveBase64Attachment(doc.Base64Content,doc.DocumentName,"EmployeeDocuments");

                                _logger.LogInformation("New document saved | TypeId: {DocumentTypeId}, Path: {DocumentPath}",doc.DocumentTypeId,documentPath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(
                                    ex,
                                    "Error saving document | TypeId: {DocumentTypeId}, Name: {DocumentName}",
                                    doc.DocumentTypeId,
                                    doc.DocumentName
                                );

                                continue;
                            }
                        }

                        // =====================================================
                        // EXISTING DOCUMENT
                        // =====================================================
                        // For an existing document Base64Content is empty,
                        // so we keep the existing DocumentPath.
                        // =====================================================

                        documentList.Add(new
                        {
                            DocumentTypeId = doc.DocumentTypeId,
                            OtherDocumentType = doc.OtherDocumentType,
                            DocumentName = doc.DocumentName,
                            DocumentPath = documentPath,
                            DocumentNumber = doc.DocumentNumber,
                            IssueDate = doc.IssueDate,
                            ExpiryDate = doc.ExpiryDate,
                            Verified = doc.Verified,
                            VerifiedBy = doc.VerifiedBy,
                            VerifiedOn = doc.VerifiedOn,
                            Remarks = doc.Remarks
                        });
                    }
                }

                // IMPORTANT:
                // null  = document section was not sent
                // []    = document section was sent but all documents removed
                string? documentJson = model.Document != null? JsonConvert.SerializeObject(documentList): null;

                _logger.LogInformation("Final document JSON: {DocumentJson}",documentJson);

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@EmployeeId", model.EmployeeId),

                    new SqlParameter("@FirstName",(object?)model.FirstName ?? DBNull.Value),
                    new SqlParameter("@MiddleName",(object?)model.MiddleName ?? DBNull.Value),
                    new SqlParameter("@LastName",(object?)model.LastName ?? DBNull.Value),
                    new SqlParameter("@OfficialEmail",(object?)model.OfficialEmail ?? DBNull.Value),
                    new SqlParameter("@PersonalEmail",(object?)model.PersonalEmail ?? DBNull.Value),
                    new SqlParameter("@Mobile",(object?)model.Mobile ?? DBNull.Value),
                    new SqlParameter("@AlternateMobile",(object?)model.AlternateMobile ?? DBNull.Value),
                    new SqlParameter("@Gender",(object?)model.Gender ?? DBNull.Value),
                    new SqlParameter("@DOB",(object?)model.DOB ?? DBNull.Value),
                    new SqlParameter("@DOJ",(object?)model.DOJ ?? DBNull.Value),
                    new SqlParameter("@MaritalStatus",(object?)model.MaritalStatus ?? DBNull.Value),
                    new SqlParameter("@BloodGroup",(object?)model.BloodGroup ?? DBNull.Value),
                    new SqlParameter("@ProbationRequired",(object?)model.ProbationRequired ?? DBNull.Value),
                    new SqlParameter("@ProbationPeriodValue",(object?)model.ProbationPeriodValue ?? DBNull.Value),
                    new SqlParameter("@ProbationPeriodUnit",(object?)model.ProbationPeriodUnit ?? DBNull.Value),
                    new SqlParameter("@RefDesignationId",(object?)model.RefDesignationId ?? DBNull.Value),
                    new SqlParameter("@ReportingManagerId",(object?)model.ReportingManagerId ?? DBNull.Value),
                    new SqlParameter("@ReportingHRManagerId",(object?)model.ReportingHRManagerId ?? DBNull.Value),
                    new SqlParameter("@RefOfficeId",(object?)model.RefOfficeId ?? DBNull.Value),
                    new SqlParameter("@RefDepartmentId",(object?)model.RefDepartmentId ?? DBNull.Value),
                    new SqlParameter("@RefRoleId",(object?)model.RefRoleId ?? DBNull.Value),
                    new SqlParameter("@ShiftId",(object?)model.ShiftId ?? DBNull.Value),
                    new SqlParameter("@LeavePolicyId",(object?)model.RefLeavePolicyId ?? DBNull.Value),
                    new SqlParameter("@UpdatedBy",(object?)model.UpdatedBy ?? DBNull.Value),
                    new SqlParameter("@Education",(object?)educationJson ?? DBNull.Value),
                    new SqlParameter("@Bank",(object?)bankJson ?? DBNull.Value),
                    new SqlParameter("@FamilyDetails",(object?)familyJson ?? DBNull.Value),
                    new SqlParameter("@Document",(object?)documentJson ?? DBNull.Value)
                };
                _logger.LogInformation("Calling USP_ORG_UpdateEmployee | EmployeeId: {EmployeeId} | Document JSON: {DocumentJson}",model.EmployeeId,documentJson ?? "NULL");

                DataTable dt = await _database.ExecuteDataTableAsync("USP_ORG_UpdateEmployee",parameters.ToArray());


                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Employee update failed."
                    };
                }


                DataRow row = dt.Rows[0];

                _logger.LogInformation("USP_ORG_UpdateEmployee returned | Columns: {Columns} | RowCount: {RowCount}",string.Join(", ", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName)),
                    dt.Rows.Count
                );

                foreach (DataColumn column in dt.Columns)
                {
                    _logger.LogInformation("USP Result | {Column} = {Value}",column.ColumnName,row[column]);
                }

                if (!dt.Columns.Contains("Status"))
                {
                    _logger.LogError("USP_ORG_UpdateEmployee result does NOT contain Status column.");

                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Employee update failed. Stored procedure did not return Status."
                    };
                }

                bool status = Convert.ToInt32(row["Status"]) == 1;

                string message = dt.Columns.Contains("Message")? row["Message"]?.ToString(): "Employee update completed.";
                object data = null;

                if (status && dt.Columns.Contains("EmployeeId"))
                {
                    data = new
                    {
                        EmployeeId = Convert.ToInt32(row["EmployeeId"])
                    };
                }

                return new ApiResponse
                {
                    Status = status,
                    Message = message,
                    Data = data
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion
        public async Task<DataTable> GetProbationStatus()
        {
            return await _database.ExecuteDataTableAsync("USP_ORG_NewEmployeeManagement",
                new SqlParameter("@Status", "GET_PROBATION_STATUS")
            );
        }

        public async Task<DataTable> GetHRManagersByOffice(int officeId)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching HR managers for OfficeId: {OfficeId}", officeId);

                var dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_NewEmployeeManagement",
                    new SqlParameter("@Status", "GET_HR_EXECUTIVES_BY_OFFICE"),
                    new SqlParameter("@OfficeId", officeId)
                );

                _logger.LogInformation(
                    "HR managers query completed. Rows: {RowCount}",
                    dt?.Rows.Count ?? 0);

                return dt;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching HR managers for OfficeId: {OfficeId}",
                    officeId);

                throw;
            }
        }

    }
}
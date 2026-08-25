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

        public EmployeeRepository(IDatabaseService database)
        {
            _database = database;
        }

        // Employee Registration 
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
                    //new SqlParameter("@LoginId", model.UserName),
                    new SqlParameter("@Password", passwordHash),
                    new SqlParameter("@RefDesignationId", (object?)model.RefDesignationId ?? DBNull.Value),
                    new SqlParameter("@ReportingManagerId", (object?)model.ReportingManagerId ?? DBNull.Value),
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
                response.Data = status
                    ? new { EmployeeId = Convert.ToInt32(row["EmployeeId"]), EmployeeCode = row["EmployeeCode"]?.ToString(), LoginId = row["LoginId"]?.ToString() }
                    : null;

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

        public async Task<ApiResponse> NewRegister(EmployeeRegisterRequest model)
        {
            try
            {
                // Serialize Child Collections
                string educationJson = model.EducationDetails != null && model.EducationDetails.Count > 0
                    ? JsonConvert.SerializeObject(model.EducationDetails)
                    : null;

                string bankJson = model.BankDetails != null && model.BankDetails.Count > 0
                    ? JsonConvert.SerializeObject(model.BankDetails)
                    : null;

                string familyJson = model.FamilyDetails != null && model.FamilyDetails.Count > 0
                    ? JsonConvert.SerializeObject(model.FamilyDetails)
                    : null;

                string documentJson = model.Documents != null && model.Documents.Count > 0
                    ? JsonConvert.SerializeObject(model.Documents)
                    : null;

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

                var dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_NewEmployeeManagement",
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
                    //new SqlParameter("@LoginId", model.UserName),
                    new SqlParameter("@Password", passwordHash),
                    new SqlParameter("@RefDesignationId", (object?)model.RefDesignationId ?? DBNull.Value),
                    new SqlParameter("@ReportingManagerId", (object?)model.ReportingManagerId ?? DBNull.Value),
                    new SqlParameter("@RefOfficeId", model.RefOfficeId),
                    new SqlParameter("@RefDepartmentId", model.RefDepartmentId),
                    new SqlParameter("@RefRoleId", model.RefRoleId),
                    new SqlParameter("@ShiftId", model.ShiftId),
                    new SqlParameter("@CreatedBy", (object?)model.CreatedBy ?? DBNull.Value),
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
                response.Data = status
                    ? new { EmployeeId = Convert.ToInt32(row["EmployeeId"]), EmployeeCode = row["EmployeeCode"]?.ToString(), LoginId = row["LoginId"]?.ToString() }
                    : null;

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

        public async Task<List<EmployeeMasterModel>> GetEmployeeMaster()
        {
            var employees = new List<EmployeeMasterModel>();

            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_ORG_NewEmployeeManagement",
                    new SqlParameter("@Status", "GET_EMPLOYEE_MASTER")
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

                        ShiftName = row["ShiftName"] == DBNull.Value ? null : row["ShiftName"]?.ToString()
                    });
                }

                return employees;
            }
            catch
            {
                throw;
            }
        }
        public async Task<EmployeeDetailsModel?> GetEmployeeDetails(int employeeId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(
                    "USP_ORG_NewEmployeeManagement",
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

                        Mobile = row["Mobile"] == DBNull.Value
                            ? null
                            : row["Mobile"]?.ToString(),

                        AlternateMobile = row["AlternateMobile"] == DBNull.Value
                            ? null
                            : row["AlternateMobile"]?.ToString(),

                        Gender = row["Gender"] == DBNull.Value
                            ? null
                            : row["Gender"]?.ToString(),

                        DOB = row["DOB"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(row["DOB"]),

                        DOJ = row["DOJ"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(row["DOJ"]),

                        LoginId = row["LoginId"] == DBNull.Value
                            ? null
                            : row["LoginId"]?.ToString(),

                        RefDesignationId = row["Ref_DesignationId"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(row["Ref_DesignationId"]),

                        DesignationName = row["DesignationName"] == DBNull.Value
                            ? null
                            : row["DesignationName"]?.ToString(),

                        RefReportingManagerId = row["Ref_ReportingManagerId"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(row["Ref_ReportingManagerId"]),

                        ReportingManagerCode = row["ReportingManagerCode"] == DBNull.Value
                            ? null
                            : row["ReportingManagerCode"]?.ToString(),

                        ReportingManagerName = row["ReportingManagerName"] == DBNull.Value
                            ? null
                            : row["ReportingManagerName"]?.ToString(),

                        RefCompanyId = row["Ref_CompanyId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_CompanyId"]),

                        CompanyName = row["CompanyName"] == DBNull.Value ? null : row["CompanyName"]?.ToString(),

                        RefRegionId = row["Ref_RegionId"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(row["Ref_RegionId"]),

                        RegionName = row["RegionName"] == DBNull.Value
                            ? null
                            : row["RegionName"]?.ToString(),

                        RefStateId = row["Ref_StateId"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(row["Ref_StateId"]),

                        StateName = row["StateName"] == DBNull.Value
                            ? null
                            : row["StateName"]?.ToString(),

                        RefOfficeId = row["Ref_OfficeId"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(row["Ref_OfficeId"]),

                        OfficeName = row["OfficeName"] == DBNull.Value
                            ? null
                            : row["OfficeName"]?.ToString(),

                        RefDepartmentId = row["Ref_DepartmentId"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(row["Ref_DepartmentId"]),

                        DepartmentName = row["DepartmentName"] == DBNull.Value
                            ? null
                            : row["DepartmentName"]?.ToString(),

                        RefRoleId = row["Ref_RoleId"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(row["Ref_RoleId"]),

                        RoleName = row["RoleName"] == DBNull.Value
                            ? null
                            : row["RoleName"]?.ToString(),

                        RefShiftId = row["Ref_ShiftId"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(row["Ref_ShiftId"]),

                        ShiftName = row["ShiftName"] == DBNull.Value
                            ? null
                            : row["ShiftName"]?.ToString()
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

                            QualificationName = row["QualificationName"] == DBNull.Value
                                ? null
                                : row["QualificationName"]?.ToString(),

                            UniversityName = row["UniversityName"] == DBNull.Value
                                ? null
                                : row["UniversityName"]?.ToString(),

                            InstituteName = row["InstituteName"] == DBNull.Value
                                ? null
                                : row["InstituteName"]?.ToString(),

                            PassingYear = row["PassingYear"] == DBNull.Value
                                ? null
                                : Convert.ToInt32(row["PassingYear"]),

                            Percentage = row["Percentage"] == DBNull.Value
                                ? null
                                : Convert.ToDecimal(row["Percentage"]),

                            Grade = row["Grade"] == DBNull.Value
                                ? null
                                : row["Grade"]?.ToString(),
                            Specialization = row["Specialization"] == DBNull.Value
                                ? null
                                : row["Specialization"]?.ToString(),

                            CertificatePath = row["CertificatePath"] == DBNull.Value
                                ? null
                                : row["CertificatePath"]?.ToString()
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

                            BankName = row["BankName"] == DBNull.Value
                                ? null
                                : row["BankName"]?.ToString(),

                            AccountTypeId = Convert.ToInt32(row["AccountTypeId"]),

                            AccountTypeName = row["AccountTypeName"] == DBNull.Value
                                ? null
                                : row["AccountTypeName"]?.ToString(),

                            AccountHolderName = row["AccountHolderName"] == DBNull.Value
                                ? null
                                : row["AccountHolderName"]?.ToString(),

                            AccountNumber = row["AccountNumber"] == DBNull.Value
                                ? null
                                : row["AccountNumber"]?.ToString(),

                            IFSCCode = row["IFSCCode"] == DBNull.Value
                                ? null
                                : row["IFSCCode"]?.ToString()
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

                            RelationshipName = row["RelationshipName"] == DBNull.Value
                                ? null
                                : row["RelationshipName"]?.ToString(),

                            FamilyMemberName = row["FamilyMemberName"] == DBNull.Value
                                ? null
                                : row["FamilyMemberName"]?.ToString(),

                            Occupation = row["Occupation"] == DBNull.Value
                                ? null
                                : row["Occupation"]?.ToString(),

                            ContactNumber = row["ContactNumber"] == DBNull.Value
                                ? null
                                : row["ContactNumber"]?.ToString(),

                            IsDependent = row["IsDependent"] == DBNull.Value
                                ? null
                                : Convert.ToBoolean(row["IsDependent"]),

                            DOB = row["DOB"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(row["DOB"]),

                            IsEmergencyContact = row["IsEmergencyContact"] == DBNull.Value
                                ? null
                                : Convert.ToBoolean(row["IsEmergencyContact"])
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

                            DocumentTypeName = row["DocumentTypeName"] == DBNull.Value
                                ? null
                                : row["DocumentTypeName"]?.ToString(),

                            IsMandatory = row["IsMandatory"] != DBNull.Value
                                && Convert.ToBoolean(row["IsMandatory"]),

                            DocumentName = row["DocumentName"] == DBNull.Value
                                ? null
                                : row["DocumentName"]?.ToString(),

                            DocumentPath = row["DocumentPath"] == DBNull.Value
                                ? null
                                : row["DocumentPath"]?.ToString(),
                            IssueDate = row["IssueDate"] == DBNull.Value ? null : Convert.ToDateTime(row["IssueDate"]),

                            ExpiryDate = row["ExpiryDate"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(row["ExpiryDate"]),

                            Verified = row["Verified"] == DBNull.Value
                                ? null
                                : Convert.ToBoolean(row["Verified"]),

                            VerifiedBy = row["VerifiedBy"] == DBNull.Value
                                ? null
                                : Convert.ToInt32(row["VerifiedBy"]),

                            VerifiedOn = row["VerifiedOn"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(row["VerifiedOn"]),

                            Remarks = row["Remarks"] == DBNull.Value
                                ? null
                                : row["Remarks"]?.ToString()
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

        public async Task<ApiResponse> UpdateEmployee(UpdateEmployeeRequest model)
        {
            try
            {
                string educationJson =
                    model.Education != null && model.Education.Count > 0
                        ? JsonConvert.SerializeObject(model.Education)
                        : null;

                string bankJson =
                    model.Bank != null && model.Bank.Count > 0
                        ? JsonConvert.SerializeObject(model.Bank)
                        : null;
                Console.WriteLine("BANK JSON:");
                Console.WriteLine(bankJson);
                string familyJson =
                    model.FamilyDetails != null && model.FamilyDetails.Count > 0
                        ? JsonConvert.SerializeObject(model.FamilyDetails)
                        : null;

                string documentJson =
                    model.Document != null && model.Document.Count > 0
                        ? JsonConvert.SerializeObject(model.Document)
                        : null;


                var parameters = new List<SqlParameter>
        {
            new SqlParameter("@EmployeeId", model.EmployeeId),

            new SqlParameter("@FirstName",
                (object?)model.FirstName ?? DBNull.Value),

            new SqlParameter("@MiddleName",
                (object?)model.MiddleName ?? DBNull.Value),

            new SqlParameter("@LastName",
                (object?)model.LastName ?? DBNull.Value),

            new SqlParameter("@OfficialEmail",
                (object?)model.OfficialEmail ?? DBNull.Value),

            new SqlParameter("@PersonalEmail",
                (object?)model.PersonalEmail ?? DBNull.Value),

            new SqlParameter("@Mobile",
                (object?)model.Mobile ?? DBNull.Value),

            new SqlParameter("@AlternateMobile",
                (object?)model.AlternateMobile ?? DBNull.Value),

            new SqlParameter("@Gender",
                (object?)model.Gender ?? DBNull.Value),

            new SqlParameter("@DOB",
                (object?)model.DOB ?? DBNull.Value),

            new SqlParameter("@DOJ",
                (object?)model.DOJ ?? DBNull.Value),

            new SqlParameter("@RefDesignationId",
                (object?)model.RefDesignationId ?? DBNull.Value),

            new SqlParameter("@ReportingManagerId",
                (object?)model.ReportingManagerId ?? DBNull.Value),

            new SqlParameter("@RefOfficeId",
                (object?)model.RefOfficeId ?? DBNull.Value),

            new SqlParameter("@RefDepartmentId",
                (object?)model.RefDepartmentId ?? DBNull.Value),

            new SqlParameter("@RefRoleId",
                (object?)model.RefRoleId ?? DBNull.Value),

            new SqlParameter("@ShiftId",
                (object?)model.ShiftId ?? DBNull.Value),

            new SqlParameter("@UpdatedBy",
                (object?)model.UpdatedBy ?? DBNull.Value),

            new SqlParameter("@Education",
                (object?)educationJson ?? DBNull.Value),

            new SqlParameter("@Bank",
                (object?)bankJson ?? DBNull.Value),

            new SqlParameter("@FamilyDetails",
                (object?)familyJson ?? DBNull.Value),

            new SqlParameter("@Document",
                (object?)documentJson ?? DBNull.Value)
        };


                DataTable dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_UpdateEmployee",
                    parameters.ToArray()
                );


                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Employee update failed."
                    };
                }


                DataRow row = dt.Rows[0];

                bool status = Convert.ToInt32(row["Status"]) == 1;


                return new ApiResponse
                {
                    Status = status,

                    Message = row["Message"]?.ToString(),

                    Data = status && dt.Columns.Contains("EmployeeId")
                        ? new
                        {
                            EmployeeId = Convert.ToInt32(row["EmployeeId"])
                        }
                        : null
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
    }
}
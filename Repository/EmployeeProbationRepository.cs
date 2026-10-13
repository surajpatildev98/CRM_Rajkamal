using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Services;
using Microsoft.Data.SqlClient;
using System.Data;
using static CRMWebServices.Models.Employee_Probation.EmployeeProbationModels;

namespace CRMWebServices.Repository
{
    public class EmployeeProbationRepository : IEmployeeProbationRepository
    {
        private readonly IDatabaseService _database;
        private readonly ILogger<EmployeeProbationRepository> _logger;
        private readonly ISchedulerExecutionService _schedulerExecutionService;
        private const string SP_NAME = "USP_ORG_EmployeeProbationManagement";
        public EmployeeProbationRepository(IDatabaseService database,ILogger<EmployeeProbationRepository> logger,ISchedulerExecutionService schedulerExecutionService)
        {
            _database = database;
            _logger = logger;
            _schedulerExecutionService = schedulerExecutionService;
        }

        #region  Helper Methods
        private string? BuildFileUrl(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            var relative = filePath.Replace("Uploads/", "").Replace("Uploads\\", "");
            var segments = relative.Split('/').Select(Uri.EscapeDataString);

            return "/Uploads/" + string.Join("/", segments);
        }
        private string SaveBase64Attachment(string base64Content, string fileName, string subFolder)
        {
            var base64Data = base64Content.Contains(",")
                ? base64Content.Substring(base64Content.IndexOf(",") + 1)
                : base64Content;

            byte[] fileBytes = Convert.FromBase64String(base64Data);

            string uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", subFolder);

            if (!Directory.Exists(uploadsRoot))
                Directory.CreateDirectory(uploadsRoot);

            string uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            string fullPath = Path.Combine(uploadsRoot, uniqueFileName);

            File.WriteAllBytes(fullPath, fileBytes);

            return Path.Combine("Uploads", subFolder, uniqueFileName).Replace("\\", "/");
        }
       
        #endregion

        #region GET EMPLOYEES
        public async Task<ApiResponse> GetProbationEmployees()
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,new SqlParameter("@Status", "GET_EMPLOYEES"));

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = true,
                        Message = "No probation employees found.",
                        Data = new List<EmployeeProbationListItem>()
                    };
                }

                var employees = dt.Rows.Cast<DataRow>()
                    .Select(r => new EmployeeProbationListItem
                    {
                        EmployeeId = Convert.ToInt32(r["EmployeeId"]),
                        EmployeeCode = r["EmployeeCode"] == DBNull.Value? null: r["EmployeeCode"].ToString(),
                        EmployeeName = r["EmployeeName"] == DBNull.Value? null: r["EmployeeName"].ToString(),
                        DOJ = r["DOJ"] == DBNull.Value? null: Convert.ToDateTime(r["DOJ"]),
                        ProbationRequired = r["ProbationRequired"] == DBNull.Value? false: Convert.ToBoolean(r["ProbationRequired"]),
                        // Role
                        RoleId =r["RoleId"] == DBNull.Value? null: Convert.ToInt32(r["RoleId"]),
                        RoleName =r["RoleName"] == DBNull.Value? null: r["RoleName"].ToString(),
                        RoleCode =r["RoleCode"] == DBNull.Value? null: r["RoleCode"].ToString(),
                        IsPrimaryRole =r["IsPrimaryRole"] == DBNull.Value? null: Convert.ToBoolean(r["IsPrimaryRole"]),
                        // Designation
                        DesignationId =r["DesignationId"] == DBNull.Value? null: Convert.ToInt32(r["DesignationId"]),
                        DesignationCode =r["DesignationCode"] == DBNull.Value? null: r["DesignationCode"].ToString(),
                        DesignationName =r["DesignationName"] == DBNull.Value? null: r["DesignationName"].ToString(),
                        ProbationStatusId = r["ProbationStatusId"] == DBNull.Value? null: Convert.ToInt32(r["ProbationStatusId"]),
                        StatusCode = r["StatusCode"] == DBNull.Value? null: r["StatusCode"].ToString(),
                        StatusName = r["StatusName"] == DBNull.Value? null: r["StatusName"].ToString(),
                        EmployeeProbationId = r["EmployeeProbationId"] == DBNull.Value? null: Convert.ToInt32(r["EmployeeProbationId"]),
                        ProbationPeriodValue = r["ProbationPeriodValue"] == DBNull.Value? null: Convert.ToInt32(r["ProbationPeriodValue"]),
                        ProbationPeriodUnit = r["ProbationPeriodUnit"] == DBNull.Value? null: r["ProbationPeriodUnit"].ToString(),
                        ProbationStartDate = r["ProbationStartDate"] == DBNull.Value? null: Convert.ToDateTime(r["ProbationStartDate"]),
                        ProbationEndDate = r["ProbationEndDate"] == DBNull.Value? null: Convert.ToDateTime(r["ProbationEndDate"]),
                        IsCurrent = r["IsCurrent"] == DBNull.Value? null: Convert.ToBoolean(r["IsCurrent"])
                    }).ToList();

                return new ApiResponse
                {
                    Status = true,
                    Message = "Probation employees retrieved successfully.",
                    Data = employees
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,"Database error while getting probation employees.");

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Error while getting probation employees.");

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        #endregion

        #region GET EMPLOYEE PROBATION
        public async Task<ApiResponse> GetEmployeeProbation(int employeeId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status","GET_EMPLOYEE_PROBATION"),
                    new SqlParameter("@EmployeeId",employeeId)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Employee probation details not found."
                    };
                }

                var r = dt.Rows[0];

                var result = new EmployeeProbationDetails
                {
                    EmployeeId = Convert.ToInt32(r["EmployeeId"]),
                    EmployeeCode = r["EmployeeCode"] == DBNull.Value? null: r["EmployeeCode"].ToString(),
                    EmployeeName = r["EmployeeName"] == DBNull.Value? null: r["EmployeeName"].ToString(),
                    OfficialEmail = r["OfficialEmail"] == DBNull.Value? null: r["OfficialEmail"].ToString(),
                    Mobile = r["Mobile"] == DBNull.Value? null: r["Mobile"].ToString(),
                    DOJ = r["DOJ"] == DBNull.Value? null: Convert.ToDateTime(r["DOJ"]),
                    DOB = r["DOB"] == DBNull.Value? null: Convert.ToDateTime(r["DOB"]),                 
                    RefDesignationId = r["DesignationId"] == DBNull.Value? null: Convert.ToInt32(r["DesignationId"]),
                    DesignationCode = r["DesignationCode"] == DBNull.Value? null: r["DesignationCode"].ToString(),
                    DesignationName = r["DesignationName"] == DBNull.Value? null: r["DesignationName"].ToString(),
                    RoleId =r["RoleId"] == DBNull.Value? null: Convert.ToInt32(r["RoleId"]),
                    RoleName =r["RoleName"] == DBNull.Value? null: r["RoleName"].ToString(),
                    RoleCode =r["RoleCode"] == DBNull.Value? null: r["RoleCode"].ToString(),
                    IsPrimaryRole =r["IsPrimaryRole"] == DBNull.Value? null: Convert.ToBoolean(r["IsPrimaryRole"]),
                    RefReportingManagerId = r["RefReportingManagerId"] == DBNull.Value? null: Convert.ToInt32(r["RefReportingManagerId"]),
                    RefReportingHRManagerId = r["RefReportingHRManagerId"] == DBNull.Value? null: Convert.ToInt32(r["RefReportingHRManagerId"]),
                    EmploymentStatus = r["EmploymentStatus"] == DBNull.Value? null: r["EmploymentStatus"].ToString(),
                    ProbationRequired = r["ProbationRequired"] == DBNull.Value? false: Convert.ToBoolean(r["ProbationRequired"]),
                    RefProbationStatusId = r["RefProbationStatusId"] == DBNull.Value? null: Convert.ToInt32(r["RefProbationStatusId"]),
                    StatusCode = r["StatusCode"] == DBNull.Value? null: r["StatusCode"].ToString(),
                    StatusName = r["StatusName"] == DBNull.Value? null: r["StatusName"].ToString(),
                    ConfirmationDate = r["ConfirmationDate"] == DBNull.Value? null: Convert.ToDateTime(r["ConfirmationDate"]),
                    // Current probation
                    EmployeeProbationId = r["EmployeeProbationId"] == DBNull.Value? null: Convert.ToInt32(r["EmployeeProbationId"]),
                    ProbationPeriodValue = r["ProbationPeriodValue"] == DBNull.Value? null: Convert.ToInt32(r["ProbationPeriodValue"]),
                    ProbationPeriodUnit = r["ProbationPeriodUnit"] == DBNull.Value? null: r["ProbationPeriodUnit"].ToString(),
                    ProbationStartDate = r["ProbationStartDate"] == DBNull.Value? null: Convert.ToDateTime(r["ProbationStartDate"]),
                    ProbationEndDate = r["ProbationEndDate"] == DBNull.Value? null: Convert.ToDateTime(r["ProbationEndDate"]),
                    ProbationStatusId = r["ProbationStatusId"] == DBNull.Value? null: Convert.ToInt32(r["ProbationStatusId"]),
                    IsCurrent = r["IsCurrent"] == DBNull.Value? null: Convert.ToBoolean(r["IsCurrent"]),
                    PreviousProbationId = r["PreviousProbationId"] == DBNull.Value? null: Convert.ToInt32(r["PreviousProbationId"]),
                    ExtensionReason = r["ExtensionReason"] == DBNull.Value? null: r["ExtensionReason"].ToString(),
                    ProbationConfirmationDate =r["ProbationConfirmationDate"] == DBNull.Value? null: Convert.ToDateTime(r["ProbationConfirmationDate"]),
                    ConfirmedBy = r["ConfirmedBy"] == DBNull.Value? null: Convert.ToInt32(r["ConfirmedBy"]),
                    ConfirmedByName = r["ConfirmedByName"] == DBNull.Value? null: r["ConfirmedByName"].ToString(),
                    ConfirmationRemark = r["ConfirmationRemark"] == DBNull.Value? null: r["ConfirmationRemark"].ToString(),
                    ConfirmationDocument =r["EmployeeDocumentId"] == DBNull.Value? null
                    : new EmployeeDocumentInfo
                    {
                        EmployeeDocumentId =Convert.ToInt32(r["EmployeeDocumentId"]),
                        Ref_EmployeeId =r["DocumentEmployeeId"] == DBNull.Value? 0: Convert.ToInt32(r["DocumentEmployeeId"]),
                        Ref_EmployeeProbationId =r["DocumentProbationId"] == DBNull.Value? null: Convert.ToInt32(r["DocumentProbationId"]),
                        DocumentCategory =r["DocumentCategory"] == DBNull.Value? null: r["DocumentCategory"].ToString(),
                        DocumentType =r["DocumentType"] == DBNull.Value? null: r["DocumentType"].ToString(),
                        FileName =r["FileName"] == DBNull.Value? null: r["FileName"].ToString(),
                        FileType =r["FileType"] == DBNull.Value? null: r["FileType"].ToString(),
                        FileSize =r["FileSize"] == DBNull.Value? null: Convert.ToInt64(r["FileSize"]),
                        FileUrl =r["FilePath"] == DBNull.Value? null: BuildFileUrl(r["FilePath"].ToString()),
                        DocumentDate =r["DocumentDate"] == DBNull.Value? null: Convert.ToDateTime(r["DocumentDate"]),
                        EffectiveFrom =r["EffectiveFrom"] == DBNull.Value? null: Convert.ToDateTime(r["EffectiveFrom"]),
                        EffectiveTo =r["EffectiveTo"] == DBNull.Value? null: Convert.ToDateTime(r["EffectiveTo"]),
                        CreatedOn =r["DocumentCreatedOn"] == DBNull.Value? null: Convert.ToDateTime(r["DocumentCreatedOn"])
                    }
                };

                return new ApiResponse
                {
                    Status = true,
                    Message = "Employee probation details retrieved successfully.",
                    Data = result
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,"Database error while getting probation details for EmployeeId {EmployeeId}.",employeeId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Error while getting probation details for EmployeeId {EmployeeId}.",employeeId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region GET PROBATION HISTORY

        public async Task<ApiResponse> GetProbationHistory(int employeeId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status","GET_PROBATION_HISTORY"),
                    new SqlParameter("@EmployeeId",employeeId)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = true,
                        Message = "No probation history found.",
                        Data = new List<EmployeeProbationHistory>()
                    };
                }

                var history = dt.Rows.Cast<DataRow>()
                    .Select(r => new EmployeeProbationHistory
                    {
                        EmployeeProbationId =Convert.ToInt32(r["EmployeeProbationId"]),
                        EmployeeId =Convert.ToInt32(r["EmployeeId"]),
                        ProbationPeriodValue =Convert.ToInt32(r["ProbationPeriodValue"]),
                        ProbationPeriodUnit =r["ProbationPeriodUnit"] == DBNull.Value? null: r["ProbationPeriodUnit"].ToString(),
                        ProbationStartDate =Convert.ToDateTime(r["ProbationStartDate"]),
                        ProbationEndDate =Convert.ToDateTime(r["ProbationEndDate"]),
                        ProbationStatusId =Convert.ToInt32(r["ProbationStatusId"]),
                        StatusCode =r["StatusCode"] == DBNull.Value? null: r["StatusCode"].ToString(),
                        StatusName =r["StatusName"] == DBNull.Value? null: r["StatusName"].ToString(),
                        IsCurrent =Convert.ToBoolean(r["IsCurrent"]),
                        PreviousProbationId =r["PreviousProbationId"] == DBNull.Value? null: Convert.ToInt32(r["PreviousProbationId"]),
                        ExtensionReason =r["ExtensionReason"] == DBNull.Value? null: r["ExtensionReason"].ToString(),
                        FailureReason =r["FailureReason"] == DBNull.Value? null: r["FailureReason"].ToString(),
                        CancellationReason =r["CancellationReason"] == DBNull.Value? null: r["CancellationReason"].ToString(),
                        ConfirmationDate =r["ConfirmationDate"] == DBNull.Value? null: Convert.ToDateTime(r["ConfirmationDate"]),
                        ConfirmedBy =r["ConfirmedBy"] == DBNull.Value? null: Convert.ToInt32(r["ConfirmedBy"]),
                        ConfirmationRemark =r["ConfirmationRemark"] == DBNull.Value? null: r["ConfirmationRemark"].ToString(),
                        CreatedOn =r["CreatedOn"] == DBNull.Value? null: Convert.ToDateTime(r["CreatedOn"]),
                        CreatedBy =r["CreatedBy"] == DBNull.Value? null: Convert.ToInt32(r["CreatedBy"]),
                        UpdatedOn =r["UpdatedOn"] == DBNull.Value? null: Convert.ToDateTime(r["UpdatedOn"]),
                        UpdatedBy =r["UpdatedBy"] == DBNull.Value? null: Convert.ToInt32(r["UpdatedBy"])
                    }).ToList();

                return new ApiResponse
                {
                    Status = true,
                    Message = "Probation history retrieved successfully.",
                    Data = history
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,"Database error while getting probation history for EmployeeId {EmployeeId}.",employeeId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Error while getting probation history for EmployeeId {EmployeeId}.",employeeId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        #endregion

        #region EXTEND PROBATION
        public async Task<ApiResponse> ExtendProbation(EmployeeProbationManagementRequest model,int employeeId)
        {
            try
            {
                if (model == null)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Request is required."
                    };
                }

                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status","EXTEND_PROBATION"),
                    new SqlParameter("@EmployeeId",model.EmployeeId ?? (object)DBNull.Value),
                    new SqlParameter("@ProbationPeriodValue",model.ProbationPeriodValue ?? (object)DBNull.Value),
                    new SqlParameter("@ProbationPeriodUnit",string.IsNullOrWhiteSpace(model.ProbationPeriodUnit)? (object)DBNull.Value: model.ProbationPeriodUnit),
                    new SqlParameter("@ExtensionReason",string.IsNullOrWhiteSpace(model.ExtensionReason)? (object)DBNull.Value: model.ExtensionReason),
                    new SqlParameter("@CreatedBy",employeeId),
                    new SqlParameter("@UpdatedBy",employeeId)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Probation extension failed."
                    };
                }

                var r = dt.Rows[0];

                var result =
                    new EmployeeProbationOperationResponse
                    {
                        EmployeeProbationId =Convert.ToInt32(r["EmployeeProbationId"]),
                        EmployeeId =Convert.ToInt32(r["EmployeeId"]),
                        ProbationPeriodValue =r["ProbationPeriodValue"] == DBNull.Value? null: Convert.ToInt32(r["ProbationPeriodValue"]),
                        ProbationPeriodUnit =r["ProbationPeriodUnit"] == DBNull.Value? null: r["ProbationPeriodUnit"].ToString(),
                        ProbationStartDate =r["ProbationStartDate"] == DBNull.Value? null: Convert.ToDateTime(r["ProbationStartDate"]),
                        ProbationEndDate =r["ProbationEndDate"] == DBNull.Value? null: Convert.ToDateTime(r["ProbationEndDate"]),
                        ProbationStatusId =r["ProbationStatusId"] == DBNull.Value? null: Convert.ToInt32(r["ProbationStatusId"]),
                        StatusCode =r["StatusCode"] == DBNull.Value? null: r["StatusCode"].ToString(),
                        StatusName =r["StatusName"] == DBNull.Value? null: r["StatusName"].ToString(),
                        IsCurrent =r["IsCurrent"] == DBNull.Value? false: Convert.ToBoolean(r["IsCurrent"]),
                        PreviousProbationId =r["PreviousProbationId"] == DBNull.Value? null: Convert.ToInt32(r["PreviousProbationId"]),
                        ExtensionReason =r["ExtensionReason"] == DBNull.Value? null: r["ExtensionReason"].ToString(),
                        ConfirmationDate =r["ConfirmationDate"] == DBNull.Value? null: Convert.ToDateTime(r["ConfirmationDate"]),
                        ConfirmedBy =r["ConfirmedBy"] == DBNull.Value? null: Convert.ToInt32(r["ConfirmedBy"]),
                        ConfirmationRemark =r["ConfirmationRemark"] == DBNull.Value? null: r["ConfirmationRemark"].ToString()
                    };

                return new ApiResponse
                {
                    Status = true,
                    Message = "Probation extended successfully.",
                    Data = result
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while extending probation for EmployeeId {EmployeeId}.",
                    employeeId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while extending probation for EmployeeId {EmployeeId}.",
                    employeeId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region CONFIRM PROBATION
        //public async Task<ApiResponse> ConfirmProbation(EmployeeProbationManagementRequest model,int employeeId)
        //{
        //    try
        //    {
        //        if (model == null)
        //        {
        //            return new ApiResponse
        //            {
        //                Status = false,
        //                Message = "Request is required."
        //            };
        //        }

        //        var dt = await _database.ExecuteDataTableAsync(
        //            SP_NAME,

        //            new SqlParameter(
        //                "@Status",
        //                "CONFIRM_PROBATION"),

        //            new SqlParameter(
        //                "@EmployeeId",
        //                model.EmployeeId ?? (object)DBNull.Value),

        //            new SqlParameter(
        //                "@ConfirmationDate",
        //                model.ConfirmationDate ?? (object)DBNull.Value),

        //            new SqlParameter(
        //                "@ConfirmationRemark",
        //                string.IsNullOrWhiteSpace(
        //                    model.ConfirmationRemark)
        //                    ? (object)DBNull.Value
        //                    : model.ConfirmationRemark),

        //            new SqlParameter(
        //                "@UpdatedBy",
        //                employeeId)
        //        );

        //        if (dt == null || dt.Rows.Count == 0)
        //        {
        //            return new ApiResponse
        //            {
        //                Status = false,
        //                Message = "Employee confirmation failed."
        //            };
        //        }

        //        var r = dt.Rows[0];

        //        var result =
        //            new EmployeeProbationOperationResponse
        //            {
        //                EmployeeProbationId =
        //                    Convert.ToInt32(
        //                        r["EmployeeProbationId"]),

        //                EmployeeId =
        //                    Convert.ToInt32(
        //                        r["EmployeeId"]),

        //                ProbationPeriodValue =
        //                    r["ProbationPeriodValue"] == DBNull.Value
        //                        ? null
        //                        : Convert.ToInt32(
        //                            r["ProbationPeriodValue"]),

        //                ProbationPeriodUnit =
        //                    r["ProbationPeriodUnit"] == DBNull.Value
        //                        ? null
        //                        : r["ProbationPeriodUnit"].ToString(),

        //                ProbationStartDate =
        //                    r["ProbationStartDate"] == DBNull.Value
        //                        ? null
        //                        : Convert.ToDateTime(
        //                            r["ProbationStartDate"]),

        //                ProbationEndDate =
        //                    r["ProbationEndDate"] == DBNull.Value
        //                        ? null
        //                        : Convert.ToDateTime(
        //                            r["ProbationEndDate"]),

        //                ProbationStatusId =
        //                    r["ProbationStatusId"] == DBNull.Value
        //                        ? null
        //                        : Convert.ToInt32(
        //                            r["ProbationStatusId"]),

        //                StatusCode =
        //                    r["StatusCode"] == DBNull.Value
        //                        ? null
        //                        : r["StatusCode"].ToString(),

        //                StatusName =
        //                    r["StatusName"] == DBNull.Value
        //                        ? null
        //                        : r["StatusName"].ToString(),

        //                IsCurrent =
        //                    r["IsCurrent"] == DBNull.Value
        //                        ? false
        //                        : Convert.ToBoolean(
        //                            r["IsCurrent"]),

        //                PreviousProbationId =
        //                    r["PreviousProbationId"] == DBNull.Value
        //                        ? null
        //                        : Convert.ToInt32(
        //                            r["PreviousProbationId"]),

        //                ExtensionReason =
        //                    r["ExtensionReason"] == DBNull.Value
        //                        ? null
        //                        : r["ExtensionReason"].ToString(),

        //                ConfirmationDate =
        //                    r["ConfirmationDate"] == DBNull.Value
        //                        ? null
        //                        : Convert.ToDateTime(
        //                            r["ConfirmationDate"]),

        //                ConfirmedBy =
        //                    r["ConfirmedBy"] == DBNull.Value
        //                        ? null
        //                        : Convert.ToInt32(
        //                            r["ConfirmedBy"]),

        //                ConfirmationRemark =
        //                    r["ConfirmationRemark"] == DBNull.Value
        //                        ? null
        //                        : r["ConfirmationRemark"].ToString()
        //            };

        //        return new ApiResponse
        //        {
        //            Status = true,
        //            Message = "Employee confirmed successfully.",
        //            Data = result
        //        };
        //    }
        //    catch (SqlException ex)
        //    {
        //        _logger.LogError(
        //            ex,
        //            "Database error while confirming probation for EmployeeId {EmployeeId}.",
        //            employeeId);

        //        return new ApiResponse
        //        {
        //            Status = false,
        //            Message = $"Database Error: {ex.Message}"
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(
        //            ex,
        //            "Error while confirming probation for EmployeeId {EmployeeId}.",
        //            employeeId);

        //        return new ApiResponse
        //        {
        //            Status = false,
        //            Message = $"Error: {ex.Message}"
        //        };
        //    }
        //}

        public async Task<ApiResponse> ConfirmProbation(EmployeeProbationManagementRequest model,int employeeId)
        {
            try
            {
                if (model == null)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Request is required."
                    };
                }

                if (!model.EmployeeId.HasValue)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "EmployeeId is required."
                    };
                }

                if (string.IsNullOrWhiteSpace(model.ConfirmationPdfFileName))
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Confirmation PDF file name is required."
                    };
                }

                if (string.IsNullOrWhiteSpace(model.ConfirmationPdfBase64))
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Confirmation PDF is required."
                    };
                }


                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status","CONFIRM_PROBATION"),
                    new SqlParameter("@EmployeeId",model.EmployeeId.Value),
                    new SqlParameter("@ConfirmationDate",model.ConfirmationDate ?? (object)DBNull.Value),
                    new SqlParameter("@ConfirmationRemark",string.IsNullOrWhiteSpace(model.ConfirmationRemark)? (object)DBNull.Value: model.ConfirmationRemark),
                    new SqlParameter("@UpdatedBy",employeeId)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Employee confirmation failed."
                    };
                }


                var r = dt.Rows[0];


                var result =new EmployeeProbationOperationResponse
                    {
                        EmployeeProbationId =Convert.ToInt32(r["EmployeeProbationId"]),
                        EmployeeId =Convert.ToInt32(r["EmployeeId"]),
                        EmployeeName =r["EmployeeName"] == DBNull.Value? null: r["EmployeeName"].ToString(),
                        ProbationPeriodValue =r["ProbationPeriodValue"] == DBNull.Value? null: Convert.ToInt32(r["ProbationPeriodValue"]),
                        ProbationPeriodUnit =r["ProbationPeriodUnit"] == DBNull.Value? null: r["ProbationPeriodUnit"].ToString(),
                        ProbationStartDate =r["ProbationStartDate"] == DBNull.Value? null: Convert.ToDateTime(r["ProbationStartDate"]),
                        ProbationEndDate =r["ProbationEndDate"] == DBNull.Value? null: Convert.ToDateTime(r["ProbationEndDate"]),
                        ProbationStatusId =r["ProbationStatusId"] == DBNull.Value? null: Convert.ToInt32(r["ProbationStatusId"]),
                        StatusCode =r["StatusCode"] == DBNull.Value? null: r["StatusCode"].ToString(),                       
                        StatusName =r["StatusName"] == DBNull.Value? null: r["StatusName"].ToString(),
                        IsCurrent =r["IsCurrent"] == DBNull.Value? false: Convert.ToBoolean(r["IsCurrent"]),
                        PreviousProbationId =r["PreviousProbationId"] == DBNull.Value? null: Convert.ToInt32(r["PreviousProbationId"]),
                        ExtensionReason =r["ExtensionReason"] == DBNull.Value? null: r["ExtensionReason"].ToString(),
                        ConfirmationDate =r["ConfirmationDate"] == DBNull.Value? null: Convert.ToDateTime(r["ConfirmationDate"]),
                        ConfirmedBy =r["ConfirmedBy"] == DBNull.Value? null: Convert.ToInt32(r["ConfirmedBy"]),
                        ConfirmationRemark =r["ConfirmationRemark"] == DBNull.Value? null: r["ConfirmationRemark"].ToString()
                    };
        
                // 2. SAVE CONFIRMATION PDF              

                string base64Data =model.ConfirmationPdfBase64.Contains(",")? model.ConfirmationPdfBase64.Substring(model.ConfirmationPdfBase64.IndexOf(",") + 1): model.ConfirmationPdfBase64;
                byte[] fileBytes;

                try
                {
                    fileBytes = Convert.FromBase64String(base64Data);
                }
                catch (FormatException)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Invalid confirmation PDF."
                    };
                }


                string savedPath;

                try
                {
                    savedPath = SaveBase64Attachment(model.ConfirmationPdfBase64,model.ConfirmationPdfFileName,"EmployeeDocuments");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,"Error saving confirmation PDF for EmployeeId {EmployeeId}.",model.EmployeeId);
                    return new ApiResponse
                    {
                        Status = false,
                        Message ="Employee confirmed, but confirmation PDF could not be saved."
                    };
                }


                string fileType =Path.GetExtension(model.ConfirmationPdfFileName).TrimStart('.').ToLowerInvariant();
                long fileSize = fileBytes.Length;


                // 3. SAVE DOCUMENT METADATA
              
                var documentDt =
                    await _database.ExecuteDataTableAsync(SP_NAME,
                        new SqlParameter("@Status","ADD_EMPLOYEE_DOCUMENT"),
                        new SqlParameter("@EmployeeId",result.EmployeeId),
                        new SqlParameter("@EmployeeProbationId",result.EmployeeProbationId),
                        new SqlParameter("@DocumentCategory","PROBATION"),
                        new SqlParameter("@DocumentType","CONFIRMATION_LETTER"),
                        new SqlParameter("@FileName",model.ConfirmationPdfFileName),
                        new SqlParameter("@FilePath",savedPath),
                        new SqlParameter("@FileType",fileType),
                        new SqlParameter("@FileSize",fileSize),
                        new SqlParameter("@DocumentDate",model.ConfirmationDate ?? (object)DBNull.Value),
                        new SqlParameter("@EffectiveFrom",model.ConfirmationDate ?? (object)DBNull.Value),
                        new SqlParameter("@CreatedBy",employeeId)
                    );

                if (documentDt == null ||
                    documentDt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message ="Employee confirmed and PDF saved, but document record could not be created."
                    };
                }  
                
                // 4. SEND CONFIRMATION EMAIL
                try
                {
                    await _schedulerExecutionService
                        .SendEmployeeEmailNotificationAsync(
                            schedulerJobItemId: null,
                            employeeId: result.EmployeeId,
                            entityId: result.EmployeeId,
                            title: "Probation Confirmation",
                            message:$"Congratulations {result.EmployeeName ?? "Employee"}, " +"your probation has been successfully completed " +"and you are now confirmed as an employee.",
                            entityType: "EMPLOYEE_PROBATION",
                            recipientType: "CONFIRMED_EMPLOYEE",
                            actionType: "PROBATION_CONFIRMED",
                            templateData:
                            new Dictionary<string, string?>
                            {
                                ["EmployeeName"] = result.EmployeeName,
                                ["ConfirmationDate"] = (result.ConfirmationDate ?? DateTime.Today).ToString("dd-MM-yyyy"),
                                ["ConfirmationRemark"] = result.ConfirmationRemark,
                                ["HeaderLabel"] = "HR Notification"
                            }
                        );
                }
                catch (Exception ex)
                {
                    // Email failure should NOT undo successful confirmation.
                    _logger.LogError(
                        ex,
                        "Employee confirmed successfully, but confirmation email failed. EmployeeId {EmployeeId}.",
                        result.EmployeeId);
                }

                // 5. FINAL RESPONSE              

                return new ApiResponse
                {
                    Status = true,
                    Message = "Employee confirmed successfully.",
                    Data = result
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while confirming probation for EmployeeId {EmployeeId}.",
                    employeeId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while confirming probation for EmployeeId {EmployeeId}.",
                    employeeId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region Currently NOT USED - FAIL PROBATION
        public async Task<ApiResponse> FailProbation(EmployeeProbationManagementRequest model,int employeeId)
        {
            try
            {
                var parameters = new[]{
                    new SqlParameter("@Status", "FAIL_PROBATION"),
                    new SqlParameter("@EmployeeId",model.EmployeeId ?? (object)DBNull.Value),
                    new SqlParameter("@EmployeeProbationId",(object?)model.EmployeeProbationId ?? DBNull.Value),
                    new SqlParameter("@FailureReason",(object?)model.FailureReason ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", employeeId),
                    new SqlParameter("@UpdatedBy", employeeId)
                };

                var ds = await _database.ExecuteDataSetAsync(
                    "USP_ORG_EmployeeProbationManagement",
                    parameters);

                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Failed to mark employee probation as failed."
                    };
                }

                var row = ds.Tables[0].Rows[0];

                var result = new EmployeeProbationOperationResponse
                {
                    EmployeeProbationId =
                        Convert.ToInt32(row["EmployeeProbationId"]),

                    EmployeeId =
                        Convert.ToInt32(row["EmployeeId"]),

                    ProbationPeriodValue =
                        row["ProbationPeriodValue"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(row["ProbationPeriodValue"]),

                    ProbationPeriodUnit =
                        row["ProbationPeriodUnit"] == DBNull.Value
                            ? null
                            : row["ProbationPeriodUnit"].ToString(),

                    ProbationStartDate =
                        row["ProbationStartDate"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(row["ProbationStartDate"]),

                    ProbationEndDate =
                        row["ProbationEndDate"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(row["ProbationEndDate"]),

                    ProbationStatusId =
                        row["ProbationStatusId"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(row["ProbationStatusId"]),

                    StatusCode =
                        row["StatusCode"] == DBNull.Value
                            ? null
                            : row["StatusCode"].ToString(),

                    StatusName =
                        row["StatusName"] == DBNull.Value
                            ? null
                            : row["StatusName"].ToString(),

                    IsCurrent =
                        row["IsCurrent"] != DBNull.Value &&
                        Convert.ToBoolean(row["IsCurrent"]),

                    PreviousProbationId =
                        row["PreviousProbationId"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(row["PreviousProbationId"]),

                    FailureReason =
                        model.FailureReason
                };

                return new ApiResponse
                {
                    Status = true,
                    Message = "Employee probation marked as failed successfully.",
                    Data = result
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error while failing probation for EmployeeId {EmployeeId}",
                    employeeId);

                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while failing probation for EmployeeId {EmployeeId}",
                    employeeId);

                return new ApiResponse
                {
                    Status = false,
                    Message = "An error occurred while failing employee probation."
                };
            }
        }
        #endregion

        #region Cancel Probation
        public async Task<ApiResponse> CancelProbation(EmployeeProbationManagementRequest model,int employeeId)
        {
            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@Status", "CANCEL_PROBATION"),
                    new SqlParameter("@EmployeeId",model.EmployeeId ?? (object)DBNull.Value),
                    new SqlParameter("@EmployeeProbationId",(object?)model.EmployeeProbationId ?? DBNull.Value),
                    new SqlParameter("@CancellationReason",(object?)model.CancellationReason ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", employeeId),
                    new SqlParameter("@UpdatedBy", employeeId)
                 };

                var ds = await _database.ExecuteDataSetAsync("USP_ORG_EmployeeProbationManagement",parameters);

                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Failed to cancel employee probation."
                    };
                }

                var row = ds.Tables[0].Rows[0];

                var result = new EmployeeProbationOperationResponse
                {
                    EmployeeProbationId =Convert.ToInt32(row["EmployeeProbationId"]),
                    EmployeeId =Convert.ToInt32(row["EmployeeId"]),
                    ProbationPeriodValue =row["ProbationPeriodValue"] == DBNull.Value? null: Convert.ToInt32(row["ProbationPeriodValue"]),
                    ProbationPeriodUnit =row["ProbationPeriodUnit"] == DBNull.Value? null: row["ProbationPeriodUnit"].ToString(),
                    ProbationStartDate =row["ProbationStartDate"] == DBNull.Value? null: Convert.ToDateTime(row["ProbationStartDate"]),
                    ProbationEndDate =row["ProbationEndDate"] == DBNull.Value? null: Convert.ToDateTime(row["ProbationEndDate"]),
                    ProbationStatusId =row["ProbationStatusId"] == DBNull.Value? null: Convert.ToInt32(row["ProbationStatusId"]),
                    StatusCode =row["StatusCode"] == DBNull.Value? null: row["StatusCode"].ToString(),
                    StatusName =row["StatusName"] == DBNull.Value? null: row["StatusName"].ToString(),
                    IsCurrent =row["IsCurrent"] != DBNull.Value &&Convert.ToBoolean(row["IsCurrent"]),
                    PreviousProbationId =row["PreviousProbationId"] == DBNull.Value? null: Convert.ToInt32(row["PreviousProbationId"]),
                    CancellationReason =model.CancellationReason
                };

                return new ApiResponse
                {
                    Status = true,
                    Message = "Employee probation cancelled successfully.",
                    Data = result
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error while cancelling probation for EmployeeId {EmployeeId}",
                    employeeId);

                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while cancelling probation for EmployeeId {EmployeeId}",
                    employeeId);

                return new ApiResponse
                {
                    Status = false,
                    Message = "An error occurred while cancelling employee probation."
                };
            }
        }
        #endregion
    }
}
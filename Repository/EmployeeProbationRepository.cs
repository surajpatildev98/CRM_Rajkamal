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
        private const string SP_NAME = "USP_ORG_EmployeeProbationManagement";
        public EmployeeProbationRepository(IDatabaseService database,ILogger<EmployeeProbationRepository> logger)
        {
            _database = database;
            _logger = logger;
        }

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
                    RefDesignationId = r["RefDesignationId"] == DBNull.Value? null: Convert.ToInt32(r["RefDesignationId"]), 
                    DesignationId =r["DesignationId"] == DBNull.Value? null: Convert.ToInt32(r["DesignationId"]),
                    DesignationCode =r["DesignationCode"] == DBNull.Value? null: r["DesignationCode"].ToString(),
                    DesignationName =r["DesignationName"] == DBNull.Value? null: r["DesignationName"].ToString(),
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
                    ConfirmationRemark = r["ConfirmationRemark"] == DBNull.Value? null: r["ConfirmationRemark"].ToString()
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
        public async Task<ApiResponse> ConfirmProbation(
            EmployeeProbationManagementRequest model,
            int employeeId)
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

                var dt = await _database.ExecuteDataTableAsync(
                    SP_NAME,

                    new SqlParameter(
                        "@Status",
                        "CONFIRM_PROBATION"),

                    new SqlParameter(
                        "@EmployeeId",
                        model.EmployeeId ?? (object)DBNull.Value),

                    new SqlParameter(
                        "@ConfirmationDate",
                        model.ConfirmationDate ?? (object)DBNull.Value),

                    new SqlParameter(
                        "@ConfirmationRemark",
                        string.IsNullOrWhiteSpace(
                            model.ConfirmationRemark)
                            ? (object)DBNull.Value
                            : model.ConfirmationRemark),

                    new SqlParameter(
                        "@UpdatedBy",
                        employeeId)
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

                var result =
                    new EmployeeProbationOperationResponse
                    {
                        EmployeeProbationId =
                            Convert.ToInt32(
                                r["EmployeeProbationId"]),

                        EmployeeId =
                            Convert.ToInt32(
                                r["EmployeeId"]),

                        ProbationPeriodValue =
                            r["ProbationPeriodValue"] == DBNull.Value
                                ? null
                                : Convert.ToInt32(
                                    r["ProbationPeriodValue"]),

                        ProbationPeriodUnit =
                            r["ProbationPeriodUnit"] == DBNull.Value
                                ? null
                                : r["ProbationPeriodUnit"].ToString(),

                        ProbationStartDate =
                            r["ProbationStartDate"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(
                                    r["ProbationStartDate"]),

                        ProbationEndDate =
                            r["ProbationEndDate"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(
                                    r["ProbationEndDate"]),

                        ProbationStatusId =
                            r["ProbationStatusId"] == DBNull.Value
                                ? null
                                : Convert.ToInt32(
                                    r["ProbationStatusId"]),

                        StatusCode =
                            r["StatusCode"] == DBNull.Value
                                ? null
                                : r["StatusCode"].ToString(),

                        StatusName =
                            r["StatusName"] == DBNull.Value
                                ? null
                                : r["StatusName"].ToString(),

                        IsCurrent =
                            r["IsCurrent"] == DBNull.Value
                                ? false
                                : Convert.ToBoolean(
                                    r["IsCurrent"]),

                        PreviousProbationId =
                            r["PreviousProbationId"] == DBNull.Value
                                ? null
                                : Convert.ToInt32(
                                    r["PreviousProbationId"]),

                        ExtensionReason =
                            r["ExtensionReason"] == DBNull.Value
                                ? null
                                : r["ExtensionReason"].ToString(),

                        ConfirmationDate =
                            r["ConfirmationDate"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(
                                    r["ConfirmationDate"]),

                        ConfirmedBy =
                            r["ConfirmedBy"] == DBNull.Value
                                ? null
                                : Convert.ToInt32(
                                    r["ConfirmedBy"]),

                        ConfirmationRemark =
                            r["ConfirmationRemark"] == DBNull.Value
                                ? null
                                : r["ConfirmationRemark"].ToString()
                    };

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

        public async Task<ApiResponse> FailProbation(EmployeeProbationManagementRequest model,int employeeId)
        {
            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@Status", "FAIL_PROBATION"),
                    new SqlParameter("@EmployeeId", employeeId),
                    new SqlParameter("@EmployeeProbationId",
                        (object?)model.EmployeeProbationId ?? DBNull.Value),

                    new SqlParameter("@FailureReason",
                        (object?)model.FailureReason ?? DBNull.Value),

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

        public async Task<ApiResponse> CancelProbation(EmployeeProbationManagementRequest model,int employeeId)
        {
            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@Status", "CANCEL_PROBATION"),
                    new SqlParameter("@EmployeeId", employeeId),
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
    }
}
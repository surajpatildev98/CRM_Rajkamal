using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CRMWebServices.Repository
{
    public class LeavePolicyRepository : ILeavePolicyRepository
    {
        private readonly IDatabaseService _database;

        public LeavePolicyRepository(IDatabaseService database)
        {
            _database = database;
        }

        //public async Task<ApiResponse> SaveLeavePolicy(LeavePolicyModels.LeavePolicySaveRequest request)
        //{
        //    try
        //    {              

        //        if (request == null)
        //        {
        //            return new ApiResponse
        //            {
        //                Status = false,
        //                Message = "Leave Policy request is required."
        //            };
        //        }

        //        if (string.IsNullOrWhiteSpace(request.PolicyName))
        //        {
        //            return new ApiResponse
        //            {
        //                Status = false,
        //                Message = "Policy Name is required."
        //            };
        //        }

        //        if (!request.Ref_CompanyId.HasValue || request.Ref_CompanyId <= 0)
        //        {
        //            return new ApiResponse
        //            {
        //                Status = false,
        //                Message = "Company is required."
        //            };
        //        }



        //        if (request.Details == null)
        //        {
        //            request.Details =new List<LeavePolicyModels.LeavePolicyDetailRequest>();
        //        }

        //        // Duplicate Leave Type check
        //        var duplicateLeaveTypes = request.Details.GroupBy(x => x.Ref_LeaveTypeId).Where(x => x.Key <= 0 || x.Count() > 1).Select(x => x.Key).ToList();
        //        if (duplicateLeaveTypes.Any())
        //        {
        //            return new ApiResponse
        //            {
        //                Status = false,
        //                Message = "Same Leave Type cannot be added more than once in a policy."
        //            };
        //        }            

        //        var detailTable = new DataTable();

        //        detailTable.Columns.Add("LeavePolicyDetailId",typeof(int));
        //        detailTable.Columns.Add("Ref_LeaveTypeId",typeof(int));
        //        detailTable.Columns.Add("AnnualEntitlement",typeof(decimal));
        //        detailTable.Columns.Add("AccrualType",typeof(string));
        //        detailTable.Columns.Add("AccrualValue",typeof(decimal));
        //        detailTable.Columns.Add("CarryForwardAllowed",typeof(bool));
        //        detailTable.Columns.Add("MaxCarryForward",typeof(decimal));
        //        detailTable.Columns.Add("EncashmentAllowed",typeof(bool));
        //        detailTable.Columns.Add("MaxEncashment",typeof(decimal));
        //        detailTable.Columns.Add("AllowNegativeBalance",typeof(bool));
        //        detailTable.Columns.Add("MaxDaysPerApplication",typeof(decimal));
        //        detailTable.Columns.Add("NoticeDays",typeof(int));
        //        detailTable.Columns.Add("IncludeWeekends",typeof(bool));
        //        detailTable.Columns.Add("IncludeHolidays",typeof(bool));
        //        detailTable.Columns.Add("SandwichRule",typeof(bool));
        //        detailTable.Columns.Add("IsHalfDayAllowed",typeof(bool));
        //        detailTable.Columns.Add("RequiresDocument",typeof(bool));
        //        detailTable.Columns.Add("IsActive",typeof(bool));
        //        detailTable.Columns.Add("IsHourlyAllowed",typeof(bool));

        //        // ADD DETAIL RECORDS
        //        foreach (var detail in request.Details)
        //        {
        //            if (detail.Ref_LeaveTypeId <= 0)
        //            {
        //                return new ApiResponse
        //                {
        //                    Status = false,
        //                    Message = "Invalid Leave Type selected."
        //                };
        //            }

        //            if (detail.AnnualEntitlement < 0)
        //            {
        //                return new ApiResponse
        //                {
        //                    Status = false,
        //                    Message =$"Annual Entitlement cannot be negative for Leave Type Id {detail.Ref_LeaveTypeId}."
        //                };
        //            }

        //            if (detail.AccrualValue < 0)
        //            {
        //                return new ApiResponse
        //                {
        //                    Status = false,
        //                    Message =$"Accrual Value cannot be negative for Leave Type Id {detail.Ref_LeaveTypeId}."
        //                };
        //            }

        //            if (detail.MaxCarryForward < 0)
        //            {
        //                return new ApiResponse
        //                {
        //                    Status = false,
        //                    Message =$"Max Carry Forward cannot be negative for Leave Type Id {detail.Ref_LeaveTypeId}."
        //                };
        //            }

        //            if (detail.MaxEncashment < 0)
        //            {
        //                return new ApiResponse
        //                {
        //                    Status = false,
        //                    Message =$"Max Encashment cannot be negative for Leave Type Id {detail.Ref_LeaveTypeId}."
        //                };
        //            }

        //            if (detail.MaxDaysPerApplication < 0)
        //            {
        //                return new ApiResponse
        //                {
        //                    Status = false,
        //                    Message =$"Max Days Per Application cannot be negative for Leave Type Id {detail.Ref_LeaveTypeId}."
        //                };
        //            }

        //            if (detail.NoticeDays < 0)
        //            {
        //                return new ApiResponse
        //                {
        //                    Status = false,
        //                    Message =$"Notice Days cannot be negative for Leave Type Id {detail.Ref_LeaveTypeId}."
        //                };
        //            }

        //            if (string.IsNullOrWhiteSpace(detail.AccrualType))
        //            {
        //                return new ApiResponse
        //                {
        //                    Status = false,
        //                    Message =$"Accrual Type is required for Leave Type Id {detail.Ref_LeaveTypeId}."
        //                };
        //            }


        //            detailTable.Rows.Add(detail.LeavePolicyDetailId.HasValue && detail.LeavePolicyDetailId.Value > 0 ? detail.LeavePolicyDetailId.Value: DBNull.Value,

        //                detail.Ref_LeaveTypeId,
        //                detail.AnnualEntitlement,
        //                detail.AccrualType.Trim(),
        //                detail.AccrualValue,
        //                detail.CarryForwardAllowed,
        //                detail.MaxCarryForward,
        //                detail.EncashmentAllowed,
        //                detail.MaxEncashment,
        //                detail.AllowNegativeBalance,
        //                detail.MaxDaysPerApplication,
        //                detail.NoticeDays,
        //                detail.IncludeWeekends,
        //                detail.IncludeHolidays,
        //                detail.SandwichRule,
        //                detail.IsHalfDayAllowed,
        //                detail.RequiresDocument,
        //                detail.IsActive,
        //                detail.IsHourlyAllowed
        //            );
        //        }              

        //        var dt = await _database.ExecuteDataTableAsync("USP_ORG_LeavePolicyManagement",

        //            new SqlParameter("@Status","SAVE_LEAVE_POLICY"),
        //            new SqlParameter("@LeavePolicyId",request.LeavePolicyId.HasValue &&request.LeavePolicyId.Value > 0? request.LeavePolicyId.Value: DBNull.Value),
        //            new SqlParameter("@PolicyName",request.PolicyName.Trim()),
        //            new SqlParameter("@Ref_CompanyId",request.Ref_CompanyId.Value),
        //            new SqlParameter("@IsActive",request.IsActive),                   
        //            new SqlParameter("@LeavePolicyDetails", detailTable)
        //            {
        //                SqlDbType = SqlDbType.Structured,
        //                TypeName = "dbo.ORG_LeavePolicyDetailType"
        //            }
        //        );

        //        // DATABASE RESPONSE VALIDATION           
        //        if (dt == null || dt.Rows.Count == 0)
        //        {
        //            return new ApiResponse
        //            {
        //                Status = false,
        //                Message = "No response returned from database."
        //            };
        //        }


        //        var row = dt.Rows[0];
        //        bool status =row.Table.Columns.Contains("Status") &&row["Status"] != DBNull.Value &&Convert.ToInt32(row["Status"]) == 1;
        //        string message =row.Table.Columns.Contains("Message") &&row["Message"] != DBNull.Value? row["Message"]?.ToString() ?? "": "No message returned from database.";

        //        if (status)
        //        {
        //            int? leavePolicyId = null;

        //            if (row.Table.Columns.Contains("LeavePolicyId") && row["LeavePolicyId"] != DBNull.Value)
        //            {
        //                leavePolicyId =Convert.ToInt32(row["LeavePolicyId"]);
        //            }

        //            return new ApiResponse
        //            {
        //                Status = true,
        //                Message = message,
        //                Data = new
        //                {
        //                    LeavePolicyId = leavePolicyId
        //                }
        //            };
        //        }

        //        // DATABASE BUSINESS VALIDATION FAILURE             

        //        return new ApiResponse
        //        {
        //            Status = false,
        //            Message = message
        //        };
        //    }
        //    catch (SqlException ex)
        //    {
        //        return new ApiResponse
        //        {
        //            Status = false,
        //            Message = $"Database Error: {ex.Message}"
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse
        //        {
        //            Status = false,
        //            Message = $"Error: {ex.Message}"
        //        };
        //    }
        //}

        // GET LEAVE POLICY LIST    

        public async Task<ApiResponse> SaveLeavePolicy(LeavePolicyModels.LeavePolicySaveRequest request,int createdBy)
        {
            try
            {
                if (request == null)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Request is required."
                    };
                }

                if (string.IsNullOrWhiteSpace(request.PolicyName))
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Policy Name is required."
                    };
                }

                if (!request.Ref_CompanyId.HasValue || request.Ref_CompanyId.Value <= 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Company is required."
                    };
                }

                if (createdBy <= 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Logged in employee is required."
                    };
                }

                request.Details ??= new List<LeavePolicyModels.LeavePolicyDetailRequest>();

                /* =====================================================
                   DUPLICATE LEAVE TYPE CHECK
                   ===================================================== */

                var duplicateLeaveType = request.Details
                    .GroupBy(x => x.Ref_LeaveTypeId)
                    .FirstOrDefault(g => g.Count() > 1);

                if (duplicateLeaveType != null)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Same Leave Type cannot be added more than once in a policy."
                    };
                }

                /* =====================================================
                   BUILD TVP
                   ===================================================== */

                var detailTable = new DataTable();

                detailTable.Columns.Add("LeavePolicyDetailId", typeof(int));
                detailTable.Columns.Add("Ref_LeaveTypeId", typeof(int));
                detailTable.Columns.Add("AnnualEntitlement", typeof(decimal));
                detailTable.Columns.Add("AccrualType", typeof(string));
                detailTable.Columns.Add("AccrualValue", typeof(decimal));
                detailTable.Columns.Add("CarryForwardAllowed", typeof(bool));
                detailTable.Columns.Add("MaxCarryForward", typeof(decimal));
                detailTable.Columns.Add("EncashmentAllowed", typeof(bool));
                detailTable.Columns.Add("MaxEncashment", typeof(decimal));
                detailTable.Columns.Add("AllowNegativeBalance", typeof(bool));
                detailTable.Columns.Add("MaxDaysPerApplication", typeof(decimal));
                detailTable.Columns.Add("NoticeDays", typeof(int));
                detailTable.Columns.Add("IncludeWeekends", typeof(bool));
                detailTable.Columns.Add("IncludeHolidays", typeof(bool));
                detailTable.Columns.Add("SandwichRule", typeof(bool));
                detailTable.Columns.Add("IsHalfDayAllowed", typeof(bool));
                detailTable.Columns.Add("RequiresDocument", typeof(bool));
                detailTable.Columns.Add("IsActive", typeof(bool));
                detailTable.Columns.Add("IsHourlyAllowed", typeof(bool));

                foreach (var detail in request.Details)
                {
                    if (detail.Ref_LeaveTypeId <= 0)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Valid Leave Type is required."
                        };
                    }

                    if (detail.AnnualEntitlement < 0)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Annual Entitlement cannot be negative."
                        };
                    }

                    if (string.IsNullOrWhiteSpace(detail.AccrualType))
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Accrual Type is required."
                        };
                    }

                    if (detail.AccrualValue < 0)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Accrual Value cannot be negative."
                        };
                    }

                    if (detail.MaxCarryForward < 0)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Max Carry Forward cannot be negative."
                        };
                    }

                    if (detail.MaxEncashment < 0)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Max Encashment cannot be negative."
                        };
                    }

                    if (detail.MaxDaysPerApplication < 0)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Max Days Per Application cannot be negative."
                        };
                    }

                    if (detail.NoticeDays < 0)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Notice Days cannot be negative."
                        };
                    }

                    detailTable.Rows.Add(
                        DBNull.Value,
                        detail.Ref_LeaveTypeId,
                        detail.AnnualEntitlement,
                        detail.AccrualType.Trim(),
                        detail.AccrualValue,
                        detail.CarryForwardAllowed,
                        detail.MaxCarryForward,
                        detail.EncashmentAllowed,
                        detail.MaxEncashment,
                        detail.AllowNegativeBalance,
                        detail.MaxDaysPerApplication,
                        detail.NoticeDays,
                        detail.IncludeWeekends,
                        detail.IncludeHolidays,
                        detail.SandwichRule,
                        detail.IsHalfDayAllowed,
                        detail.RequiresDocument,
                        detail.IsActive,
                        detail.IsHourlyAllowed
                    );
                }

                /* =====================================================
                   DATABASE CALL
                   ===================================================== */

                var dt = await _database.ExecuteDataTableAsync("USP_ORG_LeavePolicyManagement",
                    new SqlParameter("@Status", "SAVE_LEAVE_POLICY"),
                    new SqlParameter("@LeavePolicyId", DBNull.Value),
                    new SqlParameter("@PolicyName", request.PolicyName.Trim()),
                    new SqlParameter("@Ref_CompanyId",request.Ref_CompanyId.Value),
                    new SqlParameter("@IsActive", request.IsActive),
                    new SqlParameter("@CreatedBy", createdBy),
                    new SqlParameter("@UpdatedBy", DBNull.Value),
                    new SqlParameter("@LeavePolicyDetails", detailTable)
                    {
                        SqlDbType = SqlDbType.Structured,
                        TypeName = "dbo.ORG_LeavePolicyDetailType"
                    }
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "No response returned from database."
                    };
                }

                var row = dt.Rows[0];
                var status = row["Status"] != DBNull.Value &&Convert.ToInt32(row["Status"]) == 1;
                var message = row["Message"] != DBNull.Value? row["Message"]?.ToString(): "Unable to save Leave Policy.";

                if (!status)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = message
                    };
                }

                var leavePolicyId =row["LeavePolicyId"] != DBNull.Value? Convert.ToInt32(row["LeavePolicyId"]): 0;

                return new ApiResponse
                {
                    Status = true,
                    Message = message,
                    Data = new
                    {
                        LeavePolicyId = leavePolicyId
                    }
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

        public async Task<ApiResponse> UpdateLeavePolicy(LeavePolicyModels.LeavePolicyUpdateRequest request,int updatedBy)
        {
            try
            {
                if (request == null)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Request is required."
                    };
                }

                if (request.LeavePolicyId <= 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Valid Leave Policy Id is required."
                    };
                }

                if (string.IsNullOrWhiteSpace(request.PolicyName))
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Policy Name is required."
                    };
                }

                if (!request.Ref_CompanyId.HasValue ||
                    request.Ref_CompanyId.Value <= 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Company is required."
                    };
                }

                if (updatedBy <= 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Logged in employee is required."
                    };
                }

                request.Details ??=new List<LeavePolicyModels.LeavePolicyDetailRequest>();

                /* =====================================================
                   DUPLICATE LEAVE TYPE CHECK
                   ===================================================== */

                var duplicateLeaveType = request.Details.GroupBy(x => x.Ref_LeaveTypeId).FirstOrDefault(g => g.Count() > 1);
                if (duplicateLeaveType != null)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Same Leave Type cannot be added more than once in a policy."
                    };
                }

                /* =====================================================
                   BUILD TVP
                   ===================================================== */

                var detailTable = new DataTable();

                detailTable.Columns.Add("LeavePolicyDetailId", typeof(int));
                detailTable.Columns.Add("Ref_LeaveTypeId", typeof(int));
                detailTable.Columns.Add("AnnualEntitlement", typeof(decimal));
                detailTable.Columns.Add("AccrualType", typeof(string));
                detailTable.Columns.Add("AccrualValue", typeof(decimal));
                detailTable.Columns.Add("CarryForwardAllowed", typeof(bool));
                detailTable.Columns.Add("MaxCarryForward", typeof(decimal));
                detailTable.Columns.Add("EncashmentAllowed", typeof(bool));
                detailTable.Columns.Add("MaxEncashment", typeof(decimal));
                detailTable.Columns.Add("AllowNegativeBalance", typeof(bool));
                detailTable.Columns.Add("MaxDaysPerApplication", typeof(decimal));
                detailTable.Columns.Add("NoticeDays", typeof(int));
                detailTable.Columns.Add("IncludeWeekends", typeof(bool));
                detailTable.Columns.Add("IncludeHolidays", typeof(bool));
                detailTable.Columns.Add("SandwichRule", typeof(bool));
                detailTable.Columns.Add("IsHalfDayAllowed", typeof(bool));
                detailTable.Columns.Add("RequiresDocument", typeof(bool));
                detailTable.Columns.Add("IsActive", typeof(bool));
                detailTable.Columns.Add("IsHourlyAllowed", typeof(bool));

                foreach (var detail in request.Details)
                {
                    if (detail.Ref_LeaveTypeId <= 0)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Valid Leave Type is required."
                        };
                    }

                    if (detail.AnnualEntitlement < 0)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Annual Entitlement cannot be negative."
                        };
                    }

                    if (string.IsNullOrWhiteSpace(detail.AccrualType))
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Accrual Type is required."
                        };
                    }

                    if (detail.AccrualValue < 0)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Accrual Value cannot be negative."
                        };
                    }

                    if (detail.MaxCarryForward < 0)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Max Carry Forward cannot be negative."
                        };
                    }

                    if (detail.MaxEncashment < 0)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Max Encashment cannot be negative."
                        };
                    }

                    if (detail.MaxDaysPerApplication < 0)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Max Days Per Application cannot be negative."
                        };
                    }

                    if (detail.NoticeDays < 0)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Notice Days cannot be negative."
                        };
                    }

                    detailTable.Rows.Add
                        (detail.LeavePolicyDetailId.HasValue &&detail.LeavePolicyDetailId.Value > 0? detail.LeavePolicyDetailId.Value: DBNull.Value,
                        detail.Ref_LeaveTypeId,
                        detail.AnnualEntitlement,
                        detail.AccrualType.Trim(),
                        detail.AccrualValue,
                        detail.CarryForwardAllowed,
                        detail.MaxCarryForward,
                        detail.EncashmentAllowed,
                        detail.MaxEncashment,
                        detail.AllowNegativeBalance,
                        detail.MaxDaysPerApplication,
                        detail.NoticeDays,
                        detail.IncludeWeekends,
                        detail.IncludeHolidays,
                        detail.SandwichRule,
                        detail.IsHalfDayAllowed,
                        detail.RequiresDocument,
                        detail.IsActive,
                        detail.IsHourlyAllowed
                    );
                }

                /* =====================================================
                   DATABASE CALL
                   ===================================================== */

                var dt = await _database.ExecuteDataTableAsync("USP_ORG_LeavePolicyManagement",
                    new SqlParameter("@Status", "UPDATE_LEAVE_POLICY"),
                    new SqlParameter("@LeavePolicyId",request.LeavePolicyId),
                    new SqlParameter("@PolicyName",request.PolicyName.Trim()),
                    new SqlParameter("@Ref_CompanyId",request.Ref_CompanyId.Value),
                    new SqlParameter("@IsActive",request.IsActive),
                    new SqlParameter("@CreatedBy",DBNull.Value),
                    new SqlParameter("@UpdatedBy",updatedBy),
                    new SqlParameter("@LeavePolicyDetails",detailTable)
                    {
                        SqlDbType = SqlDbType.Structured,
                        TypeName = "dbo.ORG_LeavePolicyDetailType"
                    }
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "No response returned from database."
                    };
                }

                var row = dt.Rows[0];
                var status = row["Status"] != DBNull.Value && Convert.ToInt32(row["Status"]) == 1;
                var message = row["Message"] != DBNull.Value ? row["Message"]?.ToString(): "Unable to update Leave Policy.";

                if (!status)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = message
                    };
                }

                var leavePolicyId =row["LeavePolicyId"] != DBNull.Value? Convert.ToInt32(row["LeavePolicyId"]): request.LeavePolicyId;

                return new ApiResponse
                {
                    Status = true,
                    Message = message,
                    Data = new
                    {
                        LeavePolicyId = leavePolicyId
                    }
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
        public async Task<ApiResponse> GetLeavePolicyList(LeavePolicyModels.LeavePolicyListRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_LeavePolicyManagement",

                    new SqlParameter(
                        "@Status",
                        "GET_LEAVE_POLICY_LIST"),

                    new SqlParameter(
                        "@SearchText",
                        (object?)request.SearchText ?? DBNull.Value),

                    new SqlParameter(
                        "@FilterCompanyId",
                        (object?)request.FilterCompanyId ?? DBNull.Value),

                    new SqlParameter(
                        "@FilterIsActive",
                        (object?)request.FilterIsActive ?? DBNull.Value)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = true,
                        Message = "No leave policies found.",
                        Data = new List<
                            LeavePolicyModels.LeavePolicyListResponse>()
                    };
                }

                var result =
                    new List<LeavePolicyModels.LeavePolicyListResponse>();

                foreach (DataRow row in dt.Rows)
                {
                    result.Add(
                        new LeavePolicyModels.LeavePolicyListResponse
                        {
                            LeavePolicyId =
                                Convert.ToInt32(row["LeavePolicyId"]),

                            PolicyName =
                                row["PolicyName"]?.ToString(),

                            Ref_CompanyId =
                                Convert.ToInt32(row["Ref_CompanyId"]),

                            IsActive =
                                Convert.ToBoolean(row["IsActive"]),

                            LeaveTypeCount =
                                Convert.ToInt32(row["LeaveTypeCount"]),

                            CreatedOn =
                                Convert.ToDateTime(row["CreatedOn"]),

                            CreatedBy =
                                row["CreatedBy"] == DBNull.Value
                                    ? null
                                    : Convert.ToInt32(row["CreatedBy"]),

                            UpdatedOn =
                                row["UpdatedOn"] == DBNull.Value
                                    ? null
                                    : Convert.ToDateTime(row["UpdatedOn"]),

                            UpdatedBy =
                                row["UpdatedBy"] == DBNull.Value
                                    ? null
                                    : Convert.ToInt32(row["UpdatedBy"])
                        });
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Leave policies fetched successfully.",
                    Data = result
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

        // GET LEAVE POLICY DETAILS    
        public async Task<ApiResponse> GetLeavePolicyDetails(int leavePolicyId)
        {
            try
            {         
                if (leavePolicyId <= 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Valid Leave Policy Id is required."
                    };
                }

                var ds = await _database.ExecuteDataSetAsync("USP_ORG_LeavePolicyManagement",

                    new SqlParameter("@Status","GET_LEAVE_POLICY_DETAILS"),
                    new SqlParameter("@LeavePolicyId",leavePolicyId)
                );


                if (ds == null ||
                    ds.Tables.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "No response returned from database."
                    };
                }


                if (ds.Tables[0].Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Leave Policy not found."
                    };
                }


                var policyRow = ds.Tables[0].Rows[0];

                var policy = new LeavePolicyModels.LeavePolicyInfo
                {
                    LeavePolicyId =policyRow["LeavePolicyId"] != DBNull.Value? Convert.ToInt32(policyRow["LeavePolicyId"]): 0,
                    PolicyName =policyRow["PolicyName"] != DBNull.Value? policyRow["PolicyName"]?.ToString(): null,
                    Ref_CompanyId =policyRow["Ref_CompanyId"] != DBNull.Value? Convert.ToInt32(policyRow["Ref_CompanyId"]): 0,
                    IsActive =policyRow["IsActive"] != DBNull.Value && Convert.ToBoolean(policyRow["IsActive"]),
                    CreatedOn =policyRow["CreatedOn"] != DBNull.Value? Convert.ToDateTime(policyRow["CreatedOn"]): DateTime.MinValue,
                    CreatedBy =policyRow["CreatedBy"] != DBNull.Value? Convert.ToInt32(policyRow["CreatedBy"]): null,
                    UpdatedOn =policyRow["UpdatedOn"] != DBNull.Value? Convert.ToDateTime(policyRow["UpdatedOn"]): null,
                    UpdatedBy =policyRow["UpdatedBy"] != DBNull.Value? Convert.ToInt32(policyRow["UpdatedBy"]): null
                };

                var details =new List<LeavePolicyModels.LeavePolicyDetailInfo>();
                if (ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[1].Rows)
                    {
                        details.Add(new LeavePolicyModels.LeavePolicyDetailInfo
                            {
                                LeavePolicyDetailId =row["LeavePolicyDetailId"] != DBNull.Value? Convert.ToInt32(row["LeavePolicyDetailId"]): 0,
                                Ref_LeavePolicyId =row["Ref_LeavePolicyId"] != DBNull.Value? Convert.ToInt32(row["Ref_LeavePolicyId"]): 0,
                                Ref_LeaveTypeId =row["Ref_LeaveTypeId"] != DBNull.Value? Convert.ToInt32(row["Ref_LeaveTypeId"]): 0,
                                LeaveCode =row["LeaveCode"] != DBNull.Value? row["LeaveCode"]?.ToString(): null,
                                LeaveName =row["LeaveName"] != DBNull.Value? row["LeaveName"]?.ToString(): null,
                                AnnualEntitlement =row["AnnualEntitlement"] != DBNull.Value? Convert.ToDecimal(row["AnnualEntitlement"]): 0,
                                AccrualType =row["AccrualType"] != DBNull.Value? row["AccrualType"]?.ToString(): null,
                                AccrualValue =row["AccrualValue"] != DBNull.Value? Convert.ToDecimal(row["AccrualValue"]): 0,
                                CarryForwardAllowed =row["CarryForwardAllowed"] != DBNull.Value &&Convert.ToBoolean(row["CarryForwardAllowed"]),
                                MaxCarryForward =row["MaxCarryForward"] != DBNull.Value? Convert.ToDecimal(row["MaxCarryForward"]): 0,
                                EncashmentAllowed =row["EncashmentAllowed"] != DBNull.Value &&Convert.ToBoolean(row["EncashmentAllowed"]),
                                MaxEncashment =row["MaxEncashment"] != DBNull.Value? Convert.ToDecimal(row["MaxEncashment"]): 0,
                                AllowNegativeBalance =row["AllowNegativeBalance"] != DBNull.Value &&Convert.ToBoolean(row["AllowNegativeBalance"]),
                                MaxDaysPerApplication =row["MaxDaysPerApplication"] != DBNull.Value? Convert.ToDecimal(row["MaxDaysPerApplication"]): 0,
                                NoticeDays =row["NoticeDays"] != DBNull.Value? Convert.ToInt32(row["NoticeDays"]): 0,
                                IncludeWeekends =row["IncludeWeekends"] != DBNull.Value &&Convert.ToBoolean(row["IncludeWeekends"]),
                                IncludeHolidays =row["IncludeHolidays"] != DBNull.Value &&Convert.ToBoolean(row["IncludeHolidays"]),
                                SandwichRule =row["SandwichRule"] != DBNull.Value &&Convert.ToBoolean(row["SandwichRule"]),
                                IsHalfDayAllowed =row["IsHalfDayAllowed"] != DBNull.Value &&Convert.ToBoolean(row["IsHalfDayAllowed"]),
                                RequiresDocument =row["RequiresDocument"] != DBNull.Value &&Convert.ToBoolean(row["RequiresDocument"]),
                                IsActive =row["IsActive"] != DBNull.Value &&Convert.ToBoolean(row["IsActive"]),
                                IsHourlyAllowed =row["IsHourlyAllowed"] != DBNull.Value &&Convert.ToBoolean(row["IsHourlyAllowed"]),
                                CreatedOn =row["CreatedOn"] != DBNull.Value? Convert.ToDateTime(row["CreatedOn"]): DateTime.MinValue,
                                CreatedBy =row["CreatedBy"] != DBNull.Value? Convert.ToInt32(row["CreatedBy"]): null,
                                UpdatedOn =row["UpdatedOn"] != DBNull.Value? Convert.ToDateTime(row["UpdatedOn"]): null,
                                UpdatedBy =row["UpdatedBy"] != DBNull.Value? Convert.ToInt32(row["UpdatedBy"]): null
                            });
                    }
                }


                var responseData =new LeavePolicyModels.LeavePolicyDetailsResponse
                    {
                        Policy = policy,
                        Details = details
                    };


                return new ApiResponse
                {
                    Status = true,
                    Message = "Leave Policy details fetched successfully.",
                    Data = responseData
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

        // GET LEAVE TYPES   
        public async Task<ApiResponse> GetLeaveTypes()
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_ORG_LeavePolicyManagement",

                    new SqlParameter(
                        "@Status",
                        "GET_LEAVE_TYPES")
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = true,
                        Message = "No leave types found.",
                        Data = new List<
                            LeavePolicyModels.LeaveTypeDropdown>()
                    };
                }

                var result =
                    new List<LeavePolicyModels.LeaveTypeDropdown>();

                foreach (DataRow row in dt.Rows)
                {
                    result.Add(
                        new LeavePolicyModels.LeaveTypeDropdown
                        {
                            LeaveTypeId =
                                Convert.ToInt32(row["LeaveTypeId"]),

                            LeaveCode =
                                row["LeaveCode"]?.ToString(),

                            LeaveName =
                                row["LeaveName"]?.ToString(),

                            IsPaid =
                                Convert.ToBoolean(row["IsPaid"]),

                            IsActive =
                                Convert.ToBoolean(row["IsActive"]),

                            IsHourlyAllowed =
                                Convert.ToBoolean(row["IsHourlyAllowed"])
                        });
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Leave types fetched successfully.",
                    Data = result
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
using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Services;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using static CRMWebServices.Models.SchedulerModels;

namespace CRMWebServices.Repository
{
    public class SchedulerRepository : ISchedulerRepository
    {
        private readonly IDatabaseService _database;

        private const string SP_NAME = "USP_CRM_SchedulerManagement";

        public SchedulerRepository(IDatabaseService database)
        {
            _database = database;
        }

        #region GET SCHEDULER ENTITY TYPES

        public async Task<ApiResponse> GetSchedulerEntityTypes()
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    SP_NAME,
                    new SqlParameter("@Status", "GET_SCHEDULER_ENTITY_TYPES")
                );

                var result = new List<SchedulerEntityTypeResponse>();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        result.Add(new SchedulerEntityTypeResponse
                        {
                            EntityType = row["EntityType"] == DBNull.Value
                                ? string.Empty
                                : row["EntityType"].ToString()!
                        });
                    }
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = result.Count > 0
                        ? "Scheduler entity types fetched successfully."
                        : "No scheduler entity types found.",
                    Data = result
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }

        #endregion

        #region GET SCHEDULER SCOPES

        public async Task<ApiResponse> GetSchedulerScopes(string entityType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entityType))
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "EntityType is required."
                    };
                }

                var dt = await _database.ExecuteDataTableAsync(
                    SP_NAME,

                    new SqlParameter(
                        "@Status",
                        "GET_SCHEDULER_SCOPES"
                    ),

                    new SqlParameter(
                        "@EntityType",
                        entityType
                    )
                );

                var result = new List<SchedulerScopeResponse>();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        result.Add(new SchedulerScopeResponse
                        {
                            ScopeCode = row["ScopeCode"] == DBNull.Value
                                ? string.Empty
                                : row["ScopeCode"].ToString()!,

                            ScopeName = row["ScopeName"] == DBNull.Value
                                ? string.Empty
                                : row["ScopeName"].ToString()!
                        });
                    }
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = result.Count > 0
                        ? "Scheduler scopes fetched successfully."
                        : "No scheduler scopes found for the selected entity.",
                    Data = result
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }

        #endregion

        #region GET JOBS

        public async Task<ApiResponse> GetJobs()
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    SP_NAME,
                    new SqlParameter("@Status", "GET_JOBS")
                );

                var result = new List<SchedulerJobListResponse>();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        result.Add(MapJobListRow(row));
                    }
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = result.Count > 0
                        ? "Scheduler jobs fetched successfully."
                        : "No scheduler jobs found.",
                    Data = result
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }

        #endregion

        #region GET JOB

        public async Task<ApiResponse> GetJob(int schedulerJobId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    SP_NAME,

                    new SqlParameter(
                        "@Status",
                        "GET_JOB"
                    ),

                    new SqlParameter(
                        "@SchedulerJobId",
                        schedulerJobId
                    )
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Scheduler job not found."
                    };
                }

                var result = MapJobDetailsRow(dt.Rows[0]);

                return new ApiResponse
                {
                    Status = true,
                    Message = "Scheduler job fetched successfully.",
                    Data = result
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }

        #endregion

        #region CREATE JOB

        public async Task<ApiResponse> CreateJob(
            CreateSchedulerJobRequest model,
            int createdBy)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    SP_NAME,

                    new SqlParameter(
                        "@Status",
                        "CREATE_JOB"
                    ),

                    new SqlParameter(
                        "@JobCode",
                        model.JobCode
                    ),

                    new SqlParameter(
                        "@JobName",
                        model.JobName
                    ),

                    new SqlParameter(
                        "@JobType",
                        model.JobType
                    ),

                    new SqlParameter(
                        "@Description",
                        (object?)model.Description ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@EntityType",
                        model.EntityType
                    ),

                    new SqlParameter(
                        "@ScopeCode",
                        model.ScopeCode
                    ),

                    new SqlParameter(
                        "@IsActive",
                        model.IsActive
                    ),

                    new SqlParameter(
                        "@FrequencyType",
                        model.FrequencyType
                    ),

                    new SqlParameter(
                        "@FrequencyValue",
                        model.FrequencyValue
                    ),

                    new SqlParameter(
                        "@StartDate",
                        (object?)model.StartDate ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@EndDate",
                        (object?)model.EndDate ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@MaxAttempts",
                        (object?)model.MaxAttempts ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@StopAfterMinutes",
                        (object?)model.StopAfterMinutes ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@CreatedBy",
                        createdBy
                    )
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Scheduler job was not created."
                    };
                }

                var row = dt.Rows[0];

                return new ApiResponse
                {
                    Status = true,
                    Message = "Scheduler job created successfully.",
                    Data = MapJobDetailsRow(row)
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }

        #endregion

        #region UPDATE JOB

        public async Task<ApiResponse> UpdateJob(
            UpdateSchedulerJobRequest model,
            int updatedBy)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    SP_NAME,

                    new SqlParameter(
                        "@Status",
                        "UPDATE_JOB"
                    ),

                    new SqlParameter(
                        "@SchedulerJobId",
                        model.SchedulerJobId
                    ),

                    new SqlParameter(
                        "@JobName",
                        model.JobName
                    ),

                    new SqlParameter(
                        "@JobType",
                        model.JobType
                    ),

                    new SqlParameter(
                        "@Description",
                        (object?)model.Description ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@EntityType",
                        model.EntityType
                    ),

                    new SqlParameter(
                        "@ScopeCode",
                        model.ScopeCode
                    ),

                    new SqlParameter(
                        "@IsActive",
                        (object?)model.IsActive ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@FrequencyType",
                        model.FrequencyType
                    ),

                    new SqlParameter(
                        "@FrequencyValue",
                        model.FrequencyValue
                    ),

                    new SqlParameter(
                        "@StartDate",
                        (object?)model.StartDate ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@EndDate",
                        (object?)model.EndDate ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@MaxAttempts",
                        (object?)model.MaxAttempts ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@StopAfterMinutes",
                        (object?)model.StopAfterMinutes ?? DBNull.Value
                    ),

                    new SqlParameter(
                        "@UpdatedBy",
                        updatedBy
                    )
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Scheduler job was not updated."
                    };
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Scheduler job updated successfully.",
                    Data = MapJobDetailsRow(dt.Rows[0])
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }

        #endregion

        #region ENABLE JOB

        public async Task<ApiResponse> EnableJob(
            int schedulerJobId,
            int updatedBy)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    SP_NAME,

                    new SqlParameter(
                        "@Status",
                        "ENABLE_JOB"
                    ),

                    new SqlParameter(
                        "@SchedulerJobId",
                        schedulerJobId
                    ),

                    new SqlParameter(
                        "@UpdatedBy",
                        updatedBy
                    )
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Scheduler job could not be enabled."
                    };
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Scheduler job enabled successfully.",
                    Data = MapJobDetailsRow(dt.Rows[0])
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }

        #endregion

        #region DISABLE JOB

        public async Task<ApiResponse> DisableJob(
            int schedulerJobId,
            int updatedBy)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    SP_NAME,

                    new SqlParameter(
                        "@Status",
                        "DISABLE_JOB"
                    ),

                    new SqlParameter(
                        "@SchedulerJobId",
                        schedulerJobId
                    ),

                    new SqlParameter(
                        "@UpdatedBy",
                        updatedBy
                    )
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Scheduler job could not be disabled."
                    };
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Scheduler job disabled successfully.",
                    Data = MapJobDetailsRow(dt.Rows[0])
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }

        #endregion

        #region HELPER - MAP JOB LIST

        private SchedulerJobListResponse MapJobListRow(
            DataRow row)
        {
            return new SchedulerJobListResponse
            {
                SchedulerJobId =
                    Convert.ToInt32(row["SchedulerJobId"]),

                JobCode =
                    row["JobCode"]?.ToString() ?? string.Empty,

                JobName =
                    row["JobName"]?.ToString() ?? string.Empty,

                JobType =
                    row["JobType"]?.ToString() ?? string.Empty,

                Description =
                    row["Description"] == DBNull.Value
                        ? null
                        : row["Description"]?.ToString(),

                EntityType =
                    row["EntityType"]?.ToString() ?? string.Empty,

                ScopeCode =
                    row["ScopeCode"]?.ToString() ?? string.Empty,

                ScopeName =
                    row["ScopeName"] == DBNull.Value
                        ? null
                        : row["ScopeName"]?.ToString(),

                IsActive =
                    Convert.ToBoolean(row["IsActive"]),

                FrequencyType =
                    row["FrequencyType"]?.ToString() ?? string.Empty,

                FrequencyValue =
                    Convert.ToInt32(row["FrequencyValue"]),

                StartDate =
                    row["StartDate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(row["StartDate"]),

                EndDate =
                    row["EndDate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(row["EndDate"]),

                MaxAttempts =
                    row["MaxAttempts"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(row["MaxAttempts"]),

                StopAfterMinutes =
                    row["StopAfterMinutes"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(row["StopAfterMinutes"]),

                NextRunOn =
                    row["NextRunOn"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(row["NextRunOn"]),

                LastRunOn =
                    row["LastRunOn"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(row["LastRunOn"]),

                IsRunning =
                    Convert.ToBoolean(row["IsRunning"]),

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
            };
        }

        #endregion

        #region HELPER - MAP JOB DETAILS

        private SchedulerJobDetailsResponse MapJobDetailsRow(
            DataRow row)
        {
            return new SchedulerJobDetailsResponse
            {
                SchedulerJobId =
                    Convert.ToInt32(row["SchedulerJobId"]),

                JobCode =
                    row["JobCode"]?.ToString() ?? string.Empty,

                JobName =
                    row["JobName"]?.ToString() ?? string.Empty,

                JobType =
                    row["JobType"]?.ToString() ?? string.Empty,

                Description =
                    row["Description"] == DBNull.Value
                        ? null
                        : row["Description"]?.ToString(),

                EntityType =
                    row["EntityType"]?.ToString() ?? string.Empty,

                ScopeCode =
                    row["ScopeCode"]?.ToString() ?? string.Empty,

                ScopeName =
                    row["ScopeName"] == DBNull.Value
                        ? null
                        : row["ScopeName"]?.ToString(),

                IsActive =
                    Convert.ToBoolean(row["IsActive"]),

                FrequencyType =
                    row["FrequencyType"]?.ToString() ?? string.Empty,

                FrequencyValue =
                    Convert.ToInt32(row["FrequencyValue"]),

                StartDate =
                    row["StartDate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(row["StartDate"]),

                EndDate =
                    row["EndDate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(row["EndDate"]),

                MaxAttempts =
                    row["MaxAttempts"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(row["MaxAttempts"]),

                StopAfterMinutes =
                    row["StopAfterMinutes"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(row["StopAfterMinutes"]),

                NextRunOn =
                    row["NextRunOn"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(row["NextRunOn"]),

                LastRunOn =
                    row["LastRunOn"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(row["LastRunOn"]),

                IsRunning =
                    Convert.ToBoolean(row["IsRunning"]),

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
            };
        }

        #endregion
    }
}
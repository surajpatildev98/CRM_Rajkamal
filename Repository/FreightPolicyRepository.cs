using CRMWebServices.Data;
using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using static CRMWebServices.Models.FreightPolicyModels;

namespace CRMWebServices.Repository
{
    public class FreightPolicyRepository : IFreightPolicyRepository
    {
        private readonly IDatabaseService _database;
        private readonly ILogger<FreightPolicyRepository> _logger;

        private const string SP_NAME = "USP_CRM_FreightPolicyManagement";

        public FreightPolicyRepository(IDatabaseService database, ILogger<FreightPolicyRepository> logger)
        {
            _database = database;
            _logger = logger;
        }

        #region Create Freight Policy
        public async Task<ApiResponse> CreateFreightPolicy(CreateFreightPolicyRequest model, int employeeId)
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

                if (string.IsNullOrWhiteSpace(model.PolicyName))
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Policy name is required."
                    };
                }

                if (model.Slabs == null || model.Slabs.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "At least one freight slab is required."
                    };
                }

                string slabsJson = JsonSerializer.Serialize(
                    model.Slabs.Select(x => new
                    {
                        minOrderValue = x.MinOrderValue,
                        maxOrderValue = x.MaxOrderValue,
                        freightAmount = x.FreightAmount
                    })
                );

                var dt = await _database.ExecuteDataTableAsync(
                    SP_NAME,
                    new SqlParameter("@Status", "CREATE_FREIGHT_POLICY"),
                    new SqlParameter("@PolicyName", model.PolicyName.Trim()),
                    new SqlParameter("@SlabsJson", slabsJson),
                    new SqlParameter("@CreatedBy", employeeId)
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

                return new ApiResponse
                {
                    Status = Convert.ToInt32(row["Status"]) == 1,
                    Message = row["Message"]?.ToString(),
                    Data = dt.Columns.Contains("FreightPolicyId")
                        ? new
                        {
                            FreightPolicyId = Convert.ToInt32(row["FreightPolicyId"]),
                            PolicyName = row["PolicyName"]?.ToString(),
                            VersionNo = Convert.ToInt32(row["VersionNo"]),
                            IsActive = Convert.ToBoolean(row["IsActive"])
                        }
                        : null
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error creating freight policy.");
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating freight policy.");
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region Update Freight Policy
        public async Task<ApiResponse> UpdateFreightPolicy(UpdateFreightPolicyRequest model, int employeeId)
        {
            try
            {
                //var slabsJson = JsonSerializer.Serialize(model.Slabs);
                var slabsJson = JsonSerializer.Serialize(model.Slabs, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var ds = await _database.ExecuteDataSetAsync(
                    SP_NAME,
                    new SqlParameter("@Status", "UPDATE_FREIGHT_POLICY"),
                    new SqlParameter("@FreightPolicyId", model.FreightPolicyId),
                    new SqlParameter("@SlabsJson", slabsJson),
                    new SqlParameter("@UpdatedBy", employeeId)
                );

                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Unable to update freight policy."
                    };
                }

                var row = ds.Tables[0].Rows[0];

                if (Convert.ToInt32(row["Status"]) == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = row["Message"]?.ToString() ?? "Unable to update freight policy."
                    };
                }

                var slabs = ds.Tables.Count > 1
                    ? ds.Tables[1].Rows.Cast<DataRow>().Select(r => new FreightPolicySlabResponse
                    {
                        FreightPolicySlabId = Convert.ToInt32(r["FreightPolicySlabId"]),
                        FreightPolicyId = Convert.ToInt32(r["FreightPolicyId"]),
                        MinOrderValue = Convert.ToDecimal(r["MinOrderValue"]),
                        MaxOrderValue = r["MaxOrderValue"] == DBNull.Value ? null : Convert.ToDecimal(r["MaxOrderValue"]),
                        FreightAmount = Convert.ToDecimal(r["FreightAmount"])
                    }).ToList()
                    : new List<FreightPolicySlabResponse>();

                return new ApiResponse
                {
                    Status = true,
                    Message = row["Message"]?.ToString() ?? "Freight policy updated successfully.",
                    Data = new UpdateFreightPolicyResponse
                    {
                        FreightPolicyId = Convert.ToInt32(row["FreightPolicyId"]),
                        PolicyName = row["PolicyName"]?.ToString() ?? string.Empty,
                        VersionNo = Convert.ToInt32(row["VersionNo"]),
                        IsActive = Convert.ToBoolean(row["IsActive"]),
                        Slabs = slabs
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
        #endregion

        #region Deactivate Freight Policy
        public async Task<ApiResponse> DeactivateFreightPolicy(int freightPolicyId, int employeeId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(
                    SP_NAME,
                    new SqlParameter("@Status", "DEACTIVATE_FREIGHT_POLICY"),
                    new SqlParameter("@FreightPolicyId", freightPolicyId),
                    new SqlParameter("@UpdatedBy", employeeId)
                );

                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Unable to deactivate freight policy."
                    };
                }

                var row = ds.Tables[0].Rows[0];

                return new ApiResponse
                {
                    Status = Convert.ToInt32(row["Status"]) == 1,
                    Message = row["Message"]?.ToString() ?? "Unable to deactivate freight policy.",
                    Data = new
                    {
                        FreightPolicyId = Convert.ToInt32(row["FreightPolicyId"])
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
        #endregion

        #region Get Freight Policy List
        public async Task<ApiResponse> GetFreightPolicyList()
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    SP_NAME,
                    new SqlParameter("@Status", "GET_FREIGHT_POLICY_LIST")
                );

                var data = dt?.Rows.Cast<DataRow>()
                    .Select(r => new FreightPolicyListResponse
                    {
                        FreightPolicyId = Convert.ToInt32(r["FreightPolicyId"]),
                        PolicyName = r["PolicyName"]?.ToString(),
                        VersionNo = Convert.ToInt32(r["VersionNo"]),
                        IsActive = Convert.ToBoolean(r["IsActive"]),
                        CreatedOn = Convert.ToDateTime(r["CreatedOn"]),
                        CreatedBy = r["CreatedBy"] == DBNull.Value? null: Convert.ToInt64(r["CreatedBy"]),
                        SlabCount = Convert.ToInt32(r["SlabCount"])
                    }).ToList() ?? new List<FreightPolicyListResponse>();

                return new ApiResponse
                {
                    Status = true,
                    Message = "Freight policies fetched successfully.",
                    Data = data
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error fetching freight policy list.");
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching freight policy list.");
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region Get Freight Policy Details
        public async Task<ApiResponse> GetFreightPolicyDetails(int freightPolicyId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(SP_NAME,
                    new SqlParameter("@Status", "GET_FREIGHT_POLICY_DETAILS"),
                    new SqlParameter("@FreightPolicyId", freightPolicyId)
                );

                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Freight policy not found."
                    };
                }

                var row = ds.Tables[0].Rows[0];

                var slabs = ds.Tables.Count > 1
                    ? ds.Tables[1].Rows.Cast<DataRow>()
                        .Select(r => new FreightPolicySlabResponse
                        {
                            FreightPolicySlabId = Convert.ToInt32(r["FreightPolicySlabId"]),
                            FreightPolicyId = Convert.ToInt32(r["FreightPolicyId"]),
                            MinOrderValue = Convert.ToDecimal(r["MinOrderValue"]),
                            MaxOrderValue = r["MaxOrderValue"] == DBNull.Value? null: Convert.ToDecimal(r["MaxOrderValue"]),
                            FreightAmount = Convert.ToDecimal(r["FreightAmount"])
                        }).ToList(): new List<FreightPolicySlabResponse>();

                return new ApiResponse
                {
                    Status = true,
                    Message = "Freight policy details fetched successfully.",
                    Data = new FreightPolicyDetailsResponse
                    {
                        FreightPolicyId = Convert.ToInt32(row["FreightPolicyId"]),
                        PolicyName = row["PolicyName"]?.ToString(),
                        VersionNo = Convert.ToInt32(row["VersionNo"]),
                        IsActive = Convert.ToBoolean(row["IsActive"]),
                        CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                        CreatedBy = row["CreatedBy"] == DBNull.Value? null: Convert.ToInt64(row["CreatedBy"]),
                        UpdatedOn = row["UpdatedOn"] == DBNull.Value? null: Convert.ToDateTime(row["UpdatedOn"]),
                        UpdatedBy = row["UpdatedBy"] == DBNull.Value? null: Convert.ToInt64(row["UpdatedBy"]),
                        Slabs = slabs
                    }
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error fetching freight policy details for PolicyId: {PolicyId}", freightPolicyId);
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching freight policy details for PolicyId: {PolicyId}", freightPolicyId);
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region Get Applicable Freight
        public async Task<ApiResponse> GetApplicableFreight(decimal orderValue)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "GET_APPLICABLE_FREIGHT"),
                    new SqlParameter("@OrderValue", orderValue)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "No applicable freight policy found."
                    };
                }

                var row = dt.Rows[0];

                return new ApiResponse
                {
                    Status = true,
                    Message = "Applicable freight fetched successfully.",
                    Data = new ApplicableFreightResponse
                    {
                        FreightPolicyId = Convert.ToInt32(row["FreightPolicyId"]),
                        PolicyName = row["PolicyName"]?.ToString(),
                        VersionNo = Convert.ToInt32(row["VersionNo"]),
                        FreightPolicySlabId = Convert.ToInt32(row["FreightPolicySlabId"]),
                        MinOrderValue = Convert.ToDecimal(row["MinOrderValue"]),
                        MaxOrderValue = row["MaxOrderValue"] == DBNull.Value? null: Convert.ToDecimal(row["MaxOrderValue"]),
                        FreightAmount = Convert.ToDecimal(row["FreightAmount"])
                    }
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error getting applicable freight for OrderValue: {OrderValue}", orderValue);
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applicable freight for OrderValue: {OrderValue}", orderValue);
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region Get Active Freight Policy
        public async Task<ApiResponse> GetActiveFreightPolicy()
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(SP_NAME,
                    new SqlParameter("@Status", "GET_ACTIVE_FREIGHT_POLICY")
                );

                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Active freight policy not found."
                    };
                }

                var row = ds.Tables[0].Rows[0];

                var slabs = ds.Tables.Count > 1 ? ds.Tables[1].Rows.Cast<DataRow>()
                        .Select(r => new FreightPolicySlabResponse
                        {
                            FreightPolicySlabId = Convert.ToInt32(r["FreightPolicySlabId"]),
                            FreightPolicyId = Convert.ToInt32(r["FreightPolicyId"]),
                            MinOrderValue = Convert.ToDecimal(r["MinOrderValue"]),
                            MaxOrderValue = r["MaxOrderValue"] == DBNull.Value? null: Convert.ToDecimal(r["MaxOrderValue"]),
                            FreightAmount = Convert.ToDecimal(r["FreightAmount"])
                        }).ToList(): new List<FreightPolicySlabResponse>();

                return new ApiResponse
                {
                    Status = true,
                    Message = "Active freight policy fetched successfully.",
                    Data = new FreightPolicyDetailsResponse
                    {
                        FreightPolicyId = Convert.ToInt32(row["FreightPolicyId"]),
                        PolicyName = row["PolicyName"]?.ToString(),
                        VersionNo = Convert.ToInt32(row["VersionNo"]),
                        IsActive = Convert.ToBoolean(row["IsActive"]),
                        Slabs = slabs
                    }
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error fetching active freight policy.");
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active freight policy.");
                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion
    }
}
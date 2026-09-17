using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using CRMWebServices.Services;
using Microsoft.Data.SqlClient;
using System.Data;
using static CRMWebServices.Models.TicketModels;

namespace CRMWebServices.Repository
{
    public class TicketRepository : ITicketRepository
    {
        private readonly IDatabaseService _database;
        private readonly ILogger<TicketRepository> _logger;

        public TicketRepository(IDatabaseService database, ILogger<TicketRepository> logger)
        {
            _database = database;
            _logger = logger;
        }

        #region  Get My Tickets
        public async Task<ApiResponse> GetMyTickets(int employeeId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_TicketManagement",
                    new SqlParameter("@Status", "GET_MY_TICKETS"),
                    new SqlParameter("@EmployeeId", employeeId)
                );

                if (dt == null)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "No response returned from database."
                    };
                }

                var tickets = dt.AsEnumerable().Select(row => new TicketListResponse
                {
                    TicketId = Convert.ToInt32(row["TicketId"]),
                    TicketNo = row["TicketNo"]?.ToString(),
                    OpportunityId = Convert.ToInt32(row["OpportunityId"]),
                    OpportunityNo = row["OpportunityNo"]?.ToString(),
                    OpportunityName = row["OpportunityName"]?.ToString(),
                    OpportunityRequirementId = Convert.ToInt32(row["OpportunityRequirementId"]),
                    ProductId = row["ProductId"] == DBNull.Value ? 0 : Convert.ToInt64(row["ProductId"]),
                    ProductCode = row["ProductCode"]?.ToString(),
                    ProductName = row["ProductName"]?.ToString(),
                    AssignedEmployeeId = Convert.ToInt32(row["AssignedEmployeeId"]),
                    AssignedEmployeeName = row["AssignedEmployeeName"]?.ToString(),
                    TicketStatusId = Convert.ToInt32(row["TicketStatusId"]),
                    TicketStatusCode = row["TicketStatusCode"]?.ToString(),
                    TicketStatusName = row["TicketStatusName"]?.ToString(),
                    Priority = row["Priority"]?.ToString(),
                    ScheduledOn = row["ScheduledOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["ScheduledOn"]),
                    Description = row["Description"]?.ToString(),
                    ActivityId = row["ActivityId"] == DBNull.Value ? (long?)null : Convert.ToInt64(row["ActivityId"]),
                    ActivityTypeId = row["ActivityTypeId"] == DBNull.Value ? (long?)null : Convert.ToInt64(row["ActivityTypeId"]),
                    ActivityCode = row["ActivityCode"]?.ToString(),
                    ActivityName = row["ActivityName"]?.ToString(),
                    ActivityStatusId = row["ActivityStatusId"] == DBNull.Value ? (long?)null : Convert.ToInt64(row["ActivityStatusId"]),
                    ActivityStatusCode = row["ActivityStatusCode"]?.ToString(),
                    ActivityStatusName = row["ActivityStatusName"]?.ToString(),
                    ActivityDateTime = row["ActivityDateTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["ActivityDateTime"])
                }).ToList();

                return new ApiResponse
                {
                    Status = true,
                    Message = tickets.Count > 0 ? "Tickets retrieved successfully." : "No tickets found.",
                    Data = tickets
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error getting tickets for EmployeeId: {EmployeeId}", employeeId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tickets for EmployeeId: {EmployeeId}", employeeId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region Get Ticket Detail
        public async Task<ApiResponse> GetTicketDetail(int ticketId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync("USP_CRM_TicketManagement",
                    new SqlParameter("@Status", "GET_TICKET_DETAIL"),
                    new SqlParameter("@TicketId", ticketId)
                );

                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Ticket not found."
                    };
                }

                var ticketRow = ds.Tables[0].Rows[0];

                var ticket = new TicketInfo
                {
                    TicketId = Convert.ToInt32(ticketRow["TicketId"]),
                    TicketNo = ticketRow["TicketNo"]?.ToString(),
                    OpportunityId = Convert.ToInt32(ticketRow["OpportunityId"]),
                    OpportunityNo = ticketRow["OpportunityNo"]?.ToString(),
                    OpportunityName = ticketRow["OpportunityName"]?.ToString(),
                    LeadId = ticketRow["LeadId"] == DBNull.Value ? (int?)null : Convert.ToInt32(ticketRow["LeadId"]),
                    OpportunityRequirementId = Convert.ToInt32(ticketRow["OpportunityRequirementId"]),
                    ProductId = ticketRow["ProductId"] == DBNull.Value ? 0 : Convert.ToInt64(ticketRow["ProductId"]),
                    ProductCode = ticketRow["ProductCode"]?.ToString(),
                    ProductName = ticketRow["ProductName"]?.ToString(),
                    ProductDescription = ticketRow["ProductDescription"]?.ToString(),
                    BrandName = ticketRow["BrandName"]?.ToString(),
                    ModelNo = ticketRow["ModelNo"]?.ToString(),
                    ProductCategoryId = ticketRow["ProductCategoryId"] == DBNull.Value ? (int?)null : Convert.ToInt32(ticketRow["ProductCategoryId"]),
                    ProductCategoryCode = ticketRow["ProductCategoryCode"]?.ToString(),
                    ProductCategoryName = ticketRow["ProductCategoryName"]?.ToString(),
                    OfferingId = ticketRow["OfferingId"] == DBNull.Value ? (int?)null : Convert.ToInt32(ticketRow["OfferingId"]),
                    OfferingCode = ticketRow["OfferingCode"]?.ToString(),
                    OfferingName = ticketRow["OfferingName"]?.ToString(),
                    Quantity = ticketRow["Quantity"] == DBNull.Value ? 0 : Convert.ToDecimal(ticketRow["Quantity"]),
                    UnitId = ticketRow["UnitId"] == DBNull.Value ? (int?)null : Convert.ToInt32(ticketRow["UnitId"]),
                    AssignedEmployeeId = Convert.ToInt32(ticketRow["AssignedEmployeeId"]),
                    AssignedEmployeeName = ticketRow["AssignedEmployeeName"]?.ToString(),
                    EmployeeCode = ticketRow["EmployeeCode"]?.ToString(),
                    AssignedEmployeeEmail = ticketRow["AssignedEmployeeEmail"]?.ToString(),
                    TicketStatusId = Convert.ToInt32(ticketRow["TicketStatusId"]),
                    TicketStatusCode = ticketRow["TicketStatusCode"]?.ToString(),
                    TicketStatusName = ticketRow["TicketStatusName"]?.ToString(),
                    IsClosed = Convert.ToBoolean(ticketRow["IsClosed"]),
                    Priority = ticketRow["Priority"]?.ToString(),
                    ScheduledOn = ticketRow["ScheduledOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(ticketRow["ScheduledOn"]),
                    TicketDescription = ticketRow["TicketDescription"]?.ToString(),
                    CompletedOn = ticketRow["CompletedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(ticketRow["CompletedOn"]),
                    CompletedBy = ticketRow["CompletedBy"] == DBNull.Value ? (int?)null : Convert.ToInt32(ticketRow["CompletedBy"]),
                    CompletedByEmployeeName = ticketRow["CompletedByEmployeeName"]?.ToString(),
                    CreatedOn = Convert.ToDateTime(ticketRow["CreatedOn"]),
                    CreatedBy = ticketRow["CreatedBy"] == DBNull.Value ? (long?)null : Convert.ToInt64(ticketRow["CreatedBy"])
                };

                var activities = new List<TicketActivityInfo>();

                if (ds.Tables.Count > 1)
                {
                    activities = ds.Tables[1].AsEnumerable().Select(row => new TicketActivityInfo
                    {
                        ActivityId = Convert.ToInt64(row["ActivityId"]),
                        TicketId = Convert.ToInt32(row["TicketId"]),
                        OpportunityId = Convert.ToInt64(row["OpportunityId"]),
                        ActivityTypeId = Convert.ToInt64(row["ActivityTypeId"]),
                        ActivityCode = row["ActivityCode"]?.ToString(),
                        ActivityName = row["ActivityName"]?.ToString(),
                        EmployeeId = Convert.ToInt64(row["EmployeeId"]),
                        EmployeeName = row["EmployeeName"]?.ToString(),
                        ActivityDateTime = Convert.ToDateTime(row["ActivityDateTime"]),
                        Subject = row["Subject"]?.ToString(),
                        Description = row["Description"]?.ToString(),
                        Outcome = row["Outcome"]?.ToString(),
                        NextFollowUpDate = row["NextFollowUpDate"] == DBNull.Value ? null : Convert.ToDateTime(row["NextFollowUpDate"]),
                        DurationInMinutes = row["DurationInMinutes"] == DBNull.Value ? null : Convert.ToInt32(row["DurationInMinutes"]),
                        Latitude = row["Latitude"] == DBNull.Value ? null : Convert.ToDecimal(row["Latitude"]),
                        Longitude = row["Longitude"] == DBNull.Value ? null : Convert.ToDecimal(row["Longitude"]),
                        LocationAddress = row["LocationAddress"]?.ToString(),
                        ActivityStatusId = Convert.ToInt64(row["ActivityStatusId"]),
                        ActivityStatusCode = row["ActivityStatusCode"]?.ToString(),
                        ActivityStatusName = row["ActivityStatusName"]?.ToString(),
                        CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                        CreatedBy = Convert.ToInt64(row["CreatedBy"]),
                        UpdatedOn = row["UpdatedOn"] == DBNull.Value ? null : Convert.ToDateTime(row["UpdatedOn"]),
                        UpdatedBy = row["UpdatedBy"] == DBNull.Value ? null : Convert.ToInt64(row["UpdatedBy"])
                    }).ToList();
                }

                TicketQuotationEligibility quotationEligibility = null;

                if (ds.Tables.Count > 2 && ds.Tables[2].Rows.Count > 0)
                {
                    var row = ds.Tables[2].Rows[0];

                    quotationEligibility = new TicketQuotationEligibility
                    {
                        IsQuotationEligible = Convert.ToBoolean(row["IsQuotationEligible"]),
                        TicketId = Convert.ToInt32(row["TicketId"]),
                        TicketNo = row["TicketNo"]?.ToString(),
                        OpportunityRequirementId = Convert.ToInt32(row["OpportunityRequirementId"]),
                        TicketStatusCode = row["TicketStatusCode"]?.ToString(),
                        TicketStatusName = row["TicketStatusName"]?.ToString()
                    };
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Ticket details retrieved successfully.",
                    Data = new TicketDetailResponse
                    {
                        Ticket = ticket,
                        Activities = activities,
                        QuotationEligibility = quotationEligibility
                    }
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error getting ticket detail for TicketId: {TicketId}", ticketId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ticket detail for TicketId: {TicketId}", ticketId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region Update Ticket Status
        public async Task<ApiResponse> UpdateTicketStatus(UpdateTicketStatusRequest model)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_TicketManagement",
                    new SqlParameter("@Status", "UPDATE_TICKET_STATUS"),
                    new SqlParameter("@TicketId", model.TicketId),
                    new SqlParameter("@TicketStatusId", model.TicketStatusId),
                    new SqlParameter("@EmployeeId", model.EmployeeId),
                    new SqlParameter("@Description", (object?)model.Description ?? DBNull.Value)
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
                    Status = true,
                    Message = "Ticket status updated successfully.",
                    Data = new
                    {
                        TicketId = Convert.ToInt32(row["TicketId"]),
                        TicketNo = row["TicketNo"]?.ToString(),
                        OpportunityId = Convert.ToInt32(row["OpportunityId"]),
                        OpportunityRequirementId = Convert.ToInt32(row["OpportunityRequirementId"]),
                        AssignedEmployeeId = Convert.ToInt32(row["AssignedEmployeeId"]),
                        TicketStatusId = Convert.ToInt32(row["TicketStatusId"]),
                        TicketStatusCode = row["TicketStatusCode"]?.ToString(),
                        TicketStatusName = row["TicketStatusName"]?.ToString(),
                        CompletedOn = row["CompletedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["CompletedOn"]),
                        CompletedBy = row["CompletedBy"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["CompletedBy"]),
                        UpdatedOn = row["UpdatedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["UpdatedOn"]),
                        UpdatedBy = row["UpdatedBy"] == DBNull.Value ? (long?)null : Convert.ToInt64(row["UpdatedBy"])
                    }
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error updating ticket status for TicketId: {TicketId}", model.TicketId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ticket status for TicketId: {TicketId}", model.TicketId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region Update Activity
        public async Task<ApiResponse> UpdateActivity(UpdateTicketActivityRequest model)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_TicketManagement",
                    new SqlParameter("@Status", "UPDATE_ACTIVITY"),
                    new SqlParameter("@TicketId", model.TicketId),
                    new SqlParameter("@ActivityId", model.ActivityId),
                    new SqlParameter("@ActivityStatusId", model.ActivityStatusId),
                    new SqlParameter("@EmployeeId", model.EmployeeId),
                    new SqlParameter("@ScheduledOn", (object?)model.ScheduledOn ?? DBNull.Value),
                    new SqlParameter("@Description", (object?)model.Description ?? DBNull.Value)
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
                    Status = true,
                    Message = "Activity updated successfully.",
                    Data = new
                    {
                        ActivityId = Convert.ToInt64(row["ActivityId"]),
                        TicketId = Convert.ToInt32(row["TicketId"]),
                        OpportunityId = Convert.ToInt64(row["OpportunityId"]),
                        ActivityTypeId = Convert.ToInt64(row["ActivityTypeId"]),
                        ActivityCode = row["ActivityCode"]?.ToString(),
                        ActivityName = row["ActivityName"]?.ToString(),
                        EmployeeId = Convert.ToInt64(row["EmployeeId"]),
                        EmployeeName = row["EmployeeName"]?.ToString(),
                        ActivityDateTime = Convert.ToDateTime(row["ActivityDateTime"]),
                        Subject = row["Subject"]?.ToString(),
                        Description = row["Description"]?.ToString(),
                        Outcome = row["Outcome"]?.ToString(),
                        ActivityStatusId = Convert.ToInt64(row["ActivityStatusId"]),
                        ActivityStatusCode = row["ActivityStatusCode"]?.ToString(),
                        ActivityStatusName = row["ActivityStatusName"]?.ToString(),
                        UpdatedOn = row["UpdatedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["UpdatedOn"]),
                        UpdatedBy = row["UpdatedBy"] == DBNull.Value ? (long?)null : Convert.ToInt64(row["UpdatedBy"])
                    }
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error updating activity for TicketId: {TicketId}, ActivityId: {ActivityId}", model.TicketId, model.ActivityId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating activity for TicketId: {TicketId}, ActivityId: {ActivityId}", model.TicketId, model.ActivityId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region Create Ticket
        public async Task<ApiResponse> CreateTicket(CreateTicketRequest model, long createdBy)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_TicketManagement",
                    new SqlParameter("@Status", "CREATE_TICKET"),
                    new SqlParameter("@OpportunityId", model.OpportunityId),
                    new SqlParameter("@OpportunityRequirementId", model.OpportunityRequirementId),
                    new SqlParameter("@AssignedEmployeeId", model.AssignedEmployeeId),
                    new SqlParameter("@ActivityTypeId", model.ActivityTypeId),
                    new SqlParameter("@ScheduledOn", model.ScheduledOn),
                    new SqlParameter("@Description", (object?)model.Description ?? DBNull.Value),
                    new SqlParameter("@Priority", (object?)model.Priority ?? "NORMAL"),
                    new SqlParameter("@CreatedBy", createdBy)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Ticket could not be created."
                    };
                }

                var r = dt.Rows[0];

                var ticket = new CreateTicketResponse
                {
                    TicketId = Convert.ToInt32(r["TicketId"]),
                    TicketNo = r["TicketNo"] == DBNull.Value ? null : r["TicketNo"].ToString(),
                    OpportunityId = Convert.ToInt32(r["Ref_OpportunityId"]),
                    OpportunityRequirementId =Convert.ToInt32(r["Ref_OpportunityRequirementId"]),
                    AssignedEmployeeId =Convert.ToInt64(r["Ref_AssignedEmployeeId"]),
                    TicketStatusId =Convert.ToInt32(r["Ref_TicketStatusId"]),
                    TicketStatusCode =r["TicketStatusCode"] == DBNull.Value? null: r["TicketStatusCode"].ToString(),
                    TicketStatusName =r["TicketStatusName"] == DBNull.Value? null: r["TicketStatusName"].ToString(),
                    Priority =r["Priority"] == DBNull.Value? null: r["Priority"].ToString(),
                    ScheduledOn =r["ScheduledOn"] == DBNull.Value? (DateTime?)null: Convert.ToDateTime(r["ScheduledOn"]),
                    Description =r["Description"] == DBNull.Value? null: r["Description"].ToString(),
                    ActivityId =r["ActivityId"] == DBNull.Value? 0: Convert.ToInt64(r["ActivityId"])
                };

                return new ApiResponse
                {
                    Status = true,
                    Message = "Ticket created successfully.",
                    Data = ticket
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating ticket.");

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region Get Ticket Statuses
        public async Task<ApiResponse> GetTicketStatuses()
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_TicketManagement",
                    new SqlParameter("@Status", "GET_TICKET_STATUSES")
                );

                var statuses = dt.Rows.Cast<DataRow>()
                    .Select(r => new TicketStatusInfo
                    {
                        TicketStatusId = Convert.ToInt32(r["TicketStatusId"]),
                        StatusCode = r["StatusCode"] == DBNull.Value? null: r["StatusCode"].ToString(),
                        StatusName = r["StatusName"] == DBNull.Value? null: r["StatusName"].ToString(),
                        DisplayOrder = Convert.ToInt32(r["DisplayOrder"]),
                        IsClosed = Convert.ToBoolean(r["IsClosed"]),
                        IsActive = Convert.ToBoolean(r["IsActive"])
                    })
                    .ToList();

                return new ApiResponse
                {
                    Status = true,
                    Message = "Ticket statuses retrieved successfully.",
                    Data = statuses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting ticket statuses.");

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
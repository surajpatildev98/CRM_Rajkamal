using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using CRMWebServices.Services;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using static CRMWebServices.Models.OpportunityModels;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Repository
{
    public class OpportunityRepository : IOpportunityRepository
    {
        private readonly IDatabaseService _database;
        private readonly ILogger<PermissionService> _logger;
        private readonly ISchedulerExecutionService _schedulerExecutionService;

        public OpportunityRepository(IDatabaseService database, ILogger<PermissionService> logger,ISchedulerExecutionService schedulerExecutionService)
        {
            _database = database;
            _logger = logger;
            _schedulerExecutionService = schedulerExecutionService;
        }


        #region // COMMON HELPER METHODS
        private string BuildFileUrl(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return null;

                var relative = filePath.Replace("Uploads/", "").Replace("Uploads\\", "");
                var segments = relative.Split('/').Select(Uri.EscapeDataString);

                //return "/files/" + string.Join("/", segments);
                return "/Uploads/" + string.Join("/", segments);
            }
            catch (Exception ex)
            {
                // Log ex here
                throw new Exception("Error while building attachment URL.", ex);
            }

        }

        private string SaveBase64Attachment(string base64Content, string fileName, string subFolder)
        {
            var base64Data = base64Content.Contains(",")? base64Content.Substring(base64Content.IndexOf(",") + 1): base64Content;

            byte[] fileBytes = Convert.FromBase64String(base64Data);

            string uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", subFolder);

            if (!Directory.Exists(uploadsRoot))
                Directory.CreateDirectory(uploadsRoot);

            string uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            string fullPath = Path.Combine(uploadsRoot, uniqueFileName);

            File.WriteAllBytes(fullPath, fileBytes);

            // Matches BuildFileUrl's expectation: path relative to "Uploads/"
            return Path.Combine("Uploads", subFolder, uniqueFileName).Replace("\\", "/");
        }

        private ApiResponse MapResponse(DataTable dt, bool isCreate)
        {
            if (dt == null || dt.Rows.Count == 0)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = "No response returned from database."
                };
            }

            var row = dt.Rows[0];
            bool status = Convert.ToInt32(row["Status"]) == 1;
            string message = row["Message"]?.ToString();

            if (!status)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = message
                };
            }

            // Collect any extra columns beyond Status/Message as the response Data
            // (e.g. ActivityId, FollowUpId, OpportunityRequirementId)
            var extraData = new Dictionary<string, object>();

            foreach (DataColumn col in dt.Columns)
            {
                if (col.ColumnName == "Status" || col.ColumnName == "Message")
                    continue;

                extraData[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
            }

            return new ApiResponse
            {
                Status = true,
                Message = message,
                Data = extraData.Count > 0 ? extraData : null
            };
        }

        #endregion

        #region GET WON LEADS (LIST - Full Data like GetLeadList)
        public async Task<ApiResponse> GetWonLeads(string searchText = null, int? assignedEmployeeId = null)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "GET_WON_LEADS_LIST_FULL"),
                    new SqlParameter("@SearchText", (object?)searchText ?? DBNull.Value),
                    new SqlParameter("@FilterAssignedToId", (object?)assignedEmployeeId ?? DBNull.Value)
                );

                if (ds.Tables.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = true,
                        Message = "No Won leads available for conversion.",
                        Data = new List<object>()
                    };
                }

                var leadRows = ds.Tables[0].Rows.Cast<System.Data.DataRow>().ToList();
                var contactRows = ds.Tables[1].Rows.Cast<System.Data.DataRow>().ToList();
                var requirementRows = ds.Tables[2].Rows.Cast<System.Data.DataRow>().ToList();
                var activityRows = ds.Tables[3].Rows.Cast<System.Data.DataRow>().ToList();
                var fileRows = ds.Tables[4].Rows.Cast<System.Data.DataRow>().ToList();
                var followUpRows = ds.Tables[5].Rows.Cast<System.Data.DataRow>().ToList();

                var result = new List<object>();

                foreach (var leadRow in leadRows)
                {
                    int leadId = Convert.ToInt32(leadRow["LeadId"]);

                    // Parse LeadAttachmentsJson
                    List<object> leadAttachments = new List<object>();
                    if (leadRow["LeadAttachmentsJson"] != DBNull.Value)
                    {
                        var json = leadRow["LeadAttachmentsJson"].ToString();
                        if (!string.IsNullOrEmpty(json))
                        {
                            var attachments = JsonConvert.DeserializeObject<List<dynamic>>(json);
                            leadAttachments = attachments.Select(a => new
                            {
                                attachmentId = Convert.ToInt32(a.AttachmentId),
                                fileName = a.FileName?.ToString(),
                                fileType = a.FileType?.ToString(),
                                fileSize = a.FileSize != null ? Convert.ToInt64(a.FileSize) : (long?)null,
                                fileUrl = BuildFileUrl(a.FilePath?.ToString()),
                                uploadedOn = Convert.ToDateTime(a.CreatedOn?.ToString())
                            }).Cast<object>().ToList();
                        }
                    }

                    var contacts = contactRows
                        .Where(r => Convert.ToInt32(r["LeadId"]) == leadId)
                        .Select(r => new
                        {
                            contactId = Convert.ToInt32(r["LeadContactId"]),
                            contactPerson = r["ContactPerson"]?.ToString(),
                            designation = r["DesignationName"] == DBNull.Value ? null : r["DesignationName"].ToString(),
                            mobileNo = r["MobileNo"]?.ToString(),
                            email = r["Email"] == DBNull.Value ? null : r["Email"].ToString()
                        }).ToList();

                    var requirements = requirementRows
                        .Where(r => Convert.ToInt32(r["LeadId"]) == leadId)
                        .Select(r => new
                        {
                            leadRequirementId = Convert.ToInt32(r["LeadRequirementId"]),
                            offeringId = Convert.ToInt32(r["OfferingId"]),
                            offeringName = r["OfferingName"]?.ToString(),
                            remark = r["Remark"] == DBNull.Value ? null : r["Remark"].ToString(),
                            quantity = Convert.ToDecimal(r["Quantity"]),
                            unit = r["Unit"] == DBNull.Value ? null : r["Unit"].ToString()
                        }).ToList();

                    var activities = activityRows
                        .Where(r => Convert.ToInt32(r["LeadId"]) == leadId)
                        .Select(r => new
                        {
                            activityId = Convert.ToInt32(r["ActivityId"]),
                            activityType = r["ActivityType"] == DBNull.Value ? null : r["ActivityType"].ToString(),
                            activityDate = Convert.ToDateTime(r["ActivityDate"]),
                            subject = r["Subject"]?.ToString(),
                            outcome = r["Description"] == DBNull.Value ? null : r["Description"].ToString(),
                            createdBy = r["CreatedBy"] == DBNull.Value ? null : r["CreatedBy"].ToString(),
                            activityStatus = r["ActivityStatus"] == DBNull.Value ? null : r["ActivityStatus"].ToString(),
                            activityStatusColor = r["ActivityStatusColor"] == DBNull.Value ? null : r["ActivityStatusColor"].ToString()
                        }).ToList();

                    var files = fileRows
                        .Where(r => Convert.ToInt32(r["LeadId"]) == leadId)
                        .Select(r => new
                        {
                            attachmentId = Convert.ToInt32(r["AttachmentId"]),
                            fileName = r["FileName"]?.ToString(),
                            fileType = r["FileType"] == DBNull.Value ? null : r["FileType"].ToString(),
                            fileSize = r["FileSize"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["FileSize"]),
                            fileUrl = BuildFileUrl(r["FilePath"]?.ToString())
                        }).ToList();

                    var followUps = followUpRows
                        .Where(r => Convert.ToInt32(r["LeadId"]) == leadId)
                        .Select(r => new
                        {
                            followUpId = Convert.ToInt32(r["FollowUpId"]),
                            activityId = r["ActivityId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["ActivityId"]),
                            assignedEmployeeId = r["AssignedEmployeeId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["AssignedEmployeeId"]),
                            assignedTo = r["AssignedTo"]?.ToString(),
                            followUpDate = Convert.ToDateTime(r["FollowUpDate"]),
                            priority = Convert.ToInt32(r["Priority"]),
                            remarks = r["Remarks"]?.ToString(),
                            isCompleted = Convert.ToBoolean(r["IsCompleted"]),
                            completedOn = r["CompletedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CompletedOn"]),
                            followUpStatus = r["FollowUpStatus"]?.ToString(),
                            followUpStatusColor = r["FollowUpStatusColor"]?.ToString(),
                            createdOn = Convert.ToDateTime(r["CreatedOn"]),
                            createdByName = r["CreatedByName"]?.ToString()
                        }).ToList();

                    result.Add(new
                    {
                        lead = new
                        {
                            leadId,
                            leadNo = leadRow["LeadNo"]?.ToString(),
                            leadStatus = new
                            {
                                statusId = leadRow["StatusId"] == DBNull.Value ? (int?)null : Convert.ToInt32(leadRow["StatusId"]),
                                statusName = leadRow["StatusName"]?.ToString()
                            },
                            expectedValue = leadRow["ExpectedValue"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(leadRow["ExpectedValue"]),
                            leadRemark = leadRow["LeadRemark"] == DBNull.Value ? null : leadRow["LeadRemark"].ToString(),
                            attachments = leadAttachments.Any() ? leadAttachments : null,
                            assignedToEmployeeId = leadRow["AssignedToEmployeeId"] == DBNull.Value ? (int?)null : Convert.ToInt32(leadRow["AssignedToEmployeeId"]),
                            assignedToEmployee = leadRow["AssignedToEmployee"] == DBNull.Value ? null : leadRow["AssignedToEmployee"].ToString(),
                            leadSource = leadRow["LeadSource"] == DBNull.Value ? null : leadRow["LeadSource"].ToString(),
                            createdOn = Convert.ToDateTime(leadRow["CreatedOn"]),
                            company = new
                            {
                                organizationId = Convert.ToInt32(leadRow["LeadOrganizationId"]),
                                companyName = leadRow["CompanyName"]?.ToString(),
                                address = leadRow["Address"] == DBNull.Value ? null : leadRow["Address"].ToString(),
                                country = leadRow["CountryName"] == DBNull.Value ? null : leadRow["CountryName"].ToString(),
                                state = leadRow["StateName"] == DBNull.Value ? null : leadRow["StateName"].ToString(),
                                city = leadRow["CityName"] == DBNull.Value ? null : leadRow["CityName"].ToString(),
                                pincode = leadRow["PinCode"] == DBNull.Value ? null : leadRow["PinCode"].ToString(),
                                gstNo = leadRow["GstNo"] == DBNull.Value ? null : leadRow["GstNo"].ToString(),
                                panNo = leadRow["PanNo"] == DBNull.Value ? null : leadRow["PanNo"].ToString()
                            }
                        },
                        contacts,
                        requirements,
                        activities,
                        files,
                        followUps
                    });
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Won leads fetched successfully.",
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
        #endregion       

        #region GET WON LEAD DETAILS (SINGLE - Full Details like GetLeadFullDetails)
        public async Task<ApiResponse> GetWonLeadDetails(int leadId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "GET_WON_LEAD_FULL_DETAILS"),
                    new SqlParameter("@Ref_LeadId", leadId)
                );

                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Lead not found or not in WON status."
                    };
                }

                var leadRow = ds.Tables[0].Rows[0];

                // Parse LeadAttachmentsJson to List
                List<object> leadAttachments = new List<object>();
                if (leadRow["LeadAttachmentsJson"] != DBNull.Value)
                {
                    var json = leadRow["LeadAttachmentsJson"].ToString();
                    if (!string.IsNullOrEmpty(json))
                    {
                        var attachments = JsonConvert.DeserializeObject<List<dynamic>>(json);
                        leadAttachments = attachments.Select(a => new
                        {
                            attachmentId = Convert.ToInt32(a.AttachmentId),
                            fileName = a.FileName?.ToString(),
                            fileType = a.FileType?.ToString(),
                            fileSize = a.FileSize != null ? Convert.ToInt64(a.FileSize) : (long?)null,
                            fileUrl = BuildFileUrl(a.FilePath?.ToString()),
                            uploadedOn = Convert.ToDateTime(a.CreatedOn?.ToString())
                        }).Cast<object>().ToList();
                    }
                }

                var lead = new
                {
                    leadId = Convert.ToInt32(leadRow["LeadId"]),
                    leadNo = leadRow["LeadNo"]?.ToString(),
                    leadStatus = new
                    {
                        statusId = leadRow["StatusId"] == DBNull.Value ? (int?)null : Convert.ToInt32(leadRow["StatusId"]),
                        statusName = leadRow["StatusName"]?.ToString()
                    },
                    expectedValue = leadRow["ExpectedValue"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(leadRow["ExpectedValue"]),
                    leadRemark = leadRow["LeadRemark"] == DBNull.Value ? null : leadRow["LeadRemark"].ToString(),
                    attachments = leadAttachments.Any() ? leadAttachments : null,
                    assignedToEmployeeId = leadRow["AssignedToEmployeeId"] == DBNull.Value ? (int?)null : Convert.ToInt32(leadRow["AssignedToEmployeeId"]),
                    assignedToEmployee = leadRow["AssignedToEmployee"] == DBNull.Value ? null : leadRow["AssignedToEmployee"].ToString(),
                    leadSource = leadRow["LeadSource"] == DBNull.Value ? null : leadRow["LeadSource"].ToString(),
                    createdOn = Convert.ToDateTime(leadRow["CreatedOn"]),
                    company = new
                    {
                        organizationId = Convert.ToInt32(leadRow["LeadOrganizationId"]),
                        companyName = leadRow["CompanyName"]?.ToString(),
                        address = leadRow["Address"] == DBNull.Value ? null : leadRow["Address"].ToString(),
                        country = leadRow["CountryName"] == DBNull.Value ? null : leadRow["CountryName"].ToString(),
                        state = leadRow["StateName"] == DBNull.Value ? null : leadRow["StateName"].ToString(),
                        city = leadRow["CityName"] == DBNull.Value ? null : leadRow["CityName"].ToString(),
                        pincode = leadRow["PinCode"] == DBNull.Value ? null : leadRow["PinCode"].ToString(),
                        gstNo = leadRow["GstNo"] == DBNull.Value ? null : leadRow["GstNo"].ToString(),
                        panNo = leadRow["PanNo"] == DBNull.Value ? null : leadRow["PanNo"].ToString()
                    }
                };

                // Contacts
                var contacts = ds.Tables[1].Rows.Cast<System.Data.DataRow>().Select(r => new
                {
                    contactId = Convert.ToInt32(r["LeadContactId"]),
                    contactPerson = r["ContactPerson"]?.ToString(),
                    designation = r["DesignationName"] == DBNull.Value ? null : r["DesignationName"].ToString(),
                    mobileNo = r["MobileNo"]?.ToString(),
                    email = r["Email"] == DBNull.Value ? null : r["Email"].ToString()
                }).ToList();

                // Requirements
                var requirements = ds.Tables[2].Rows.Cast<System.Data.DataRow>().Select(r => new
                {
                    leadRequirementId = Convert.ToInt32(r["LeadRequirementId"]),
                    offeringId = Convert.ToInt32(r["OfferingId"]),
                    offeringName = r["OfferingName"]?.ToString(),
                    remark = r["Remark"] == DBNull.Value ? null : r["Remark"].ToString(),
                    quantity = Convert.ToDecimal(r["Quantity"]),
                    unit = r["Unit"] == DBNull.Value ? null : r["Unit"].ToString()
                }).ToList();

                // Activities
                var activities = ds.Tables[3].Rows.Cast<System.Data.DataRow>().Select(r => new
                {
                    activityId = Convert.ToInt32(r["ActivityId"]),
                    activityType = r["ActivityType"] == DBNull.Value ? null : r["ActivityType"].ToString(),
                    activityDate = Convert.ToDateTime(r["ActivityDate"]),
                    subject = r["Subject"]?.ToString(),
                    outcome = r["Description"] == DBNull.Value ? null : r["Description"].ToString(),
                    createdBy = r["CreatedBy"] == DBNull.Value ? null : r["CreatedBy"].ToString(),
                    activityStatus = r["ActivityStatus"] == DBNull.Value ? null : r["ActivityStatus"].ToString(),
                    activityStatusColor = r["ActivityStatusColor"] == DBNull.Value ? null : r["ActivityStatusColor"].ToString()
                }).ToList();

                // Files
                var files = ds.Tables[4].Rows.Cast<System.Data.DataRow>().Select(r => new
                {
                    attachmentId = Convert.ToInt32(r["AttachmentId"]),
                    fileName = r["FileName"]?.ToString(),
                    fileType = r["FileType"] == DBNull.Value ? null : r["FileType"].ToString(),
                    fileSize = r["FileSize"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["FileSize"]),
                    fileUrl = BuildFileUrl(r["FilePath"]?.ToString())
                }).ToList();

                // Follow-ups
                var followUps = ds.Tables[5].Rows.Cast<System.Data.DataRow>().Select(r => new
                {
                    followUpId = Convert.ToInt32(r["FollowUpId"]),
                    activityId = r["ActivityId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["ActivityId"]),
                    assignedEmployeeId = r["AssignedEmployeeId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["AssignedEmployeeId"]),
                    assignedTo = r["AssignedTo"]?.ToString(),
                    followUpDate = Convert.ToDateTime(r["FollowUpDate"]),
                    priority = Convert.ToInt32(r["Priority"]),
                    remarks = r["Remarks"]?.ToString(),
                    isCompleted = Convert.ToBoolean(r["IsCompleted"]),
                    completedOn = r["CompletedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CompletedOn"]),
                    followUpStatus = r["FollowUpStatus"]?.ToString(),
                    followUpStatusColor = r["FollowUpStatusColor"]?.ToString(),
                    createdOn = Convert.ToDateTime(r["CreatedOn"]),
                    createdByName = r["CreatedByName"]?.ToString()
                }).ToList();

                return new ApiResponse
                {
                    Status = true,
                    Message = "Lead details fetched successfully.",
                    Data = new
                    {
                        lead,
                        contacts,
                        requirements,
                        activities,
                        files,
                        followUps
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

        #region  // CONVERT LEAD TO OPPORTUNITY
        public async Task<ApiResponse> ConvertLeadToOpportunity(ConvertLeadRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "CONVERT_LEAD_TO_OPPORTUNITY"),
                    new SqlParameter("@Ref_LeadId", request.Ref_LeadId),
                    new SqlParameter("@OpportunityName", request.OpportunityName),
                    new SqlParameter("@Ref_OpportunityTypeId", request.Ref_OpportunityTypeId),
                    //new SqlParameter("@Ref_OpportunityStageId", request.Ref_OpportunityStageId),
                    new SqlParameter("@ExpectedRevenue", request.ExpectedRevenue),
                    new SqlParameter("@ExpectedCloseDate", request.ExpectedCloseDate),
                    new SqlParameter("@Probability", request.Probability),
                    new SqlParameter("@Ref_PriorityId", request.Ref_PriorityId),
                    new SqlParameter("@OpportunityRemark", (object?)request.OpportunityRemark ?? DBNull.Value),
                    new SqlParameter("@AssignedToEmployeeId",(object?)request.AssignedToEmployeeId ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", (object?)request.CreatedBy ?? DBNull.Value)
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
                var status = Convert.ToInt32(row["Status"]);
                var message = row["Message"]?.ToString();

                if (status == 1)
                {
                    /*var data = new ConvertLeadResponse
                    {
                        OpportunityId = Convert.ToInt32(row["OpportunityId"]),
                        CustomerId = row["CustomerId"] == DBNull.Value ? null : Convert.ToInt32(row["CustomerId"]),
                        OpportunityNo = row["OpportunityNo"]?.ToString()
                    };
                    return new ApiResponse
                    {
                        Status = true,
                        Message = message,
                        Data = data
                    };*/

                    int opportunityId =
       Convert.ToInt32(row["OpportunityId"]);

                    string opportunityNo =
                        row["OpportunityNo"]?.ToString();

                    int assignedEmployeeId =
                        row["AssignedToEmployeeId"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(row["AssignedToEmployeeId"]);
                    if (assignedEmployeeId > 0)
                    {
                        try
                        {
                            await _schedulerExecutionService
                                .SendEmployeeNotificationAsync(
                                    schedulerJobItemId: null,
                                    employeeId: assignedEmployeeId,
                                    entityId: opportunityId,
                                    title: "Opportunity Assigned",
                                    message: $"Opportunity {opportunityNo} has been assigned to you.",
                                    entityType: "OPPORTUNITY",
                                    recipientType: "ASSIGNED_EMPLOYEE"
                                );
                        }
                        catch (Exception ex)
                        {
                            // Notification failure should not make
                            // opportunity conversion fail
                            _logger.LogError(
                                ex,
                                "Failed to send opportunity assignment notification. " +
                                "OpportunityId {OpportunityId}, " +
                                "OpportunityNo {OpportunityNo}, " +
                                "AssignedEmployeeId {AssignedEmployeeId}",
                                opportunityId,
                                opportunityNo,
                                assignedEmployeeId);
                        }
                    }
                    var data = new ConvertLeadResponse
                    {
                        OpportunityId = opportunityId,

                        CustomerId =
                            row["CustomerId"] == DBNull.Value
                                ? null
                                : Convert.ToInt32(row["CustomerId"]),

                        OpportunityNo = opportunityNo
                    };

                    return new ApiResponse
                    {
                        Status = true,
                        Message = message,
                        Data = data
                    };
                }
                else
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = message
                    };
                }
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

        #region  // OPPORTUNITY TRANSFER
        public async Task<ApiResponse> TransferOpportunity(OpportunityTransferRequest model)
        {
            var dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",
                new SqlParameter("@Status", "TRANSFER_OPPORTUNITY"),
                new SqlParameter("@Ref_OpportunityId", model.Ref_OpportunityId),
                new SqlParameter("@AssignedToEmployeeId", model.AssignedToEmployeeId),
                new SqlParameter("@AssignmentRemark", (object?)model.AssignmentRemark ?? DBNull.Value),
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
            if (status)
            {
                int opportunityId = Convert.ToInt32(row["OpportunityId"]);
                int assignedToEmployeeId = Convert.ToInt32(row["AssignedToEmployeeId"]);

                response.Data = new
                {
                    OpportunityId = opportunityId,
                    AssignedToEmployeeId = assignedToEmployeeId
                };

                // Send notification to assigned employee
                try
                {
                    await _schedulerExecutionService.SendEmployeeNotificationAsync(
                        schedulerJobItemId: null,
                        employeeId: assignedToEmployeeId,
                        entityId: opportunityId,
                        title: "Opportunity Assigned",
                        message: "An opportunity has been assigned to you.",
                        entityType: "OPPORTUNITY",
                        recipientType: "ASSIGN"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send assigned Opportunity notification. OpportunityId {OpportunityId}, EmployeeId {EmployeeId}",
                        opportunityId,
                        assignedToEmployeeId);
                }
            }
            else
            {
                response.Data = null;
            }

            return response;
        }
        #endregion

        #region // GET OPPORTUNITY LIST (MULTIPLE - READ-ONLY)
        public async Task<ApiResponse> GetOpportunityList(OpportunityListRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "GET_OPPORTUNITY_LIST"),
                    new SqlParameter("@SearchText", (object?)request.SearchText ?? DBNull.Value),
                    new SqlParameter("@FilterStageId", (object?)request.FilterStageId ?? DBNull.Value),
                    new SqlParameter("@FilterOwnerId", (object?)request.FilterOwnerId ?? DBNull.Value),
                    new SqlParameter("@FilterTypeId", (object?)request.FilterTypeId ?? DBNull.Value),            
                    new SqlParameter("@LoggedInEmployeeId",(object?)request.LoggedInEmployeeId ?? DBNull.Value));// IMPORTANT
    
                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = true,
                        Message = "No opportunities found.",
                        Data = new List<OpportunityListResponse>()
                    };
                }

                var result = new List<OpportunityListResponse>();
                foreach (DataRow row in dt.Rows)
                {
                    result.Add(new OpportunityListResponse
                    {
                        OpportunityId = Convert.ToInt32(row["OpportunityId"]),
                        OpportunityNo = row["OpportunityNo"]?.ToString(),
                        OpportunityName = row["OpportunityName"]?.ToString(),
                        Ref_LeadId = row["Ref_LeadId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_LeadId"]),
                        CompanyName = row["CompanyName"]?.ToString(),
                        OpportunityTypeName = row["OpportunityTypeName"]?.ToString(),
                        OpportunityStage = row["OpportunityStage"]?.ToString(),
                        IsClosed = Convert.ToBoolean(row["IsClosed"]),
                        IsWon = Convert.ToBoolean(row["IsWon"]),
                        IsLost = Convert.ToBoolean(row["IsLost"]),
                        ExpectedRevenue = row["ExpectedRevenue"] == DBNull.Value ? null : Convert.ToDecimal(row["ExpectedRevenue"]),
                        ExpectedCloseDate = row["ExpectedCloseDate"] == DBNull.Value ? null : Convert.ToDateTime(row["ExpectedCloseDate"]),
                        Probability = row["Probability"] == DBNull.Value ? null : Convert.ToInt32(row["Probability"]),
                        PriorityName = row["PriorityName"]?.ToString(),
                        //public string PriorityColor { get; set; }
                        OwnerEmployeeId = row["OwnerEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(row["OwnerEmployeeId"]),
                        OwnerEmployee = row["OwnerEmployee"]?.ToString(),
                        CreatedOn = Convert.ToDateTime(row["CreatedOn"])
                    });
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Opportunities fetched successfully.",
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
        #endregion

        #region  // GET OPPORTUNITY DETAILS (SINGLE - READ-ONLY)       
        public async Task<ApiResponse> GetOpportunityDetails(int opportunityId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "GET_OPPORTUNITY_DETAILS"),
                    new SqlParameter("@Ref_OpportunityId", opportunityId)
                );

                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Opportunity not found."
                    };
                }

                var row = ds.Tables[0].Rows[0];
                var contactRows = ds.Tables.Count > 1 ? ds.Tables[1].Rows.Cast<DataRow>().ToList() : new List<DataRow>();
                var requirementRows = ds.Tables.Count > 2 ? ds.Tables[2].Rows.Cast<DataRow>().ToList() : new List<DataRow>();
                var activityRows = ds.Tables.Count > 3 ? ds.Tables[3].Rows.Cast<DataRow>().ToList() : new List<DataRow>();
                var followUpRows = ds.Tables.Count > 4 ? ds.Tables[4].Rows.Cast<DataRow>().ToList() : new List<DataRow>();
                var leadAttachmentRows = ds.Tables.Count > 5 ? ds.Tables[5].Rows.Cast<DataRow>().ToList() : new List<DataRow>();
                var timelineRows = ds.Tables.Count > 6 ? ds.Tables[6].Rows.Cast<DataRow>().ToList() : new List<DataRow>();

                var opportunityInfo = new OpportunityInfo
                {
                    OpportunityId = Convert.ToInt32(row["OpportunityId"]),
                    OpportunityNo = row["OpportunityNo"]?.ToString(),
                    Ref_LeadId = row["Ref_LeadId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_LeadId"]),
                    OpportunityName = row["OpportunityName"]?.ToString(),
                    ExpectedRevenue = row["ExpectedRevenue"] == DBNull.Value? null: Convert.ToDecimal(row["ExpectedRevenue"]),
                    ExpectedCloseDate = row["ExpectedCloseDate"] == DBNull.Value? null: Convert.ToDateTime(row["ExpectedCloseDate"]),
                    Probability = row["Probability"] == DBNull.Value? null: Convert.ToInt32(row["Probability"]),
                    CompetitorName = row["CompetitorName"] == DBNull.Value? null: row["CompetitorName"].ToString(),
                    OpportunityRemark = row["OpportunityRemark"] == DBNull.Value? null: row["OpportunityRemark"].ToString(),
                    CreatedOn = Convert.ToDateTime(row["CreatedOn"]),

                    Customer = new CustomerInfo
                    {
                        CustomerId = Convert.ToInt32(row["CustomerId"]),
                        CompanyName = row["CompanyName"]?.ToString(),
                        Address = row["Address"] == DBNull.Value ? null : row["Address"].ToString(),
                        CountryName = row["CountryName"] == DBNull.Value ? null : row["CountryName"].ToString(),
                        StateName = row["StateName"] == DBNull.Value ? null : row["StateName"].ToString(),
                        CityName = row["CityName"] == DBNull.Value ? null : row["CityName"].ToString(),
                        PinCode = row["PinCode"] == DBNull.Value ? null : row["PinCode"].ToString(),
                        GstNo = row["GstNo"] == DBNull.Value ? null : row["GstNo"].ToString(),
                        PanNo = row["PanNo"] == DBNull.Value ? null : row["PanNo"].ToString()
                    },

                    OpportunityType = new OpportunityTypeInfo
                    {
                        OpportunityTypeId = row["OpportunityTypeId"] == DBNull.Value? null: Convert.ToInt32(row["OpportunityTypeId"]),
                        OpportunityTypeName = row["OpportunityTypeName"]?.ToString()
                    },

                    OpportunityStage = new OpportunityStageInfo
                    {
                        OpportunityStageId = row["OpportunityStageId"] == DBNull.Value? null: Convert.ToInt32(row["OpportunityStageId"]),
                        OpportunityStage = row["OpportunityStage"]?.ToString(),
                        IsClosed = Convert.ToBoolean(row["IsClosed"]),
                        IsWon = Convert.ToBoolean(row["IsWon"]),
                        IsLost = Convert.ToBoolean(row["IsLost"])
                    },

                    Priority = new PriorityInfo
                    {
                        PriorityId = row["PriorityId"] == DBNull.Value? null: Convert.ToInt32(row["PriorityId"]),
                        PriorityName = row["PriorityName"]?.ToString(),
                        PriorityLevel = row["PriorityLevel"] == DBNull.Value? null: Convert.ToInt32(row["PriorityLevel"])
                    },

                    Owner = new OwnerInfo
                    {
                        OwnerEmployeeId = row["OwnerEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(row["OwnerEmployeeId"]),
                        OwnerEmployeeName = row["OwnerEmployeeName"]?.ToString(),
                        OwnerEmail = row["OwnerEmail"] == DBNull.Value ? null : row["OwnerEmail"].ToString(),
                        OwnerMobileNo = row["OwnerMobileNo"] == DBNull.Value ? null : row["OwnerMobileNo"].ToString(),
                        OwnerRoleId = row["OwnerRoleId"] == DBNull.Value ? null : Convert.ToInt32(row["OwnerRoleId"]),
                        OwnerRoleName = row["OwnerRoleName"]?.ToString()
                    }                   
                };

                var contacts = contactRows
                    .Select(contactRow => new CustomerContactInfo
                    {
                        CustomerContactId = Convert.ToInt32(contactRow["CustomerContactId"]),
                        ContactPerson = contactRow["ContactPerson"]?.ToString(),
                        DesignationName = contactRow["DesignationName"] == DBNull.Value? null: contactRow["DesignationName"].ToString(),
                        Mobile = contactRow["Mobile"]?.ToString(),
                        Email = contactRow["Email"] == DBNull.Value? null: contactRow["Email"].ToString(),
                        IsPrimary = Convert.ToBoolean(contactRow["IsPrimary"])
                    }).ToList();

                var requirements = requirementRows
                     .Select(req => new OpportunityRequirementInfo
                     {
                         LeadRequirementId = req["LeadRequirementId"] == DBNull.Value? (int?)null: Convert.ToInt32(req["LeadRequirementId"]),
                         OpportunityRequirementId = Convert.ToInt32(req["OpportunityRequirementId"]),
                         OfferingId = Convert.ToInt32(req["OfferingId"]),
                         OfferingName = req["OfferingName"]?.ToString(),
                         Quantity = Convert.ToDecimal(req["Quantity"]),
                         Remark = req["Remark"] == DBNull.Value? null: req["Remark"].ToString(),
                         Unit = req["Unit"] == DBNull.Value? null: req["Unit"].ToString(),
                         IsOpportunityRequirement = Convert.ToBoolean(req["IsOpportunityRequirement"])
                     }).ToList();
                       

                var activities = activityRows.Select(r =>
                    {                        
                        List<object> attachments = new List<object>();

                        if (r["AttachmentsJson"] != DBNull.Value)
                        {
                            var attachmentJson = r["AttachmentsJson"]?.ToString();

                            if (!string.IsNullOrWhiteSpace(attachmentJson))
                            {
                                var attachmentList =JsonConvert.DeserializeObject<List<dynamic>>(attachmentJson);

                                if (attachmentList != null)
                                {
                                    foreach (var a in attachmentList)
                                    {
                                        attachments.Add(new
                                        {
                                            attachmentId = Convert.ToInt32(a.AttachmentId),
                                            fileName = a.FileName?.ToString(),
                                            fileType = a.FileType?.ToString(),
                                            fileSize = a.FileSize != null? Convert.ToInt64(a.FileSize): (long?)null,
                                            fileUrl = BuildFileUrl(a.FilePath?.ToString()),
                                            uploadedOn = a.CreatedOn != null? Convert.ToDateTime(a.CreatedOn.ToString()): (DateTime?)null
                                        });
                                    }
                                }
                            }
                        }
       
                        List<object> expenses = new List<object>();

                        if (r["ExpensesJson"] != DBNull.Value)
                        {
                            var expenseJson = r["ExpensesJson"]?.ToString();

                            if (!string.IsNullOrWhiteSpace(expenseJson))
                            {
                                var expenseList =JsonConvert.DeserializeObject<List<dynamic>>(expenseJson);

                                if (expenseList != null)
                                {
                                    foreach (var e in expenseList)
                                    {                        
                                        List<object> expenseAttachments =new List<object>();

                                        if (e.AttachmentsJson != null)
                                        {
                                            var expenseAttachmentJson =e.AttachmentsJson.ToString();

                                            if (!string.IsNullOrWhiteSpace(expenseAttachmentJson))
                                            {
                                                var expenseAttachmentList =JsonConvert.DeserializeObject<List<dynamic>>(expenseAttachmentJson);

                                                if (expenseAttachmentList != null)
                                                {
                                                    foreach (var ea in expenseAttachmentList)
                                                    {
                                                        expenseAttachments.Add(new
                                                        {
                                                            attachmentId =Convert.ToInt32(ea.AttachmentId),
                                                            fileName =ea.FileName?.ToString(),
                                                            fileType =ea.FileType?.ToString(),
                                                            fileSize =ea.FileSize != null? Convert.ToInt64(ea.FileSize): (long?)null,
                                                            fileUrl =BuildFileUrl(ea.FilePath?.ToString()),
                                                            uploadedOn =ea.CreatedOn != null? Convert.ToDateTime(ea.CreatedOn.ToString()): (DateTime?)null
                                                        });
                                                    }
                                                }
                                            }
                                        }
                        
                                        expenses.Add(new
                                        {
                                            activityExpenseId =Convert.ToInt32(e.ActivityExpenseId),
                                            expenseCategoryId =Convert.ToInt32(e.Ref_ExpenseCategoryId),
                                            categoryCode =e.CategoryCode?.ToString(),
                                            categoryName =e.CategoryName?.ToString(),
                                            transportModeId =e.Ref_TransportModeId != null? Convert.ToInt32(e.Ref_TransportModeId): (int?)null,
                                            modeCode =e.ModeCode?.ToString(),
                                            modeName =e.ModeName?.ToString(),
                                            expenseDate =e.ExpenseDate != null? Convert.ToDateTime(e.ExpenseDate.ToString()): (DateTime?)null,
                                            amount =e.Amount != null? Convert.ToDecimal(e.Amount): 0,
                                            description =e.Description?.ToString(),
                                            fromLocation =e.FromLocation?.ToString(),
                                            toLocation =e.ToLocation?.ToString(),
                                            createdOn =e.CreatedOn != null? Convert.ToDateTime(e.CreatedOn.ToString()): (DateTime?)null,
                                            attachments =expenseAttachments
                                        });
                                    }
                                }
                            }
                        }

       
                        return new OpportunityActivityInfo
                        {
                            ActivityId =Convert.ToInt32(r["ActivityId"]),
                            ActivityType =r["ActivityType"]?.ToString(),
                            ActivityDate =Convert.ToDateTime(r["ActivityDate"]),
                            Subject =r["Subject"]?.ToString(),
                            Description =r["Description"] == DBNull.Value? null: r["Description"].ToString(),
                            LocationAddress =r["LocationAddress"] == DBNull.Value? null: r["LocationAddress"].ToString(),
                            Outcome =r["Outcome"] == DBNull.Value? null: r["Outcome"].ToString(),
                            CreatedBy =r["CreatedBy"]?.ToString(),
                            ActivityStatusId =r["Ref_ActivityStatusId"] == DBNull.Value? (int?)null: Convert.ToInt32(r["Ref_ActivityStatusId"]),
                            ActivityStatus =r["ActivityStatus"]?.ToString(),
                            ActivityStatusColor =r["ActivityStatusColor"]?.ToString(),
                            Attachments =attachments,
                            Expenses =expenses
                        };
                }).ToList();

                var followUps = followUpRows
                         .Select(r => new OpportunityFollowUpInfo
                         {
                             FollowUpId = Convert.ToInt32(r["FollowUpId"]),
                             ActivityId = r["ActivityId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["ActivityId"]),
                             AssignedEmployeeId = r["AssignedEmployeeId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["AssignedEmployeeId"]),
                             AssignedTo = r["AssignedTo"]?.ToString(),
                             FollowUpDate = Convert.ToDateTime(r["FollowUpDate"]),
                             Priority = Convert.ToInt32(r["Priority"]),
                             Remarks = r["Remarks"]?.ToString(),
                             IsCompleted = Convert.ToBoolean(r["IsCompleted"]),
                             CompletedOn = r["CompletedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["CompletedOn"]),
                             FollowUpStatus = r["FollowUpStatus"]?.ToString(),
                             FollowUpStatusColor = r["FollowUpStatusColor"]?.ToString(),
                             CreatedOn = Convert.ToDateTime(r["CreatedOn"]),
                             CreatedByName = r["CreatedByName"]?.ToString()
                         })
                         .ToList();
                var files = leadAttachmentRows
                        .Select(r => new OpportunityAttachmentInfo
                        {
                            AttachmentId = Convert.ToInt32(r["AttachmentId"]),
                            FileName = r["FileName"]?.ToString(),
                            FileType = r["FileType"] == DBNull.Value ? null : r["FileType"].ToString(),
                            FileSize = r["FileSize"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["FileSize"]),
                            FileUrl = BuildFileUrl(r["FilePath"]?.ToString()),
                            UploadedOn = Convert.ToDateTime(r["CreatedOn"])
                        }).ToList();

                var timeline = timelineRows.Select(r =>
                {
                    List<object> attachments = new List<object>();

                    if (r["AttachmentsJson"] != DBNull.Value)
                    {
                        var json = r["AttachmentsJson"]?.ToString();

                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var attachmentList = JsonConvert.DeserializeObject<List<dynamic>>(json);

                            if (attachmentList != null)
                            {
                                attachments = attachmentList.Select(a => new
                                {
                                    attachmentId = Convert.ToInt32(a.AttachmentId),
                                    fileName = a.FileName?.ToString(),
                                    fileType = a.FileType?.ToString(),
                                    fileSize = a.FileSize != null ? Convert.ToInt64(a.FileSize) : (long?)null,
                                    fileUrl = BuildFileUrl(a.FilePath?.ToString()),
                                    uploadedOn = a.CreatedOn != null ? Convert.ToDateTime(a.CreatedOn.ToString()) : (DateTime?)null
                                }).Cast<object>().ToList();
                            }
                        }
                    }

                    List<object> expenses = new List<object>();

                    if (r["ExpensesJson"] != DBNull.Value)
                    {
                        var json = r["ExpensesJson"]?.ToString();

                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var expenseList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                            if (expenseList != null)
                            {
                                foreach (var e in expenseList)
                                {
                                    List<object> expenseAttachments = new List<object>();

                                    if (e.ContainsKey("AttachmentsJson") && e["AttachmentsJson"] != null)
                                    {
                                        var attachmentJson = e["AttachmentsJson"]?.ToString();

                                        if (!string.IsNullOrWhiteSpace(attachmentJson))
                                        {
                                            var expenseAttachmentList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(attachmentJson);

                                            if (expenseAttachmentList != null)
                                            {
                                                foreach (var a in expenseAttachmentList)
                                                {
                                                    expenseAttachments.Add(new
                                                    {
                                                        attachmentId = a.ContainsKey("AttachmentId") && a["AttachmentId"] != null ? Convert.ToInt32(a["AttachmentId"]) : 0,
                                                        fileName = a.ContainsKey("FileName") ? a["FileName"]?.ToString() : null,
                                                        fileType = a.ContainsKey("FileType") ? a["FileType"]?.ToString() : null,
                                                        fileSize = a.ContainsKey("FileSize") && a["FileSize"] != null ? Convert.ToInt64(a["FileSize"]) : (long?)null,
                                                        fileUrl = a.ContainsKey("FilePath") ? BuildFileUrl(a["FilePath"]?.ToString()) : null,
                                                        uploadedOn = a.ContainsKey("CreatedOn") && a["CreatedOn"] != null ? Convert.ToDateTime(a["CreatedOn"]) : (DateTime?)null
                                                    });
                                                }
                                            }
                                        }
                                    }

                                    expenses.Add(new
                                    {
                                        activityExpenseId = e.ContainsKey("ActivityExpenseId") && e["ActivityExpenseId"] != null ? Convert.ToInt32(e["ActivityExpenseId"]) : 0,
                                        expenseCategoryId = e.ContainsKey("Ref_ExpenseCategoryId") && e["Ref_ExpenseCategoryId"] != null ? Convert.ToInt32(e["Ref_ExpenseCategoryId"]) : 0,
                                        categoryCode = e.ContainsKey("CategoryCode") ? e["CategoryCode"]?.ToString() : null,
                                        categoryName = e.ContainsKey("CategoryName") ? e["CategoryName"]?.ToString() : null,
                                        transportModeId = e.ContainsKey("Ref_TransportModeId") && e["Ref_TransportModeId"] != null ? Convert.ToInt32(e["Ref_TransportModeId"]) : (int?)null,
                                        modeCode = e.ContainsKey("ModeCode") ? e["ModeCode"]?.ToString() : null,
                                        modeName = e.ContainsKey("ModeName") ? e["ModeName"]?.ToString() : null,
                                        amount = e.ContainsKey("Amount") && e["Amount"] != null ? Convert.ToDecimal(e["Amount"]) : 0,
                                        description = e.ContainsKey("Description") ? e["Description"]?.ToString() : null,
                                        fromLocation = e.ContainsKey("FromLocation") ? e["FromLocation"]?.ToString() : null,
                                        toLocation = e.ContainsKey("ToLocation") ? e["ToLocation"]?.ToString() : null,
                                        expenseDate = e.ContainsKey("ExpenseDate") && e["ExpenseDate"] != null ? Convert.ToDateTime(e["ExpenseDate"]) : (DateTime?)null,
                                        createdOn = e.ContainsKey("CreatedOn") && e["CreatedOn"] != null ? Convert.ToDateTime(e["CreatedOn"]) : (DateTime?)null,
                                        attachments = expenseAttachments
                                    });
                                }
                            }
                        }
                    }

                    //    return new
                    //    {
                    //        itemType = r["ItemType"]?.ToString(),
                    //        itemId = Convert.ToInt32(r["ItemId"]),
                    //        activityType = r["ActivityType"]?.ToString(),
                    //        itemDate = Convert.ToDateTime(r["ActivityDate"]),
                    //        subject = r["Subject"]?.ToString(),
                    //        description = r["Description"] == DBNull.Value ? null : r["Description"].ToString(),
                    //        outcome = r["Outcome"] == DBNull.Value ? null : r["Outcome"].ToString(),
                    //        locationAddress = r["LocationAddress"] == DBNull.Value ? null : r["LocationAddress"].ToString(),
                    //        createdBy = r["CreatedBy"] == DBNull.Value ? null : r["CreatedBy"].ToString(),
                    //        status = r["Status"]?.ToString(),
                    //        statusColor = r["StatusColor"]?.ToString(),
                    //        createdOn = Convert.ToDateTime(r["CreatedOn"]),
                    //        attachments,
                    //        expenses
                    //    };
                    //}).ToList();

                    return new
                    {
                        itemType = r["ItemType"]?.ToString(),
                        itemId = Convert.ToInt32(r["ItemId"]),
                        activityType = r["ActivityType"]?.ToString(),
                        itemDate = Convert.ToDateTime(r["ActivityDate"]),
                        subject = r["Subject"]?.ToString(),
                        description = r["Description"] == DBNull.Value ? null : r["Description"].ToString(),
                        outcome = r["Outcome"] == DBNull.Value ? null : r["Outcome"].ToString(),
                        locationAddress = r["LocationAddress"] == DBNull.Value ? null : r["LocationAddress"].ToString(),
                        createdBy = r["CreatedBy"] == DBNull.Value ? null : r["CreatedBy"].ToString(),
                        status = r["Status"]?.ToString(),
                        statusColor = r["StatusColor"]?.ToString(),
                        createdOn = Convert.ToDateTime(r["CreatedOn"]),
                        attachments,
                        expenses
                    };}).Cast<object>().ToList();

                var responseData = new OpportunityDetailResponse
                {
                    Opportunity = opportunityInfo,
                    Contacts = contacts,
                    Requirements = requirements,
                    Activities = activities,
                    Files = files,
                    FollowUps = followUps,
                    Timeline = timeline
                };

                return new ApiResponse
                {
                    Status = true,
                    Message = "Opportunity details fetched successfully.",
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
        #endregion

        #region  // UPDATE OPPORTUNITY Currently Not in Use
        public async Task<ApiResponse> UpdateOpportunity(UpdateOpportunityRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "UPDATE_OPPORTUNITY"),
                    new SqlParameter("@Ref_OpportunityId", request.Ref_OpportunityId),
                    new SqlParameter("@OpportunityName", (object?)request.OpportunityName ?? DBNull.Value),
                    new SqlParameter("@Ref_OpportunityTypeId", (object?)request.Ref_OpportunityTypeId ?? DBNull.Value),
                    new SqlParameter("@Ref_OpportunityStageId", (object?)request.Ref_OpportunityStageId ?? DBNull.Value),
                    new SqlParameter("@ExpectedRevenue", (object?)request.ExpectedRevenue ?? DBNull.Value),
                    new SqlParameter("@ExpectedCloseDate", (object?)request.ExpectedCloseDate ?? DBNull.Value),
                    new SqlParameter("@Probability", (object?)request.Probability ?? DBNull.Value),
                    new SqlParameter("@Ref_PriorityId", (object?)request.Ref_PriorityId ?? DBNull.Value),
                    new SqlParameter("@OpportunityRemark", (object?)request.OpportunityRemark ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", (object?)request.CreatedBy ?? DBNull.Value)
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
                var status = Convert.ToInt32(row["Status"]);
                var message = row["Message"]?.ToString();

                return new ApiResponse
                {
                    Status = status == 1,
                    Message = message
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

        #region // DELETE OPPORTUNITY Currently Not in Use
        public async Task<ApiResponse> DeleteOpportunity(DeleteOpportunityRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "DELETE_OPPORTUNITY"),
                    new SqlParameter("@Ref_OpportunityId", request.Ref_OpportunityId),
                    new SqlParameter("@CreatedBy", (object?)request.CreatedBy ?? DBNull.Value)
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
                var status = Convert.ToInt32(row["Status"]);
                var message = row["Message"]?.ToString();

                return new ApiResponse
                {
                    Status = status == 1,
                    Message = message
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

        #region  // DROPDOWNS
        public async Task<ApiResponse> GetOpportunityTypes()
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "GET_OPPORTUNITY_TYPES")
                );

                var result = new List<OpportunityTypeDropdown>();
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        result.Add(new OpportunityTypeDropdown
                        {
                            Id = Convert.ToInt32(row["Id"]),
                            Name = row["Name"]?.ToString()
                        });
                    }
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Opportunity types fetched successfully.",
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
        public async Task<ApiResponse> GetOpportunityStages()
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "GET_OPPORTUNITY_STAGES")
                );

                var result = new List<OpportunityStageDropdown>();
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        result.Add(new OpportunityStageDropdown
                        {
                            Id = Convert.ToInt32(row["Id"]),
                            Name = row["Name"]?.ToString(),
                            IsClosed = Convert.ToBoolean(row["IsClosed"]),
                            IsWon = Convert.ToBoolean(row["IsWon"]),
                            IsLost = Convert.ToBoolean(row["IsLost"])
                        });
                    }
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Opportunity stages fetched successfully.",
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
        public async Task<ApiResponse> GetPriorities()
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "GET_OPPORTUNITY_PRIORITIES")
                );

                var result = new List<PriorityDropdown>();
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        result.Add(new PriorityDropdown
                        {
                            Id = Convert.ToInt32(row["Id"]),
                            Name = row["Name"]?.ToString(),
                            PriorityLevel = Convert.ToInt32(row["PriorityLevel"]),
                            //ColorCode = row["ColorCode"]?.ToString()
                        });
                    }
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Priorities fetched successfully.",
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
        #endregion

        #region // GET ASSIGNED EMPLOYEE ID FOR OPPORTUNITY
        public async Task<int?> GetAssignedEmployeeId(int opportunityId)
        {
            var parameters = new[]
            {
                new SqlParameter("@Status", "GET_ASSIGNED_EMPLOYEE"),
                new SqlParameter("@Ref_OpportunityId", opportunityId)
            };

            DataTable dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",parameters);

            if (dt.Rows.Count == 0)
                return null;

            return dt.Rows[0]["Ref_CurrentAssignedEmployeeId"] == DBNull.Value? null: Convert.ToInt32(dt.Rows[0]["Ref_CurrentAssignedEmployeeId"]);
        }
        #endregion

        #region // GET OPPORTUNITY ACCESS INFO
        public async Task<OpportunityAccessInfo?> GetOpportunityAccessInfo(int opportunityId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "GET_OPPORTUNITY_ACCESS_INFO"),
                    new SqlParameter("@Ref_OpportunityId", opportunityId));

                if (dt.Rows.Count == 0)
                    return null;

                var row = dt.Rows[0];
                return new OpportunityAccessInfo
                {
                    OpportunityId = opportunityId,
                    CreatedBy = row["CreatedBy"] == DBNull.Value ? null : (int?)Convert.ToInt32(row["CreatedBy"]),
                    AssignedEmployeeId = row["Ref_OwnerEmployeeId"] == DBNull.Value
                        ? null
                        : (int?)Convert.ToInt32(row["Ref_OwnerEmployeeId"])
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting opportunity access info for OpportunityId: {OpportunityId}", opportunityId);
                return null;
            }
        }
        #endregion

        #region // ADD OPPORTUNITY REQUIREMENT
        public async Task<ApiResponse> AddOpportunityRequirement(AddOpportunityRequirementRequest model)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "ADD_OPPORTUNITY_REQUIREMENT"),
                    new SqlParameter("@Ref_OpportunityId", model.Ref_OpportunityId),
                    new SqlParameter("@Ref_ProductId", model.Ref_ProductId),
                    new SqlParameter("@Quantity", model.Quantity),
                    new SqlParameter("@RequirementRemark", (object?)model.RequirementRemark ?? DBNull.Value),
                    new SqlParameter("@Ref_UnitId", (object?)model.Ref_UnitId ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", model.CreatedBy)
                );

                return MapResponse(dt, isCreate: true);
            }
            catch (Exception ex)
            {
                return new ApiResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }
        #endregion

        #region // UPDATE OPPORTUNITY REQUIREMENT
        public async Task<ApiResponse> UpdateOpportunityRequirement(UpdateOpportunityRequirementRequest model)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "UPDATE_OPPORTUNITY_REQUIREMENT"),
                    new SqlParameter("@Ref_OpportunityId", model.Ref_OpportunityId),
                    new SqlParameter("@Ref_OpportunityRequirementId", (object?)model.Ref_OpportunityRequirementId ?? DBNull.Value),
                    new SqlParameter("@Ref_LeadRequirementId", (object?)model.Ref_LeadRequirementId ?? DBNull.Value),
                    new SqlParameter("@Ref_ProductId", (object?)model.Ref_ProductId ?? DBNull.Value),
                    new SqlParameter("@Quantity", model.Quantity),
                    new SqlParameter("@RequirementRemark", (object?)model.RequirementRemark ?? DBNull.Value),
                    new SqlParameter("@Ref_UnitId", (object?)model.Ref_UnitId ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", model.CreatedBy)
                );

                return MapResponse(dt, isCreate: false);
            }
            catch (Exception ex)
            {
                return new ApiResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }
        #endregion

        #region // CREATE OPPORTUNITY ACTIVITY
        public async Task<ApiResponse> CreateOpportunityActivity(CreateOpportunityActivityRequest model)
        {
            try
            {
                List<object> savedAttachments = new List<object>();

                if (model.Attachments != null && model.Attachments.Count > 0)
                {
                    foreach (var att in model.Attachments)
                    {
                        if (string.IsNullOrWhiteSpace(att.Base64Content) || string.IsNullOrWhiteSpace(att.FileName))
                            continue;

                        try
                        {
                            string savedPath = SaveBase64Attachment(
                                att.Base64Content,
                                att.FileName,
                                "OpportunityActivityAttachments"
                            );

                            savedAttachments.Add(new
                            {
                                FileName = att.FileName,
                                FilePath = savedPath,
                                FileType = Path.GetExtension(att.FileName).TrimStart('.').ToLowerInvariant(),
                                FileSize = Convert.FromBase64String(att.Base64Content.Contains(",")
                                        ? att.Base64Content.Substring(att.Base64Content.IndexOf(",") + 1)
                                        : att.Base64Content).Length
                            });
                        }
                        catch (Exception ex)
                        {
                            return new ApiResponse
                            {
                                Status = false,
                                Message = $"Error processing attachment '{att.FileName}': {ex.Message}"
                            };
                        }
                    }
                }

                string? attachmentsJson = savedAttachments.Count > 0? JsonConvert.SerializeObject(savedAttachments) : null;

                var dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "CREATE_OPPORTUNITY_ACTIVITY"),
                    new SqlParameter("@Ref_OpportunityId", model.Ref_OpportunityId),
                    new SqlParameter("@Ref_ActivityTypeId", model.Ref_ActivityTypeId),
                    new SqlParameter("@Subject", model.Subject),
                    new SqlParameter("@Description", model.Description),
                    new SqlParameter("@Outcome", (object?)model.Outcome ?? DBNull.Value),
                    new SqlParameter("@ActivityDateTime", model.ActivityDateTime),
                    new SqlParameter("@DurationInMinutes", (object?)model.DurationInMinutes ?? DBNull.Value),
                    new SqlParameter("@Latitude", (object?)model.Latitude ?? DBNull.Value),
                    new SqlParameter("@Longitude", (object?)model.Longitude ?? DBNull.Value),
                    new SqlParameter("@LocationAddress", (object?)model.LocationAddress ?? DBNull.Value),
                    new SqlParameter("@Ref_ActivityStatusId", (object?)model.Ref_ActivityStatusId ?? DBNull.Value),
                    new SqlParameter("@Ref_NewOpportunityStageId", (object?)model.Ref_NewOpportunityStageId ?? DBNull.Value),
                    new SqlParameter("@NextFollowUpDate", (object?)model.NextFollowUpDate ?? DBNull.Value),
                    new SqlParameter("@AttachmentsMeta", (object?)attachmentsJson ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", model.CreatedBy)
                );

                return MapResponse(dt, isCreate: true);
            }
            catch (SqlException ex)
            {
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }
        #endregion

        #region // CREATE OPPORTUNITY FOLLOW-UP
        public async Task<ApiResponse> CreateOpportunityFollowUp(CreateOpportunityFollowUpRequest model)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "CREATE_OPPORTUNITY_FOLLOWUP"),
                    new SqlParameter("@Ref_OpportunityId", model.Ref_OpportunityId),
                    new SqlParameter("@NextFollowUpDate", model.NextFollowUpDate),
                    new SqlParameter("@FollowUpAssignedEmployeeId", (object?)model.FollowUpAssignedEmployeeId ?? DBNull.Value),
                    new SqlParameter("@FollowUpRemarks", (object?)model.FollowUpRemarks ?? DBNull.Value),
                    new SqlParameter("@FollowUpPriority", (object?)model.FollowUpPriority ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", model.CreatedBy)
                );

                return MapResponse(dt, isCreate: true);
            }
            catch (Exception ex)
            {
                return new ApiResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }
        #endregion

        #region // CLOSE OPPORTUNITY
        //public async Task<ApiResponse> CloseOpportunity(CloseOpportunityRequest model)
        //{
        //    try
        //    {
        //        var dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",
        //            new SqlParameter("@Status", "CLOSE_OPPORTUNITY"),
        //            new SqlParameter("@Ref_OpportunityId", model.Ref_OpportunityId),
        //            new SqlParameter("@Ref_OpportunityStageIdClose", model.Ref_OpportunityStageIdClose),
        //            new SqlParameter("@ClosureRemark", (object?)model.ClosureRemark ?? DBNull.Value),
        //            new SqlParameter("@CreatedBy", model.CreatedBy)
        //        );

        //        return MapResponse(dt, isCreate: false);
        //    }
        //    catch (SqlException ex)
        //    {
        //        return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse { Status = false, Message = $"Error: {ex.Message}" };
        //    }
        //}

        public async Task<ApiResponse> CloseOpportunity(CloseOpportunityRequest model)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "CLOSE_OPPORTUNITY"),
                    new SqlParameter("@Ref_OpportunityId", model.Ref_OpportunityId),
                    new SqlParameter("@Ref_OpportunityStageIdClose", model.Ref_OpportunityStageIdClose),
                    new SqlParameter("@ClosureRemark", (object?)model.ClosureRemark ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", model.CreatedBy)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse { Status = false, Message = "No response from server." };
                }

                var row = dt.Rows[0];
                bool success = Convert.ToBoolean(row["Status"]);

                if (!success)
                {
                    return new ApiResponse { Status = false, Message = row["Message"]?.ToString() };
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = row["Message"]?.ToString(),
                    Data = new
                    {
                        OpportunityId = Convert.ToInt32(row["OpportunityId"]),
                        OpportunityStageId = Convert.ToInt32(row["OpportunityStageId"]),
                        StageName = row["StageName"]?.ToString(),
                        CustomerId = row["CustomerId"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["CustomerId"])
                    }
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }
        #endregion

        #region // GET OPPORTUNITY CLOSE STAGES
        public async Task<ApiResponse> GetOpportunityCloseStages()
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "GET_OPPORTUNITY_CLOSE_STAGES")
                );

                var result = new List<OpportunityCloseStageDropdown>();
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        result.Add(new OpportunityCloseStageDropdown
                        {
                            Id = Convert.ToInt32(row["Id"]),
                            Name = row["Name"]?.ToString(),
                            IsWon = Convert.ToBoolean(row["IsWon"]),
                            IsLost = Convert.ToBoolean(row["IsLost"])
                        });
                    }
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Opportunity close stages fetched successfully.",
                    Data = result
                };
            }
            catch (SqlException ex)
            {
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }

        #endregion

        #region // UPDATE OPPORTUNITY FOLLOW-UP STATUS
        public async Task<ApiResponse> UpdateOpportunityFollowUpStatus(UpdateOpportunityFollowUpStatusRequest model)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "UPDATE_OPPORTUNITY_FOLLOWUP_STATUS"),
                    new SqlParameter("@FollowUpId", model.FollowUpId),
                    new SqlParameter("@Ref_OpportunityId", model.Ref_OpportunityId),
                    new SqlParameter("@Ref_ActivityStatusId", model.Ref_ActivityStatusId),
                    new SqlParameter("@NewScheduledOn", (object?)model.NewScheduledOn ?? DBNull.Value),
                    new SqlParameter("@StatusRemark", (object?)model.StatusRemark ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", model.CreatedBy)
                );

                return MapResponse(dt, isCreate: false);
            }
            catch (SqlException ex)
            {
                return new ApiResponse { Status = false, Message = $"Database Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Status = false, Message = $"Error: {ex.Message}" };
            }
        }
        #endregion

        #region // DELETE OPPORTUNITY REQUIREMENT
        public async Task<ApiResponse> DeleteOpportunityRequirement(DeleteOpportunityRequirementRequest model)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync("USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "DELETE_OPPORTUNITY_REQUIREMENT"),
                    new SqlParameter("@Ref_OpportunityRequirementId", model.Ref_OpportunityRequirementId),
                    new SqlParameter("@DeleteRemark", (object?)model.DeleteRemark ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", model.CreatedBy)
                );

                return MapResponse(dt, isCreate: false);
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

        #region // ADD EXPENSEE
        public async Task<ApiResponse> AddOpportunityActivityExpenses(AddOpportunityActivityExpenseRequest model)
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

                if (model.Ref_OpportunityId <= 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Opportunity Id is required."
                    };
                }

                if (model.Ref_ActivityId <= 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Activity Id is required."
                    };
                }

                if (model.Expenses == null || model.Expenses.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "At least one expense is required."
                    };
                }

                /*
                 * -------------------------------------------------------
                 * Prepare Expense JSON
                 * -------------------------------------------------------
                 *
                 * Base64 files are first saved physically.
                 * SQL receives only:
                 *
                 * FileName
                 * FilePath
                 * FileType
                 * FileSize
                 *
                 */

                var expenseItems = new List<object>();

                foreach (var expense in model.Expenses)
                {
                    var attachments = new List<object>();

                    if (expense.Attachments != null &&
                        expense.Attachments.Count > 0)
                    {
                        foreach (var attachment in expense.Attachments)
                        {
                            if (string.IsNullOrWhiteSpace(attachment.FileName))
                            {
                                return new ApiResponse
                                {
                                    Status = false,
                                    Message = "Attachment file name is required."
                                };
                            }

                            if (string.IsNullOrWhiteSpace(attachment.Base64))
                            {
                                return new ApiResponse
                                {
                                    Status = false,
                                    Message =
                                        $"Attachment content is required for '{attachment.FileName}'."
                                };
                            }

                            byte[] fileBytes;

                            try
                            {
                                var base64Data =
                                    attachment.Base64.Contains(",")
                                        ? attachment.Base64.Substring(
                                            attachment.Base64.IndexOf(",") + 1)
                                        : attachment.Base64;

                                fileBytes =
                                    Convert.FromBase64String(base64Data);
                            }
                            catch
                            {
                                return new ApiResponse
                                {
                                    Status = false,
                                    Message =
                                        $"Invalid Base64 content for '{attachment.FileName}'."
                                };
                            }

                            /*
                             * Save physical file using existing helper
                             */
                            string filePath = SaveBase64Attachment(
                                attachment.Base64,
                                attachment.FileName,
                                "OpportunityActivityExpenseAttachments"
                            );

                            string fileType =
                                Path.GetExtension(attachment.FileName)
                                    .TrimStart('.')
                                    .ToLowerInvariant();

                            attachments.Add(new
                            {
                                FileName = attachment.FileName,
                                FilePath = filePath,
                                FileType = fileType,
                                FileSize = fileBytes.LongLength
                            });
                        }
                    }

                    expenseItems.Add(new
                    {
                        Ref_ExpenseCategoryId =
                            expense.Ref_ExpenseCategoryId,

                        Ref_TransportModeId =
                            expense.Ref_TransportModeId,

                        Amount =
                            expense.Amount,

                        Description =
                            expense.Description,

                        FromLocation =
                            expense.FromLocation,

                        ToLocation =
                            expense.ToLocation,

                        Attachments = attachments
                    });
                }

                string expensesJson =
                    JsonConvert.SerializeObject(expenseItems);
       

                var dt = await _database.ExecuteDataTableAsync(
                    "USP_CRM_OpportunityManagement",

                    new SqlParameter(
                        "@Status",
                        "ADD_ACTIVITY_EXPENSE"
                    ),

                    new SqlParameter(
                        "@Ref_OpportunityId",
                        model.Ref_OpportunityId
                    ),

                    new SqlParameter(
                        "@Ref_ActivityId",
                        model.Ref_ActivityId
                    ),

                    new SqlParameter(
                        "@ExpenseItems",
                        expensesJson
                    ),

                    new SqlParameter(
                        "@CreatedBy",
                        (object?)model.CreatedBy ?? DBNull.Value
                    )
                );

                /*
                 * -------------------------------------------------------
                 * Map SP response
                 * -------------------------------------------------------
                 */

                return MapResponse(dt, true);
            }
            catch (FormatException)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = "One or more attachment files contain invalid Base64 content."
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
    }
}
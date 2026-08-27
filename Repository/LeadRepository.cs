using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using CRMWebServices.Services;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CRMWebServices.Repository
{
    public class LeadRepository : ILeadRepository
    {
        private readonly IDatabaseService _database;
        private readonly ILogger<PermissionService> _logger;

        public LeadRepository(IDatabaseService database, ILogger<PermissionService> logger)
        {
            _database = database;
            _logger = logger;
        }

        #region // Attachment Handling and File Saving & Map Response

        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        private string SaveBase64Attachment(string base64, string originalFileName, string folderName)
        {
            if (string.IsNullOrWhiteSpace(base64) || string.IsNullOrWhiteSpace(originalFileName))
                return null;

            // Remove data URI prefix if Android sends it
            if (base64.Contains(","))
                base64 = base64.Substring(base64.IndexOf(",") + 1);

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

        private ApiResponse MapResponse(System.Data.DataTable dt, bool isCreate)
        {
            try
            {


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
                    response.Data = isCreate
                        ? new
                        {
                            LeadId = Convert.ToInt32(row["LeadId"]),
                            LeadNo = row["LeadNo"]?.ToString(),
                            LeadOrganizationId = Convert.ToInt32(row["LeadOrganizationId"]),
                            //AssignedToEmployeeId = Convert.ToInt32(row["AssignedToEmployeeId"])
                            AssignedToEmployeeId = row["AssignedToEmployeeId"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["AssignedToEmployeeId"])
                        }
                        : new
                        {
                            LeadId = Convert.ToInt32(row["LeadId"]),
                            //AssignedToEmployeeId = Convert.ToInt32(row["AssignedToEmployeeId"])
                            AssignedToEmployeeId = row["AssignedToEmployeeId"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["AssignedToEmployeeId"])
                        };
                }

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

        #region // Create Lead 
        public async Task<ApiResponse> CreateLead(LeadRegisterRequest model)
        {
            try
            {
                string contactsJson = model.Contacts != null && model.Contacts.Count > 0
                    ? JsonConvert.SerializeObject(model.Contacts) : null;

                string requirementsJson = model.Requirements != null && model.Requirements.Count > 0
                    ? JsonConvert.SerializeObject(model.Requirements) : null;

                // Process LEAD ATTACHMENTS - Same pattern as Activity
                List<object> savedAttachments = new List<object>();

                if (model.Attachments != null && model.Attachments.Count > 0)
                {
                    foreach (var att in model.Attachments)
                    {
                        if (string.IsNullOrWhiteSpace(att.Base64Content) || string.IsNullOrWhiteSpace(att.FileName))
                            continue;

                        try
                        {
                            string savedPath = SaveBase64Attachment(att.Base64Content, att.FileName, "LeadAttachments");

                            savedAttachments.Add(new
                            {
                                FileName = att.FileName,
                                FilePath = savedPath,
                                FileType = Path.GetExtension(att.FileName).TrimStart('.').ToLowerInvariant(),
                                FileSize = Convert.FromBase64String(
                                    att.Base64Content.Contains(",")
                                        ? att.Base64Content.Substring(att.Base64Content.IndexOf(",") + 1)
                                        : att.Base64Content
                                ).Length
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

                string attachmentsJson = savedAttachments.Count > 0 ? JsonConvert.SerializeObject(savedAttachments) : null;

                var dt = await _database.ExecuteDataTableAsync(
                    "USP_CRM_LeadManagement",
                    new SqlParameter("@Status", "CREATE_LEAD"),
                    //new SqlParameter("@CustomerName", model.CustomerName),
                    new SqlParameter("@CustomerName", (object?)model.CustomerName?.Trim() ?? DBNull.Value),
                    new SqlParameter("@Address", (object?)model.Address?.Trim() ?? DBNull.Value),
                    new SqlParameter("@Ref_CountryId", (object?)model.Ref_CountryId ?? DBNull.Value),
                    new SqlParameter("@Ref_StateId", (object?)model.Ref_StateId ?? DBNull.Value),
                    new SqlParameter("@Ref_CityId", (object?)model.Ref_CityId ?? DBNull.Value),
                    new SqlParameter("@PinCode", (object?)model.PinCode?.Trim() ?? DBNull.Value),
                    //new SqlParameter("@Gst", (object?)model.Gst?.Trim() ?? DBNull.Value),
                    //new SqlParameter("@Pan", (object?)model.Pan?.Trim() ?? DBNull.Value),
                    new SqlParameter("@Gst", string.IsNullOrWhiteSpace(model.Gst) ? DBNull.Value : model.Gst.Trim()),
                    new SqlParameter("@Pan", string.IsNullOrWhiteSpace(model.Pan) ? DBNull.Value : model.Pan.Trim()),
                    new SqlParameter("@Contacts", (object?)contactsJson ?? DBNull.Value),
                    new SqlParameter("@Requirements", (object?)requirementsJson ?? DBNull.Value),
                    new SqlParameter("@Requirement", (object?)model.Requirement?.Trim() ?? DBNull.Value),
                    new SqlParameter("@Ref_LeadSourceId", model.Ref_LeadSourceId),
                    new SqlParameter("@LeadRemark", (object?)model.LeadRemark ?? DBNull.Value),
                    new SqlParameter("@AssignedToEmployeeId", (object?)model.AssignedToEmployeeId ?? DBNull.Value),
                    new SqlParameter("@AssignmentRemark", (object?)model.AssignmentRemark ?? DBNull.Value),
                    new SqlParameter("@Attachments", (object?)attachmentsJson ?? DBNull.Value),
                    new SqlParameter("@LeadOrganizationId", (object?)model.LeadOrganizationId ?? DBNull.Value), // ADDED fOR LEAD ORGANIZATION
                    new SqlParameter("@CreatedBy", (object?)model.CreatedBy ?? DBNull.Value)
                );

                return MapResponse(dt, isCreate: true);
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

        #region // Transfer Lead
        public async Task<ApiResponse> TransferLead(LeadTransferRequest model)
        {
            try
            {


                var dt = await _database.ExecuteDataTableAsync(
                    "USP_CRM_LeadManagement",

                    new SqlParameter("@Status", "TRANSFER_LEAD"),
                    new SqlParameter("@Ref_LeadId", model.Ref_LeadId),
                    new SqlParameter("@AssignedToEmployeeId", model.AssignedToEmployeeId),
                    new SqlParameter("@AssignmentRemark", (object?)model.AssignmentRemark ?? DBNull.Value),
                    new SqlParameter("@CreatedBy", (object?)model.CreatedBy ?? DBNull.Value)
                );

                return MapResponse(dt, isCreate: false);

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

        #region // Update Lead Status
        public async Task<ApiResponse> UpdateLeadStatus(LeadStatusUpdateRequest model)
        {
            try
            {


                var dt = await _database.ExecuteDataTableAsync(
                    "USP_CRM_LeadManagement",

                    new SqlParameter("@Status", "UPDATE_LEAD_STATUS"),
                    new SqlParameter("@Ref_LeadId", model.Ref_LeadId),
                    new SqlParameter("@Ref_LeadStatusId", model.Ref_LeadStatusId),
                    new SqlParameter("@StatusRemark", (object?)model.StatusRemark ?? DBNull.Value),
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
                    ? new
                    {
                        LeadId = Convert.ToInt32(row["LeadId"]),
                        LeadStatusId = Convert.ToInt32(row["LeadStatusId"])
                    }
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
        #endregion

        #region // Create Activity
        public async Task<ApiResponse> CreateActivity(LeadActivityRequest model)
        {
            try
            {


                List<object> savedAttachments = new();

                if (model.Attachments != null)
                {
                    foreach (var att in model.Attachments)
                    {
                        if (string.IsNullOrWhiteSpace(att.Base64) || string.IsNullOrWhiteSpace(att.FileName))
                            continue;

                        try
                        {
                            var relativePath = SaveBase64Attachment(att.Base64, att.FileName, "LeadActivityAttachments");

                            savedAttachments.Add(new
                            {
                                FileName = att.FileName,
                                FilePath = relativePath,
                                FileType = Path.GetExtension(att.FileName).TrimStart('.'),
                                FileSize = Convert.FromBase64String(att.Base64.Contains(",") ? att.Base64.Substring(att.Base64.IndexOf(",") + 1) : att.Base64).Length
                            });
                        }
                        catch (Exception ex)
                        {
                            return new ApiResponse { Status = false, Message = ex.Message };
                        }
                    }
                }

                string attachmentsMetaJson = savedAttachments.Count > 0 ? JsonConvert.SerializeObject(savedAttachments) : null;

                var dt = await _database.ExecuteDataTableAsync(
                    "USP_CRM_LeadManagement",

                    new SqlParameter("@Status", "CREATE_ACTIVITY"),
                    new SqlParameter("@Ref_LeadId", model.Ref_LeadId),
                    new SqlParameter("@Ref_ActivityTypeId", model.Ref_ActivityTypeId),
                    new SqlParameter("@Subject", model.Subject),
                    new SqlParameter("@Description", model.Description),
                    new SqlParameter("@Outcome", (object?)model.Outcome ?? DBNull.Value),
                    new SqlParameter("@ActivityDateTime", model.ActivityDateTime),
                    new SqlParameter("@DurationInMinutes", (object?)model.DurationInMinutes ?? DBNull.Value),
                    new SqlParameter("@Ref_ActivityStatusId", (object?)model.Ref_ActivityStatusId ?? DBNull.Value),
                    new SqlParameter("@Ref_NewLeadStatusId", (object?)model.Ref_NewLeadStatusId ?? DBNull.Value),
                    new SqlParameter("@Latitude", (object?)model.Latitude ?? DBNull.Value),
                    new SqlParameter("@Longitude", (object?)model.Longitude ?? DBNull.Value),
                     new SqlParameter("@LocationAddress", (object?)model.LocationAddress ?? DBNull.Value),
                    new SqlParameter("@AttachmentsMeta", (object?)attachmentsMetaJson ?? DBNull.Value),

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
                response.Data = status ? new { ActivityId = Convert.ToInt32(row["ActivityId"]) } : null;

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

        #region // Create Follow-Up
        public async Task<ApiResponse> CreateFollowUp(LeadFollowUpRequest model)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                        "USP_CRM_LeadManagement",

                        new SqlParameter("@Status", "CREATE_FOLLOWUP"),
                        new SqlParameter("@Ref_LeadId", model.Ref_LeadId),
                        new SqlParameter("@NextFollowUpDate", model.NextFollowUpDate),
                        new SqlParameter("@FollowUpRemarks", (object?)model.FollowUpRemarks ?? DBNull.Value),
                        new SqlParameter("@FollowUpAssignedEmployeeId", (object?)model.FollowUpAssignedEmployeeId ?? DBNull.Value),
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
                    ? new
                    {
                        FollowUpId = Convert.ToInt32(row["FollowUpId"]),
                        ActivityId = Convert.ToInt32(row["ActivityId"])
                    }
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
        #endregion

        #region // Update Follow-Up Status
        public async Task<ApiResponse> UpdateFollowUpStatus(FollowUpStatusUpdateRequest model)
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_CRM_LeadManagement",
                new SqlParameter("@Status", "UPDATE_FOLLOWUP_STATUS"),
                new SqlParameter("@FollowUpId", model.FollowUpId),
                new SqlParameter("@Ref_ActivityStatusId", model.Ref_ActivityStatusId),
                new SqlParameter("@NewScheduledOn", (object?)model.NewScheduledOn ?? DBNull.Value),
                new SqlParameter("@StatusRemark", (object?)model.StatusRemark ?? DBNull.Value),
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
            response.Status = Convert.ToInt32(row["Status"]) == 1;
            response.Message = row["Message"]?.ToString();
            return response;
        }
        #endregion

        #region // Get Lead Full Details
        public async Task<ApiResponse> GetLeadFullDetails(int leadId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(
                    "USP_CRM_LeadManagement",
                    new SqlParameter("@Status", "GET_LEAD_FULL_DETAILS"),
                    new SqlParameter("@Ref_LeadId", leadId)
                );

                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return new ApiResponse { Status = false, Message = "Lead not found." };
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

                    // NEW: Attachments directly in lead object (as array)
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

                var contacts = ds.Tables[1].Rows.Cast<System.Data.DataRow>().Select(r => new
                {
                    contactId = Convert.ToInt32(r["LeadContactId"]),
                    contactPerson = r["ContactPerson"]?.ToString(),
                    designation = r["DesignationName"] == DBNull.Value ? null : r["DesignationName"].ToString(),
                    mobileNo = r["MobileNo"]?.ToString(),
                    email = r["Email"] == DBNull.Value ? null : r["Email"].ToString()
                }).ToList();

                var requirements = ds.Tables[2].Rows.Cast<System.Data.DataRow>().Select(r => new
                {
                    leadRequirementId = Convert.ToInt32(r["LeadRequirementId"]),
                    offeringId = Convert.ToInt32(r["OfferingId"]),
                    offeringName = r["OfferingName"]?.ToString(),
                    remark = r["Remark"] == DBNull.Value ? null : r["Remark"].ToString(),
                    quantity = Convert.ToDecimal(r["Quantity"]),
                    unit = r["Unit"] == DBNull.Value ? null : r["Unit"].ToString()
                }).ToList();

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

                var files = ds.Tables[4].Rows.Cast<System.Data.DataRow>().Select(r => new
                {
                    attachmentId = Convert.ToInt32(r["AttachmentId"]),
                    fileName = r["FileName"]?.ToString(),
                    fileType = r["FileType"] == DBNull.Value ? null : r["FileType"].ToString(),
                    fileSize = r["FileSize"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["FileSize"]),
                    fileUrl = BuildFileUrl(r["FilePath"]?.ToString())
                }).ToList();

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

                //var timeline = ds.Tables[6].Rows.Cast<DataRow>()
                //    .Select(r => new
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
                //        createdOn = Convert.ToDateTime(r["CreatedOn"])
                //    }).ToList();

                var timeline = ds.Tables[6].Rows.Cast<DataRow>().Select(r =>
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
                                attachments = attachmentList
                                    .Select(a => new
                                    {
                                        attachmentId =Convert.ToInt32(a.AttachmentId),
                                        fileName =a.FileName?.ToString(),
                                        fileType =a.FileType?.ToString(),
                                        fileSize =a.FileSize != null? Convert.ToInt64(a.FileSize): (long?)null,
                                        fileUrl =BuildFileUrl(a.FilePath?.ToString()),
                                        uploadedOn =a.CreatedOn != null? Convert.ToDateTime(a.CreatedOn.ToString()): (DateTime?)null
                                    }).Cast<object>().ToList();
                            }
                        }
                    }

                    // Activity Expenses

                    //List<object> expenses = new List<object>();
                    //if (r["ExpensesJson"] != DBNull.Value)
                    //{
                    //    var json = r["ExpensesJson"]?.ToString();

                    //    if (!string.IsNullOrWhiteSpace(json))
                    //    {
                    //        var expenseList =JsonConvert.DeserializeObject<List<dynamic>>(json);

                    //        if (expenseList != null)
                    //        {
                    //            expenses = expenseList
                    //                .Select(e => new
                    //                {
                    //                    activityExpenseId =Convert.ToInt32(e.ActivityExpenseId),
                    //                    expenseCategoryId =Convert.ToInt32(e.Ref_ExpenseCategoryId),
                    //                    categoryCode =e.CategoryCode?.ToString(),
                    //                    categoryName =e.CategoryName?.ToString(),
                    //                    transportModeId =e.Ref_TransportModeId != null? Convert.ToInt32(e.Ref_TransportModeId): (int?)null,
                    //                    modeCode =e.ModeCode?.ToString(),
                    //                    modeName =e.ModeName?.ToString(),
                    //                    amount =e.Amount != null? Convert.ToDecimal(e.Amount): 0,
                    //                    description =e.Description?.ToString(),
                    //                    fromLocation =e.FromLocation?.ToString(),
                    //                    toLocation =e.ToLocation?.ToString(),
                    //                    expenseDate =e.ExpenseDate != null? Convert.ToDateTime(e.ExpenseDate.ToString()): (DateTime?)null,
                    //                    createdOn =e.CreatedOn != null? Convert.ToDateTime(e.CreatedOn.ToString()): (DateTime?)null
                    //                }).Cast<object>().ToList();
                    //        }
                    //    }
                    //}                    

                    // Activity Expenses
                    List<object> expenses = new List<object>();

                    if (r["ExpensesJson"] != DBNull.Value)
                    {
                        var json = r["ExpensesJson"]?.ToString();

                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var expenseList =
                                JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                            if (expenseList != null)
                            {
                                foreach (var e in expenseList)
                                {
                                    // -------------------------------------------------
                                    // Expense Attachments
                                    // -------------------------------------------------
                                    List<object> expenseAttachments = new List<object>();

                                    if (e.ContainsKey("AttachmentsJson") &&
                                        e["AttachmentsJson"] != null)
                                    {
                                        var attachmentJson =
                                            e["AttachmentsJson"]?.ToString();

                                        if (!string.IsNullOrWhiteSpace(attachmentJson))
                                        {
                                            var expenseAttachmentList =
                                                JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                                                    attachmentJson
                                                );

                                            if (expenseAttachmentList != null)
                                            {
                                                foreach (var a in expenseAttachmentList)
                                                {
                                                    expenseAttachments.Add(new
                                                    {
                                                        attachmentId =
                                                            a.ContainsKey("AttachmentId") &&
                                                            a["AttachmentId"] != null
                                                                ? Convert.ToInt32(a["AttachmentId"])
                                                                : 0,

                                                        fileName =
                                                            a.ContainsKey("FileName")
                                                                ? a["FileName"]?.ToString()
                                                                : null,

                                                        fileType =
                                                            a.ContainsKey("FileType")
                                                                ? a["FileType"]?.ToString()
                                                                : null,

                                                        fileSize =
                                                            a.ContainsKey("FileSize") &&
                                                            a["FileSize"] != null
                                                                ? Convert.ToInt64(a["FileSize"])
                                                                : (long?)null,

                                                        fileUrl =
                                                            a.ContainsKey("FilePath")
                                                                ? BuildFileUrl(
                                                                    a["FilePath"]?.ToString()
                                                                  )
                                                                : null,

                                                        uploadedOn =
                                                            a.ContainsKey("CreatedOn") &&
                                                            a["CreatedOn"] != null
                                                                ? Convert.ToDateTime(a["CreatedOn"])
                                                                : (DateTime?)null
                                                    });
                                                }
                                            }
                                        }
                                    }

                                    // -------------------------------------------------
                                    // Expense
                                    // -------------------------------------------------
                                    expenses.Add(new
                                    {
                                        activityExpenseId =
                                            e.ContainsKey("ActivityExpenseId") &&
                                            e["ActivityExpenseId"] != null
                                                ? Convert.ToInt32(e["ActivityExpenseId"])
                                                : 0,

                                        expenseCategoryId =
                                            e.ContainsKey("Ref_ExpenseCategoryId") &&
                                            e["Ref_ExpenseCategoryId"] != null
                                                ? Convert.ToInt32(e["Ref_ExpenseCategoryId"])
                                                : 0,

                                        categoryCode =
                                            e.ContainsKey("CategoryCode")
                                                ? e["CategoryCode"]?.ToString()
                                                : null,

                                        categoryName =
                                            e.ContainsKey("CategoryName")
                                                ? e["CategoryName"]?.ToString()
                                                : null,

                                        transportModeId =
                                            e.ContainsKey("Ref_TransportModeId") &&
                                            e["Ref_TransportModeId"] != null
                                                ? Convert.ToInt32(e["Ref_TransportModeId"])
                                                : (int?)null,

                                        modeCode =
                                            e.ContainsKey("ModeCode")
                                                ? e["ModeCode"]?.ToString()
                                                : null,

                                        modeName =
                                            e.ContainsKey("ModeName")
                                                ? e["ModeName"]?.ToString()
                                                : null,

                                        amount =
                                            e.ContainsKey("Amount") &&
                                            e["Amount"] != null
                                                ? Convert.ToDecimal(e["Amount"])
                                                : 0,

                                        description =
                                            e.ContainsKey("Description")
                                                ? e["Description"]?.ToString()
                                                : null,

                                        fromLocation =
                                            e.ContainsKey("FromLocation")
                                                ? e["FromLocation"]?.ToString()
                                                : null,

                                        toLocation =
                                            e.ContainsKey("ToLocation")
                                                ? e["ToLocation"]?.ToString()
                                                : null,

                                        expenseDate =
                                            e.ContainsKey("ExpenseDate") &&
                                            e["ExpenseDate"] != null
                                                ? Convert.ToDateTime(e["ExpenseDate"])
                                                : (DateTime?)null,

                                        createdOn =
                                            e.ContainsKey("CreatedOn") &&
                                            e["CreatedOn"] != null
                                                ? Convert.ToDateTime(e["CreatedOn"])
                                                : (DateTime?)null,

                                        // Expense-specific receipts
                                        attachments = expenseAttachments
                                    });
                                }
                            }
                        }
                    }
                    // Timeline object                    
                    return new
                    {
                        itemType = r["ItemType"]?.ToString(),
                        itemId =Convert.ToInt32(r["ItemId"]),
                        activityType =r["ActivityType"]?.ToString(),
                        itemDate =Convert.ToDateTime(r["ActivityDate"]),
                        subject =r["Subject"]?.ToString(),
                        description =r["Description"] == DBNull.Value? null: r["Description"].ToString(),
                        outcome =r["Outcome"] == DBNull.Value? null: r["Outcome"].ToString(),
                        locationAddress =r["LocationAddress"] == DBNull.Value? null: r["LocationAddress"].ToString(),
                        createdBy =r["CreatedBy"] == DBNull.Value? null: r["CreatedBy"].ToString(),
                        status =r["Status"]?.ToString(),
                        statusColor =r["StatusColor"]?.ToString(),
                        createdOn =Convert.ToDateTime(r["CreatedOn"]),
                        // NEW
                        attachments,
                        // NEW
                        expenses
                    };
                })
                .ToList();

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
                        files,      // Activity files (keep as is)
                        followUps,
                        timeline
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

        #region // Get Lead List
        public async Task<ApiResponse> GetLeadList(LeadListFilterRequest filter)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(
                    "USP_CRM_LeadManagement",
                    new SqlParameter("@Status", "GET_LEAD_LIST_FULL"),
                    new SqlParameter("@SearchText", (object?)filter?.SearchText ?? DBNull.Value),
                    new SqlParameter("@FilterStatusId", (object?)filter?.FilterStatusId ?? DBNull.Value),
                    new SqlParameter("@FilterAssignedToId", (object?)filter?.FilterAssignedToId ?? DBNull.Value),
                    new SqlParameter("@LoggedInEmployeeId", (object?)filter?.LoggedInEmployeeId ?? DBNull.Value)
                );

                if (ds.Tables.Count == 0)
                {
                    return new ApiResponse { Status = true, Message = "Lead list fetched successfully.", Data = new List<object>() };
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

                            // NEW: Attachments directly in lead object
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
                    Message = "Lead list fetched successfully.",
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

        #region // Close Lead
        public async Task<ApiResponse> CloseLead(LeadCloseRequest model)
        {
            var dt = await _database.ExecuteDataTableAsync("USP_CRM_LeadManagement",
                new SqlParameter("@Status", "CLOSE_LEAD"),
                new SqlParameter("@Ref_LeadId", model.Ref_LeadId),
                new SqlParameter("@Ref_LeadStatusIdClose", model.Ref_LeadStatusIdClose),
                new SqlParameter("@Ref_LostReasonId", (object?)model.Ref_LostReasonId ?? DBNull.Value),
                new SqlParameter("@ClosureRemark", (object?)model.ClosureRemark ?? DBNull.Value),
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
            response.Status = Convert.ToInt32(row["Status"]) == 1;
            response.Message = row["Message"]?.ToString();
            return response;
        }
        #endregion

        #region // Get Assigned Employee Id
        public async Task<int?> GetAssignedEmployeeId(int leadId)
        {
            var parameters = new[]
            {
                new SqlParameter("@Status", "GET_ASSIGNED_EMPLOYEE"),
                new SqlParameter("@Ref_LeadId", leadId)
            };

            DataTable dt = await _database.ExecuteDataTableAsync(
                "USP_CRM_LeadManagement",
                parameters);

            if (dt.Rows.Count == 0)
                return null;

            return dt.Rows[0]["Ref_CurrentAssignedEmployeeId"] == DBNull.Value
                ? null
                : Convert.ToInt32(dt.Rows[0]["Ref_CurrentAssignedEmployeeId"]);
        }
        #endregion

        #region // Get Lead Access Info
        public async Task<LeadAccessInfo?> GetLeadAccessInfo(int leadId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_CRM_LeadManagement",
                    new SqlParameter("@Status", "GET_LEAD_ACCESS_INFO"),
                    new SqlParameter("@Ref_LeadId", leadId));

                if (dt.Rows.Count == 0)
                    return null;

                var row = dt.Rows[0];
                return new LeadAccessInfo
                {
                    LeadId = leadId,
                    CreatedBy = row["CreatedBy"] == DBNull.Value ? 0 : Convert.ToInt32(row["CreatedBy"]),
                    AssignedEmployeeId = row["Ref_CurrentAssignedEmployeeId"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(row["Ref_CurrentAssignedEmployeeId"])
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lead access info for LeadId: {LeadId}", leadId);
                return null;
            }

        }
        #endregion

        #region // Assign Lead
        public async Task<ApiResponse> AssignLead(LeadAssignRequest model)
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_CRM_LeadManagement",
                new SqlParameter("@Status", "ASSIGN_LEAD"),
                new SqlParameter("@Ref_LeadId", model.Ref_LeadId),
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
            response.Data = status
                ? new
                {
                    LeadId = Convert.ToInt32(row["LeadId"]),
                    AssignedToEmployeeId = Convert.ToInt32(row["AssignedToEmployeeId"])
                }
                : null;

            return response;
        }
        #endregion

        #region // Check Customer Exists
        public async Task<ApiResponse> CheckCustomerExists(string? gst, string? customerName, string? pan)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(
                    "USP_CRM_LeadManagement",
                    new SqlParameter("@Status", "CHECK_CUSTOMER_EXISTS"),
                    new SqlParameter("@Gst", string.IsNullOrWhiteSpace(gst) ? (object)DBNull.Value : gst),
                    new SqlParameter("@CustomerName", string.IsNullOrWhiteSpace(customerName) ? (object)DBNull.Value : customerName),
                    new SqlParameter("@Pan", string.IsNullOrWhiteSpace(pan) ? (object)DBNull.Value : pan)
                );

                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return new ApiResponse { Status = false, Message = "Invalid response." };
                }

                bool customerExists = Convert.ToBoolean(ds.Tables[0].Rows[0]["IsCustomerExists"]);

                if (!customerExists)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = ds.Tables[0].Rows[0]["Message"].ToString()
                    };
                }

                var companyRow = ds.Tables[1].Rows[0];

                var response = new ExistingCustomerResponse
                {
                    CompanyId = Convert.ToInt32(companyRow["CompanyId"]),
                    CustomerName = companyRow["CustomerName"]?.ToString(),
                    Pan = companyRow["Pan"]?.ToString()
                };

                // Result Set 3: branches
                var branchMap = new Dictionary<int, CustomerBranchOption>();

                foreach (DataRow row in ds.Tables[2].Rows)
                {
                    var branch = new CustomerBranchOption
                    {
                        LeadOrganizationId = Convert.ToInt32(row["LeadOrganizationId"]),
                        CustomerName = row["CustomerName"]?.ToString(),
                        Address = row["Address"]?.ToString(),
                        Ref_CountryId = row["Ref_CountryId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_CountryId"]),
                        Ref_StateId = row["Ref_StateId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_StateId"]),
                        Ref_CityId = row["Ref_CityId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_CityId"]),
                        PinCode = Convert.ToInt32(row["PinCode"]),
                        Gst = row["Gst"]?.ToString(),
                        Pan = row["Pan"]?.ToString(),
                        IsExactGstMatch = Convert.ToBoolean(row["IsExactGstMatch"])
                    };

                    branchMap[branch.LeadOrganizationId] = branch;
                    response.Branches.Add(branch);
                }

                // Result Set 4: contacts, grouped into their branch
                if (ds.Tables.Count > 3)
                {
                    foreach (DataRow row in ds.Tables[3].Rows)
                    {
                        int branchId = Convert.ToInt32(row["Ref_LeadOrganizationId"]);

                        if (branchMap.TryGetValue(branchId, out var branch))
                        {
                            branch.Contacts.Add(new ExistingCustomerContact
                            {
                                LeadContactId = Convert.ToInt32(row["LeadContactId"]),
                                Ref_LeadOrganizationId = branchId,
                                ContactPerson = row["ContactPerson"]?.ToString(),
                                Mobile = row["Mobile"]?.ToString(),
                                Email = row["Email"]?.ToString(),
                                Ref_DesignationId = row["Ref_DesignationId"] == DBNull.Value
                                    ? null
                                    : Convert.ToInt32(row["Ref_DesignationId"])
                            });
                        }
                    }
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Customer found.",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Status = false, Message = ex.Message };
            }
        }
        #endregion

        #region // Search Customer
        public async Task<ApiResponse> SearchCustomer(string searchText)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_CRM_LeadManagement",
                    new SqlParameter("@Status", "SEARCH_CUSTOMER"),
                    new SqlParameter("@SearchText", searchText)
                );

                var customers = new List<CustomerSearchResponse>();

                foreach (DataRow row in dt.Rows)
                {
                    customers.Add(new CustomerSearchResponse
                    {
                        LeadOrganizationId = Convert.ToInt32(row["LeadOrganizationId"]),
                        Ref_LeadCompanyId = Convert.ToInt32(row["Ref_LeadCompanyId"]),
                        CustomerName = row["CustomerName"]?.ToString(),
                        CityName = row["CityName"]?.ToString(),
                        DisplayName = row["DisplayName"]?.ToString(),
                        Address = row["Address"]?.ToString(),
                        Gst = row["Gst"]?.ToString(),
                        Pan = row["Pan"]?.ToString()
                    });
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = customers.Count > 0 ? "Customer(s) found." : "No customer found.",
                    Data = customers
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Status = false, Message = ex.Message };
            }
        }
        #endregion

        #region // Add Activity Expenses
        //public async Task<ApiResponse> AddActivityExpenses(LeadActivityExpenseRequest model)
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

        //        if (model.Ref_LeadId <= 0)
        //        {
        //            return new ApiResponse
        //            {
        //                Status = false,
        //                Message = "Lead Id is required."
        //            };
        //        }

        //        if (model.Ref_ActivityId <= 0)
        //        {
        //            return new ApiResponse
        //            {
        //                Status = false,
        //                Message = "Activity Id is required."
        //            };
        //        }

        //        if (model.Expenses == null || model.Expenses.Count == 0)
        //        {
        //            return new ApiResponse
        //            {
        //                Status = false,
        //                Message = "At least one expense is required."
        //            };
        //        }

        //        string expensesJson = JsonConvert.SerializeObject(
        //            model.Expenses
        //        );

        //        var dt = await _database.ExecuteDataTableAsync(
        //            "USP_CRM_LeadManagement",

        //            new SqlParameter(
        //                "@Status",
        //                "CREATE_ACTIVITY_EXPENSE"
        //            ),

        //            new SqlParameter(
        //                "@Ref_LeadId",
        //                model.Ref_LeadId
        //            ),

        //            new SqlParameter(
        //                "@Ref_ActivityId",
        //                model.Ref_ActivityId
        //            ),

        //            new SqlParameter(
        //                "@ExpenseItems",
        //                (object?)expensesJson ?? DBNull.Value
        //            ),

        //            new SqlParameter(
        //                "@CreatedBy",
        //                (object?)model.CreatedBy ?? DBNull.Value
        //            )
        //        );

        //        var response = new ApiResponse();

        //        if (dt == null || dt.Rows.Count == 0)
        //        {
        //            response.Status = false;
        //            response.Message =
        //                "No response returned from database.";

        //            return response;
        //        }

        //        var row = dt.Rows[0];

        //        bool status =
        //            Convert.ToInt32(row["Status"]) == 1;

        //        response.Status = status;

        //        response.Message =
        //            row["Message"]?.ToString();

        //        if (status)
        //        {
        //            response.Data = new
        //            {
        //                LeadId = model.Ref_LeadId,

        //                ActivityId = model.Ref_ActivityId,

        //                TotalItems =
        //                    row["TotalItems"] == DBNull.Value
        //                        ? 0
        //                        : Convert.ToInt32(row["TotalItems"]),

        //                TotalAmount =
        //                    row["TotalAmount"] == DBNull.Value
        //                        ? 0
        //                        : Convert.ToDecimal(row["TotalAmount"])
        //            };
        //        }

        //        return response;
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

      

        public async Task<ApiResponse> AddActivityExpenses(LeadActivityExpenseRequest model)
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

                if (model.Ref_LeadId <= 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Lead Id is required."
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

                // Prepare expenses with saved attachment metadata   
                var expenseItems = new List<object>();
                foreach (var expense in model.Expenses)
                {
                    if (expense.Ref_ExpenseCategoryId <= 0)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Expense Category is required for every expense."
                        };
                    }

                    if (expense.Amount <= 0)
                    {
                        return new ApiResponse
                        {
                            Status = false,
                            Message = "Expense Amount must be greater than zero."
                        };
                    }                 

                    var savedExpenseAttachments = new List<object>();
                    if (expense.Attachments != null &&
                        expense.Attachments.Count > 0)
                    {
                        foreach (var attachment in expense.Attachments)
                        {
                            if (string.IsNullOrWhiteSpace(attachment.Base64) ||
                                string.IsNullOrWhiteSpace(attachment.FileName))
                            {
                                continue;
                            }

                            try
                            {
                                // Use the SAME existing attachment method
                                // already used by CreateActivity.
                                string relativePath = SaveBase64Attachment(
                                    attachment.Base64,
                                    attachment.FileName,
                                    "LeadActivityAttachments"
                                );

                                string base64Content = attachment.Base64;

                                // Remove data URI prefix if present
                                if (base64Content.Contains(","))
                                {
                                    base64Content =
                                        base64Content.Substring(
                                            base64Content.IndexOf(",") + 1
                                        );
                                }

                                int fileSize =
                                    Convert.FromBase64String(base64Content).Length;

                                savedExpenseAttachments.Add(new
                                {
                                    FileName = attachment.FileName,
                                    FilePath = relativePath,
                                    FileType = Path
                                        .GetExtension(attachment.FileName)
                                        .TrimStart('.')
                                        .ToLowerInvariant(),
                                    FileSize = fileSize
                                });
                            }
                            catch (Exception ex)
                            {
                                return new ApiResponse
                                {
                                    Status = false,
                                    Message =
                                        $"Error processing expense attachment '{attachment.FileName}': {ex.Message}"
                                };
                            }
                        }
                    }
                    
                    expenseItems.Add(new
                    {
                        Ref_ExpenseCategoryId =expense.Ref_ExpenseCategoryId,
                        Ref_TransportModeId =expense.Ref_TransportModeId,
                        Amount =expense.Amount,
                        Description =expense.Description,
                        FromLocation =expense.FromLocation,
                        ToLocation =expense.ToLocation,
                        Attachments =savedExpenseAttachments
                    });
                }
               
   
                string expensesJson =JsonConvert.SerializeObject(expenseItems);


                var dt = await _database.ExecuteDataTableAsync("USP_CRM_LeadManagement",
                    new SqlParameter("@Status","CREATE_ACTIVITY_EXPENSE"),
                    new SqlParameter("@Ref_LeadId",model.Ref_LeadId),
                    new SqlParameter("@Ref_ActivityId",model.Ref_ActivityId),
                    new SqlParameter("@ExpenseItems",(object?)expensesJson ?? DBNull.Value),
                    new SqlParameter("@CreatedBy",(object?)model.CreatedBy ?? DBNull.Value
                    )
                );

                var response = new ApiResponse();

                if (dt == null || dt.Rows.Count == 0)
                {
                    response.Status = false;
                    response.Message =
                        "No response returned from database.";

                    return response;
                }

                var row = dt.Rows[0];
                bool status =Convert.ToInt32(row["Status"]) == 1;
                response.Status = status;
                response.Message =row["Message"]?.ToString();

                if (status)
                {
                    response.Data = new
                    {
                        LeadId = model.Ref_LeadId,
                        ActivityId = model.Ref_ActivityId,
                        TotalItems =row["TotalItems"] == DBNull.Value? 0: Convert.ToInt32(row["TotalItems"]),
                        TotalAmount =row["TotalAmount"] == DBNull.Value? 0: Convert.ToDecimal(row["TotalAmount"])
                    };
                }

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

        public async Task<ApiResponse> GetActivityExpenseMasters()
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync("USP_CRM_LeadManagement",new SqlParameter("@Status", "GET_EXPENSE_MASTER_DATA"));

                if (ds == null || ds.Tables.Count < 2)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Expense master data could not be loaded."
                    };
                }

                var categories = ds.Tables[0].Rows.Cast<DataRow>()
                    .Select(r => new
                    {
                        expenseCategoryId = Convert.ToInt32(r["ExpenseCategoryId"]),
                        categoryCode = r["CategoryCode"]?.ToString(),
                        categoryName = r["CategoryName"]?.ToString(),
                        requiresTransportMode =Convert.ToBoolean(r["RequiresTransportMode"]),
                        requiresFromLocation =Convert.ToBoolean(r["RequiresFromLocation"]),
                        requiresToLocation =Convert.ToBoolean(r["RequiresToLocation"])
                    }).ToList();

                var transportModes = ds.Tables[1].Rows.Cast<DataRow>()
                    .Select(r => new
                    {
                        transportModeId =Convert.ToInt32(r["TransportModeId"]),
                        modeCode = r["ModeCode"]?.ToString(),
                        modeName = r["ModeName"]?.ToString()
                    })
                    .ToList();

                return new ApiResponse
                {
                    Status = true,
                    Message = "Expense masters fetched successfully.",
                    Data = new
                    {
                        categories,
                        transportModes
                    }
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
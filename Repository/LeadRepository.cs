using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace CRMWebServices.Repository
{
    public class LeadRepository : ILeadRepository
    {
        private readonly IDatabaseService _database;

        public LeadRepository(IDatabaseService database)
        {
            _database = database;
        }

        #region old CreateLead method (commented out)
        //public async Task<ApiResponse> CreateLead(LeadRegisterRequest model)
        //{
        //    string contactsJson = model.Contacts != null && model.Contacts.Count > 0 ? JsonSerializer.Serialize(model.Contacts)  : null;

        //    string requirementsJson = model.Requirements != null && model.Requirements.Count > 0 ? JsonSerializer.Serialize(model.Requirements): null;

        //    string savedAttachmentPath;
        //    try
        //    {
        //        savedAttachmentPath = SaveBase64Attachment(model.AttachmentBase64, model.AttachmentFileName);
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse { Status = false, Message = ex.Message };
        //    }


        //    var dt = await _database.ExecuteDataTableAsync(
        //        "USP_CRM_LeadManagement",

        //        new SqlParameter("@Status", "CREATE_LEAD"),

        //        new SqlParameter("@CustomerName", model.CustomerName),
        //        new SqlParameter("@Address", (object?)model.Address ?? DBNull.Value),
        //        new SqlParameter("@Ref_CountryId", (object?)model.Ref_CountryId ?? DBNull.Value),
        //        new SqlParameter("@Ref_StateId", (object?)model.Ref_StateId ?? DBNull.Value),
        //        new SqlParameter("@Ref_CityId", (object?)model.Ref_CityId ?? DBNull.Value),
        //        new SqlParameter("@PinCode", (object?)model.PinCode ?? DBNull.Value),
        //        new SqlParameter("@Gst", (object?)model.Gst ?? DBNull.Value),
        //        new SqlParameter("@Pan", (object?)model.Pan ?? DBNull.Value),

        //        new SqlParameter("@Contacts", (object?)contactsJson ?? DBNull.Value),
        //        new SqlParameter("@Requirements", (object?)requirementsJson ?? DBNull.Value),

        //        new SqlParameter("@Requirement", model.Requirement),
        //        new SqlParameter("@Ref_LeadSourceId", model.Ref_LeadSourceId),
        //        new SqlParameter("@LeadRemark", (object?)model.LeadRemark ?? DBNull.Value),

        //        new SqlParameter("@AssignedToEmployeeId", (object?)model.AssignedToEmployeeId ?? DBNull.Value),
        //        new SqlParameter("@AssignmentRemark", (object?)model.AssignmentRemark ?? DBNull.Value),
        //        new SqlParameter("@AttachmentPath", (object?)model.savedAttachmentPath ?? DBNull.Value),

        //        new SqlParameter("@CreatedBy", (object?)model.CreatedBy ?? DBNull.Value)
        //    );

        //    return MapResponse(dt, isCreate: true);
        //}
        #endregion

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

            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Uploads",
                folderName);

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

        public async Task<ApiResponse> CreateLead(LeadRegisterRequest model)
        {
            string contactsJson = model.Contacts != null && model.Contacts.Count > 0
                ? JsonSerializer.Serialize(model.Contacts) : null;

            string requirementsJson = model.Requirements != null && model.Requirements.Count > 0
                ? JsonSerializer.Serialize(model.Requirements) : null;

            // Decode and save attachment (if provided) before hitting the DB
            string savedAttachmentPath;
            try
            {
                //savedAttachmentPath = SaveBase64Attachment(model.AttachmentBase64, model.AttachmentFileName);
                savedAttachmentPath = SaveBase64Attachment(
                                    model.AttachmentBase64,
                                    model.AttachmentFileName,
                                    "LeadAttachments");
            }
            catch (Exception ex)
            {
                return new ApiResponse { Status = false, Message = ex.Message };
            }

            var dt = await _database.ExecuteDataTableAsync(
                "USP_CRM_LeadManagement",

                new SqlParameter("@Status", "CREATE_LEAD"),

                new SqlParameter("@CustomerName", model.CustomerName),
                new SqlParameter("@Address", (object?)model.Address ?? DBNull.Value),
                new SqlParameter("@Ref_CountryId", (object?)model.Ref_CountryId ?? DBNull.Value),
                new SqlParameter("@Ref_StateId", (object?)model.Ref_StateId ?? DBNull.Value),
                new SqlParameter("@Ref_CityId", (object?)model.Ref_CityId ?? DBNull.Value),
                new SqlParameter("@PinCode", (object?)model.PinCode ?? DBNull.Value),
                new SqlParameter("@Gst", (object?)model.Gst ?? DBNull.Value),
                new SqlParameter("@Pan", (object?)model.Pan ?? DBNull.Value),

                new SqlParameter("@Contacts", (object?)contactsJson ?? DBNull.Value),
                new SqlParameter("@Requirements", (object?)requirementsJson ?? DBNull.Value),

                new SqlParameter("@Requirement", model.Requirement),
                new SqlParameter("@Ref_LeadSourceId", model.Ref_LeadSourceId),
                new SqlParameter("@LeadRemark", (object?)model.LeadRemark ?? DBNull.Value),

                new SqlParameter("@AssignedToEmployeeId", (object?)model.AssignedToEmployeeId ?? DBNull.Value),
                new SqlParameter("@AssignmentRemark", (object?)model.AssignmentRemark ?? DBNull.Value),
                new SqlParameter("@AttachmentPath", (object?)savedAttachmentPath ?? DBNull.Value),

                new SqlParameter("@CreatedBy", (object?)model.CreatedBy ?? DBNull.Value)
            );

            return MapResponse(dt, isCreate: true);
        }      


        public async Task<ApiResponse> TransferLead(LeadTransferRequest model)
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

        private ApiResponse MapResponse(System.Data.DataTable dt, bool isCreate)
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
                        AssignedToEmployeeId = Convert.ToInt32(row["AssignedToEmployeeId"])
                    }
                    : new
                    {
                        LeadId = Convert.ToInt32(row["LeadId"]),
                        AssignedToEmployeeId = Convert.ToInt32(row["AssignedToEmployeeId"])
                    };
            }

            return response;
        }

        public async Task<ApiResponse> UpdateLeadStatus(LeadStatusUpdateRequest model)
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

        #region OLD CreateActivity method (commented out)
        //public async Task<ApiResponse> CreateActivity(LeadActivityRequest model)
        //{

        //    List<object> savedAttachments = new();

        //    if (model.Attachments != null)
        //    {
        //        foreach (var att in model.Attachments)
        //        {
        //            if (string.IsNullOrWhiteSpace(att.Base64) ||
        //                string.IsNullOrWhiteSpace(att.FileName))
        //                continue;

        //            try
        //            {
        //                var relativePath = SaveBase64Attachment(
        //                    att.Base64,
        //                    att.FileName,
        //                    "LeadActivityAttachments");

        //                savedAttachments.Add(new
        //                {
        //                    FileName = att.FileName,
        //                    FilePath = relativePath,
        //                    FileType = Path.GetExtension(att.FileName).TrimStart('.'),
        //                    FileSize = Convert.FromBase64String(
        //                        att.Base64.Contains(",")
        //                            ? att.Base64.Substring(att.Base64.IndexOf(",") + 1)
        //                            : att.Base64
        //                    ).Length
        //                });
        //            }
        //            catch (Exception ex)
        //            {
        //                return new ApiResponse
        //                {
        //                    Status = false,
        //                    Message = ex.Message
        //                };
        //            }
        //        }
        //    }

        //    string attachmentsMetaJson = savedAttachments.Count > 0 ? JsonSerializer.Serialize(savedAttachments)    : null;

        //    byte? priority = model.FollowUpPriority?.Trim().ToUpper() switch
        //    {
        //        "LOW" => 1,
        //        "MEDIUM" => 2,
        //        "HIGH" => 3,
        //        "URGENT" => 4,
        //        _ => null
        //    };

        //    var dt = await _database.ExecuteDataTableAsync(
        //        "USP_CRM_LeadManagement",

        //        new SqlParameter("@Status", "CREATE_ACTIVITY"),
        //        new SqlParameter("@Ref_LeadId", model.Ref_LeadId),
        //        new SqlParameter("@Ref_ActivityTypeId", model.Ref_ActivityTypeId),
        //        new SqlParameter("@Subject", model.Subject),
        //        new SqlParameter("@Description", model.Description),
        //        new SqlParameter("@Outcome", (object?)model.Outcome ?? DBNull.Value),
        //        new SqlParameter("@ActivityDateTime", model.ActivityDateTime),
        //        new SqlParameter("@DurationInMinutes", (object?)model.DurationInMinutes ?? DBNull.Value),

        //        new SqlParameter("@NextFollowUpDate", (object?)model.NextFollowUpDate ?? DBNull.Value),
        //        //new SqlParameter("@FollowUpPriority", (object?)model.FollowUpPriority ?? DBNull.Value),
        //        new SqlParameter("@FollowUpPriority", (object?)priority ?? DBNull.Value),
        //        //new SqlParameter( "@FollowUpPriority", model.FollowUpPriority.HasValue ? (byte)model.FollowUpPriority.Value : DBNull.Value),
        //        new SqlParameter("@FollowUpRemarks", (object?)model.FollowUpRemarks ?? DBNull.Value),
        //        new SqlParameter("@FollowUpAssignedEmployeeId", (object?)model.FollowUpAssignedEmployeeId ?? DBNull.Value),

        //        new SqlParameter("@Latitude", (object?)model.Latitude ?? DBNull.Value),
        //        new SqlParameter("@Longitude", (object?)model.Longitude ?? DBNull.Value),

        //        new SqlParameter("@AttachmentsMeta", (object?)attachmentsMetaJson ?? DBNull.Value),

        //        new SqlParameter("@CreatedBy", (object?)model.CreatedBy ?? DBNull.Value)
        //    );

        //    var response = new ApiResponse();

        //    if (dt == null || dt.Rows.Count == 0)
        //    {
        //        response.Status = false;
        //        response.Message = "No response returned from database.";
        //        return response;
        //    }

        //    var row = dt.Rows[0];
        //    bool status = Convert.ToInt32(row["Status"]) == 1;

        //    response.Status = status;
        //    response.Message = row["Message"]?.ToString();
        //    response.Data = status
        //        ? new { ActivityId = Convert.ToInt32(row["ActivityId"]) }
        //        : null;

        //    return response;
        //}
        #endregion


        public async Task<ApiResponse> CreateActivity(LeadActivityRequest model)
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
                            FileSize = Convert.FromBase64String(
                                att.Base64.Contains(",") ? att.Base64.Substring(att.Base64.IndexOf(",") + 1) : att.Base64
                            ).Length
                        });
                    }
                    catch (Exception ex)
                    {
                        return new ApiResponse { Status = false, Message = ex.Message };
                    }
                }
            }

            string attachmentsMetaJson = savedAttachments.Count > 0 ? JsonSerializer.Serialize(savedAttachments) : null;

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

        public async Task<ApiResponse> CreateFollowUp(LeadFollowUpRequest model)
        {
            byte? priority = model.FollowUpPriorityName?.Trim().ToUpper() switch
            {
                "LOW" => 1,
                "MEDIUM" => 2,
                "HIGH" => 3,
                "URGENT" => 4,
                _ => null
            };

            var dt = await _database.ExecuteDataTableAsync(
                "USP_CRM_LeadManagement",

                new SqlParameter("@Status", "CREATE_FOLLOWUP"),
                new SqlParameter("@Ref_LeadId", model.Ref_LeadId),
                new SqlParameter("@Ref_ActivityId", (object?)model.Ref_ActivityId ?? DBNull.Value),
                new SqlParameter("@NextFollowUpDate", model.NextFollowUpDate),
                new SqlParameter("@FollowUpPriority", (object?)priority ?? DBNull.Value),
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
            response.Data = status ? new { FollowUpId = Convert.ToInt32(row["FollowUpId"]) } : null;

            return response;
        }

        public async Task<ApiResponse> GetLeadFullDetails(int leadId)
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
                quantity = Convert.ToDecimal(r["Quantity"]),
                unit = r["Unit"] == DBNull.Value ? null : r["Unit"].ToString()
            }).ToList();

            var activities = ds.Tables[3].Rows.Cast<System.Data.DataRow>().Select(r => new
            {
                activityId = Convert.ToInt32(r["ActivityId"]),
                activityType = r["ActivityType"] == DBNull.Value ? null : r["ActivityType"].ToString(),
                activityDate = Convert.ToDateTime(r["ActivityDate"]),
                subject = r["Subject"]?.ToString(),
                outcome = r["Outcome"] == DBNull.Value ? null : r["Outcome"].ToString(),
                createdBy = r["CreatedBy"] == DBNull.Value ? null : r["CreatedBy"].ToString()
            }).ToList();

            var files = ds.Tables[4].Rows.Cast<System.Data.DataRow>().Select(r => new
            {
                attachmentId = Convert.ToInt32(r["AttachmentId"]),
                fileName = r["FileName"]?.ToString(),
                fileType = Path.GetExtension(r["FileName"]?.ToString() ?? "").TrimStart('.').ToUpper()
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
                    files
                }
            };
        }

        public async Task<ApiResponse> GetLeadList(LeadListFilterRequest filter)
        {
            var ds = await _database.ExecuteDataSetAsync(
                "USP_CRM_LeadManagement",
                new SqlParameter("@Status", "GET_LEAD_LIST_FULL"),
                new SqlParameter("@SearchText", (object?)filter?.SearchText ?? DBNull.Value),
                new SqlParameter("@FilterStatusId", (object?)filter?.FilterStatusId ?? DBNull.Value),
                new SqlParameter("@FilterAssignedToId", (object?)filter?.FilterAssignedToId ?? DBNull.Value)
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

            var result = new List<object>();

            foreach (var leadRow in leadRows)
            {
                int leadId = Convert.ToInt32(leadRow["LeadId"]);

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
                        outcome = r["Outcome"] == DBNull.Value ? null : r["Outcome"].ToString(),
                        createdBy = r["CreatedBy"] == DBNull.Value ? null : r["CreatedBy"].ToString()
                    }).ToList();

                var files = fileRows
                    .Where(r => Convert.ToInt32(r["LeadId"]) == leadId)
                    .Select(r => new
                    {
                        attachmentId = Convert.ToInt32(r["AttachmentId"]),
                        fileName = r["FileName"]?.ToString(),
                        filePath = r["FilePath"]?.ToString(),
                        fileType = r["FileType"] == DBNull.Value ? null : r["FileType"].ToString(),
                        fileSize = r["FileSize"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["FileSize"])
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
                    files
                });
            }

            return new ApiResponse
            {
                Status = true,
                Message = "Lead list fetched successfully.",
                Data = result
            };
        }

    }
}
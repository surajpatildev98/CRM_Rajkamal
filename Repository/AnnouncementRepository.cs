using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using CRMWebServices.Models.AnnouncementModels;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using static CRMWebServices.Models.AnnouncementModels.AnnouncementResponse;

namespace CRMWebServices.Repository
{
    public class AnnouncementRepository : IAnnouncementRepository
    {
        private readonly IDatabaseService _database;
        private readonly ILogger<AnnouncementRepository> _logger;
        private readonly ISchedulerExecutionService _schedulerExecutionService;

        private const string SP_NAME ="USP_ORG_AnnouncementManagement";

        public AnnouncementRepository(IDatabaseService database,ILogger<AnnouncementRepository> logger,ISchedulerExecutionService schedulerExecutionService)
        {
            _database = database;
            _logger = logger;
            _schedulerExecutionService = schedulerExecutionService;
        }

        #region GET ANNOUNCEMENTS
        public async Task<ApiResponse> GetAnnouncements()
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status", "GET_ANNOUNCEMENTS")
                );

                var data = new List<AnnouncementInfo>();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        data.Add(MapAnnouncement(row));
                    }
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Announcements retrieved successfully.",
                    Data = data
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,"Database error while getting announcements.");

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Unexpected error while getting announcements.");

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion


        #region GET ANNOUNCEMENT
        public async Task<ApiResponse> GetAnnouncement(int announcementId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(SP_NAME,
                    new SqlParameter("@Status", "GET_ANNOUNCEMENT"),
                    new SqlParameter("@AnnouncementId", announcementId)
                );

                if (ds == null ||
                    ds.Tables.Count == 0 ||
                    ds.Tables[0].Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Announcement not found."
                    };
                }

                var announcement = MapAnnouncement(ds.Tables[0].Rows[0]);

                var targets = new List<AnnouncementTargetInfo>();

                if (ds.Tables.Count > 1)
                {
                    targets = ds.Tables[1].AsEnumerable().Select(MapAnnouncementTarget).ToList();
                }

                announcement.Targets = targets;

                return new ApiResponse
                {
                    Status = true,
                    Message = "Announcement retrieved successfully.",
                    Data = announcement
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,"Database error while getting AnnouncementId {AnnouncementId}.",announcementId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Unexpected error while getting AnnouncementId {AnnouncementId}.",announcementId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion


        #region CREATE ANNOUNCEMENT
        public async Task<ApiResponse> CreateAnnouncement(AnnouncementRequest model,int employeeId)
        {
            try
            {
                if (model == null)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Announcement request is required."
                    };
                }

                if (string.IsNullOrWhiteSpace(model.Title))
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Announcement title is required."
                    };
                }

                if (string.IsNullOrWhiteSpace(model.Message))
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Announcement message is required."
                    };
                }

                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status","CREATE_ANNOUNCEMENT"),
                    new SqlParameter("@Title",model.Title),
                    new SqlParameter("@Message",model.Message),
                    new SqlParameter("@AttachmentPath",(object?)model.AttachmentPath ?? DBNull.Value),
                    new SqlParameter("@AttachmentName",(object?)model.AttachmentName ?? DBNull.Value),
                    new SqlParameter("@AttachmentType",(object?)model.AttachmentType ?? DBNull.Value),
                    new SqlParameter("@AttachmentSize",(object?)model.AttachmentSize ?? DBNull.Value),
                    new SqlParameter("@ScheduledOn",(object?)model.ScheduledOn ?? DBNull.Value),
                    new SqlParameter("@CreatedBy",employeeId)
                );

                return MapDatabaseResponse(dt,"Announcement created successfully.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,"Database error while creating announcement.");

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Unexpected error while creating announcement.");

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        
        #region UPDATE ANNOUNCEMENT
        public async Task<ApiResponse> UpdateAnnouncement(AnnouncementRequest model,int employeeId)
        {
            try
            {
                if (model == null)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Announcement request is required."
                    };
                }

                if (!model.AnnouncementId.HasValue)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "AnnouncementId is required."
                    };
                }

                if (string.IsNullOrWhiteSpace(model.Title))
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Announcement title is required."
                    };
                }

                if (string.IsNullOrWhiteSpace(model.Message))
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Announcement message is required."
                    };
                }

                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status","UPDATE_ANNOUNCEMENT"),
                    new SqlParameter("@AnnouncementId",model.AnnouncementId.Value),
                    new SqlParameter("@Title",model.Title),
                    new SqlParameter("@Message",model.Message),
                    new SqlParameter("@AttachmentPath",(object?)model.AttachmentPath ?? DBNull.Value),
                    new SqlParameter("@AttachmentName",(object?)model.AttachmentName ?? DBNull.Value),
                    new SqlParameter("@AttachmentType",(object?)model.AttachmentType ?? DBNull.Value),
                    new SqlParameter("@AttachmentSize",(object?)model.AttachmentSize ?? DBNull.Value),
                    new SqlParameter("@ScheduledOn",(object?)model.ScheduledOn ?? DBNull.Value),
                    new SqlParameter("@UpdatedBy",employeeId)
                );

                return MapDatabaseResponse(dt,"Announcement updated successfully.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,"Database error while updating AnnouncementId {AnnouncementId}.",model?.AnnouncementId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Unexpected error while updating AnnouncementId {AnnouncementId}.",model?.AnnouncementId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion


        #region ADD ANNOUNCEMENT TARGET
        public async Task<ApiResponse> AddAnnouncementTarget(int announcementId,AnnouncementTargetRequest model,int employeeId)
        {
            try
            {
                if (announcementId <= 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "AnnouncementId is required."
                    };
                }

                if (model == null)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Announcement target is required."
                    };
                }

                if (model.Ref_CompanyId <= 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Company is required."
                    };
                }

                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status","ADD_ANNOUNCEMENT_TARGET"),
                    new SqlParameter("@AnnouncementId",announcementId),
                    new SqlParameter("@Ref_CompanyId",model.Ref_CompanyId),
                    new SqlParameter("@Ref_RegionId",(object?)model.Ref_RegionId ?? DBNull.Value),
                    new SqlParameter("@Ref_StateId",(object?)model.Ref_StateId ?? DBNull.Value),
                    new SqlParameter("@Ref_OfficeId",(object?)model.Ref_OfficeId ?? DBNull.Value),
                    new SqlParameter("@Ref_DepartmentId",(object?)model.Ref_DepartmentId ?? DBNull.Value),
                    new SqlParameter("@SendPush", model.SendPush),
                    new SqlParameter("@SendEmail", model.SendEmail),
                    new SqlParameter("@CreatedBy",employeeId)
                );

                return MapDatabaseResponse(dt,"Announcement target added successfully.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,"Database error while adding target for AnnouncementId {AnnouncementId}.",announcementId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Unexpected error while adding target for AnnouncementId {AnnouncementId}.",announcementId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion


        #region GET ANNOUNCEMENT TARGETS
        public async Task<ApiResponse> GetAnnouncementTargets(int announcementId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status","GET_ANNOUNCEMENT_TARGETS"),
                    new SqlParameter("@AnnouncementId",announcementId)
                );

                var data = new List<AnnouncementTargetInfo>();

                if (dt != null && dt.Rows.Count > 0)
                {
                    data = dt.AsEnumerable().Select(MapAnnouncementTarget).ToList();
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Announcement targets retrieved successfully.",
                    Data = data
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,"Database error while getting targets for AnnouncementId {AnnouncementId}.",announcementId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Unexpected error while getting targets for AnnouncementId {AnnouncementId}.",announcementId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion



        #region GET ANNOUNCEMENT RECIPIENTS

        public async Task<ApiResponse> GetAnnouncementRecipients(int announcementId)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status","GET_ANNOUNCEMENT_RECIPIENTS"),
                    new SqlParameter("@AnnouncementId",announcementId)
                );

                var data = new List<AnnouncementRecipient>();

                if (dt != null && dt.Rows.Count > 0)
                {
                    data = dt.AsEnumerable().Select(MapAnnouncementRecipient).ToList();
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Announcement recipients retrieved successfully.",
                    Data = new AnnouncementRecipientResponse
                    {
                        RecipientCount = data.Count,
                        Recipients = data
                    }
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,"Database error while getting recipients for AnnouncementId {AnnouncementId}.",announcementId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Unexpected error while getting recipients for AnnouncementId {AnnouncementId}.",announcementId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        #endregion

        #region SEND ANNOUNCEMENT
        public async Task<ApiResponse> SendAnnouncement(int announcementId,int employeeId)
        {
            try
            {
                if (announcementId <= 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "AnnouncementId is required."
                    };
                }

                
                 // First get the announcement details.
                
                var announcementResponse =await GetAnnouncement(announcementId);

                if (!announcementResponse.Status ||
                    announcementResponse.Data is not AnnouncementInfo announcement)
                {
                    return announcementResponse;
                }

               
                // * Do not allow already sent announcements.
                
                if (string.Equals(announcement.Status,"SENT",StringComparison.OrdinalIgnoreCase))
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Announcement has already been sent."
                    };
                }

           
                var recipientResponse =await GetAnnouncementRecipients(announcementId);

                if (!recipientResponse.Status)
                {
                    return recipientResponse;
                }

                if (recipientResponse.Data is not AnnouncementRecipientResponse recipientData)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Invalid recipient response."
                    };
                }

                var recipients = recipientData.Recipients;

                if (recipients.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "No active employees found for this announcement."
                    };
                }

                int emailSent = 0;
                int emailFailed = 0;
                int notificationSent = 0;
                int notificationFailed = 0;

              
                foreach (var recipient in recipients)
                {
                    string employeeName =string.Join(" ",new[]
                            {
                                recipient.FirstName,
                                recipient.MiddleName,
                                recipient.LastName
                            }.Where(x => !string.IsNullOrWhiteSpace(x)));

                    string displayName = string.IsNullOrWhiteSpace(employeeName) ? "Team Member" : employeeName;


                    //try
                    //{
                    //    await _schedulerExecutionService.SendEmployeeNotificationAsync(
                    //            schedulerJobItemId: null,
                    //            employeeId: recipient.EmployeeId,
                    //            entityId: announcementId,
                    //            title: announcement.Title ?? "Announcement",
                    //            message: announcement.Message ?? string.Empty,
                    //            entityType: "ANNOUNCEMENT",
                    //            recipientType: "ANNOUNCEMENT_RECIPIENT"
                    //    );

                    //    notificationSent++;
                    //}
                    //catch (Exception ex)
                    //{
                    //    notificationFailed++;

                    //    _logger.LogError(ex,"Failed to send announcement notification. " +"AnnouncementId {AnnouncementId}, EmployeeId {EmployeeId}",announcementId,recipient.EmployeeId);
                    //}

                    if (recipient.SendPush)
                    {
                        try
                        {
                            await _schedulerExecutionService.SendEmployeeNotificationAsync(
                                schedulerJobItemId: null,
                                employeeId: recipient.EmployeeId,
                                entityId: announcementId,
                                title: announcement.Title ?? "Announcement",
                                message: announcement.Message ?? string.Empty,
                                entityType: "ANNOUNCEMENT",
                                recipientType: "ANNOUNCEMENT_RECIPIENT"
                            );

                            notificationSent++;
                        }
                        catch (Exception ex)
                        {
                            notificationFailed++;

                            _logger.LogError(
                                ex,
                                "Failed to send announcement notification. " +
                                "AnnouncementId {AnnouncementId}, EmployeeId {EmployeeId}",
                                announcementId,
                                recipient.EmployeeId);
                        }
                    }
                    if (recipient.SendEmail)
                    {
                        try
                        {
                            await _schedulerExecutionService.SendEmployeeEmailNotificationAsync(
                                schedulerJobItemId: null,
                                employeeId: recipient.EmployeeId,
                                entityId: announcementId,
                                title: announcement.Title ?? "Announcement",
                                message: announcement.Message ?? string.Empty,
                                entityType: "ANNOUNCEMENT",
                                recipientType: "ANNOUNCEMENT_RECIPIENT",
                                actionType: "ANNOUNCEMENT",
                                templateData: new Dictionary<string, string?>
                                {
                                    ["EmployeeName"] = displayName,
                                    ["AnnouncementDate"] = DateTime.Today.ToString("dd MMM yyyy"),
                                    ["HeaderLabel"] = "Company Announcement"
                                });

                            emailSent++;
                        }
                        catch (Exception ex)
                        {
                            emailFailed++;

                            _logger.LogError(
                                ex,
                                "Failed to send announcement email. " +
                                "AnnouncementId {AnnouncementId}, EmployeeId {EmployeeId}",
                                announcementId,
                                recipient.EmployeeId);
                        }
                    }
                    //try
                    //{
                    //    await _schedulerExecutionService.SendEmployeeEmailNotificationAsync(
                    //        schedulerJobItemId: null,
                    //        employeeId: recipient.EmployeeId,
                    //        entityId: announcementId,
                    //        title: announcement.Title ?? "Announcement",
                    //        message: announcement.Message ?? string.Empty,
                    //        entityType: "ANNOUNCEMENT",
                    //        recipientType: "ANNOUNCEMENT_RECIPIENT",
                    //        actionType: "ANNOUNCEMENT",
                    //        templateData: new Dictionary<string, string?>
                    //        {
                    //            ["EmployeeName"] = displayName,
                    //            ["AnnouncementDate"] = DateTime.Today.ToString("dd MMM yyyy"),
                    //            ["HeaderLabel"] = "Company Announcement"
                    //        });

                    //    emailSent++;
                    //}
                    //catch (Exception ex)
                    //{
                    //    emailFailed++;

                    //    _logger.LogError(ex, "Failed to send announcement email. AnnouncementId {AnnouncementId}, EmployeeId {EmployeeId}", announcementId, recipient.EmployeeId);
                    //}                   
                }

               
                var sentDt = await _database.ExecuteDataTableAsync(SP_NAME,
                    new SqlParameter("@Status","SEND_ANNOUNCEMENT"),
                    new SqlParameter("@AnnouncementId",announcementId),  
                    new SqlParameter("@UpdatedBy",employeeId)
                );

                var sendResponse =MapDatabaseResponse(sentDt,"Announcement sent successfully.");

                if (!sendResponse.Status)
                {
                    return sendResponse;
                }

                return new ApiResponse
                {
                    Status = true,
                    Message ="Announcement sending completed.",

                    Data = new
                    {
                        AnnouncementId = announcementId,
                        TotalRecipients = recipients.Count,
                        EmailSent = emailSent,
                        EmailFailed = emailFailed,
                        NotificationSent = notificationSent,
                        NotificationFailed = notificationFailed
                    }
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,"Database error while sending AnnouncementId {AnnouncementId}.",announcementId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Database Error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Unexpected error while sending AnnouncementId {AnnouncementId}.",announcementId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        #region GET OFFICES BY COMPANY

        public async Task<ApiResponse> GetOfficesByCompany(int companyId)
        {
            try
            {
                if (companyId <= 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "CompanyId is required."
                    };
                }

                var dt = await _database.ExecuteDataTableAsync(
                    SP_NAME,
                    new SqlParameter("@Status", "GET_OFFICES_BY_COMPANY"),
                    new SqlParameter("@CompanyId", companyId)
                );

                var data = new List<OfficeLookupInfo>();

                if (dt != null && dt.Rows.Count > 0)
                {
                    data = dt.AsEnumerable()
                             .Select(row => new OfficeLookupInfo
                             {
                                 OfficeId = Convert.ToInt32(row["OfficeId"]),

                                 OfficeName = row["OfficeName"] == DBNull.Value
                                     ? null
                                     : row["OfficeName"].ToString(),

                                 RegionId = Convert.ToInt32(row["RegionId"]),

                                 RegionName = row["RegionName"] == DBNull.Value
                                     ? null
                                     : row["RegionName"].ToString(),

                                 StateId = Convert.ToInt32(row["StateId"]),

                                 StateName = row["StateName"] == DBNull.Value
                                     ? null
                                     : row["StateName"].ToString()
                             })
                             .ToList();
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Offices retrieved successfully.",
                    Data = data
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while getting offices for CompanyId {CompanyId}.",
                    companyId);

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
                    "Unexpected error while getting offices for CompanyId {CompanyId}.",
                    companyId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        #endregion

        #region GET DEPARTMENTS BY OFFICE

        public async Task<ApiResponse> GetDepartmentsByOffice(int officeId)
        {
            try
            {
                if (officeId <= 0)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "OfficeId is required."
                    };
                }

                var dt = await _database.ExecuteDataTableAsync(
                    SP_NAME,
                    new SqlParameter("@Status", "GET_DEPARTMENTS_BY_OFFICE"),
                    new SqlParameter("@OfficeId", officeId)
                );

                var data = new List<DepartmentLookupInfo>();

                if (dt != null && dt.Rows.Count > 0)
                {
                    data = dt.AsEnumerable()
                             .Select(row => new DepartmentLookupInfo
                             {
                                 DepartmentId = Convert.ToInt32(row["DepartmentId"]),

                                 DepartmentName = row["DepartmentName"] == DBNull.Value
                                     ? null
                                     : row["DepartmentName"].ToString()
                             })
                             .ToList();
                }

                return new ApiResponse
                {
                    Status = true,
                    Message = "Departments retrieved successfully.",
                    Data = data
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while getting departments for OfficeId {OfficeId}.",
                    officeId);

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
                    "Unexpected error while getting departments for OfficeId {OfficeId}.",
                    officeId);

                return new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        #endregion

        #region MAPPING METHODS
        private AnnouncementInfo MapAnnouncement(DataRow row)
        {
            return new AnnouncementInfo
            {
                AnnouncementId =Convert.ToInt32(row["AnnouncementId"]),
                Title =row["Title"] == DBNull.Value? null: row["Title"].ToString(),
                Message =row["Message"] == DBNull.Value? null: row["Message"].ToString(),
                AttachmentPath =row["AttachmentPath"] == DBNull.Value? null: row["AttachmentPath"].ToString(),
                AttachmentName =row["AttachmentName"] == DBNull.Value? null: row["AttachmentName"].ToString(),
                AttachmentType =row["AttachmentType"] == DBNull.Value? null: row["AttachmentType"].ToString(),
                AttachmentSize =row["AttachmentSize"] == DBNull.Value? null: Convert.ToInt64(row["AttachmentSize"]),
                Status =row["Status"] == DBNull.Value? null: row["Status"].ToString(),
                ScheduledOn =row["ScheduledOn"] == DBNull.Value? null: Convert.ToDateTime(row["ScheduledOn"]),
                SentOn =row["SentOn"] == DBNull.Value? null: Convert.ToDateTime(row["SentOn"]),
                CreatedBy =row["CreatedBy"] == DBNull.Value? null: Convert.ToInt32(row["CreatedBy"]),
                CreatedOn =row["CreatedOn"] == DBNull.Value? null: Convert.ToDateTime(row["CreatedOn"]),
                UpdatedBy =row["UpdatedBy"] == DBNull.Value? null: Convert.ToInt32(row["UpdatedBy"]),
                UpdatedOn =row["UpdatedOn"] == DBNull.Value? null: Convert.ToDateTime(row["UpdatedOn"])
            };
        }
        private AnnouncementTargetInfo MapAnnouncementTarget(DataRow row)
        {
            return new AnnouncementTargetInfo
            {
                AnnouncementTargetId =Convert.ToInt32(row["AnnouncementTargetId"]),
                Ref_AnnouncementId =Convert.ToInt32(row["Ref_AnnouncementId"]),
                Ref_CompanyId =Convert.ToInt32(row["Ref_CompanyId"]),
                CompanyName =row["CompanyName"] == DBNull.Value? null: row["CompanyName"].ToString(),
                Ref_RegionId =row["Ref_RegionId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_RegionId"]),
                RegionName =row["RegionName"] == DBNull.Value? null: row["RegionName"].ToString(),
                Ref_StateId =row["Ref_StateId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_StateId"]),
                StateName =row["StateName"] == DBNull.Value? null: row["StateName"].ToString(),
                Ref_OfficeId =row["Ref_OfficeId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_OfficeId"]),
                OfficeName =row["OfficeName"] == DBNull.Value? null: row["OfficeName"].ToString(),
                Ref_DepartmentId =row["Ref_DepartmentId"] == DBNull.Value? null: Convert.ToInt32(row["Ref_DepartmentId"]),
                DepartmentName =row["DepartmentName"] == DBNull.Value? null: row["DepartmentName"].ToString(),
                SendPush = row["SendPush"] != DBNull.Value && Convert.ToBoolean(row["SendPush"]),
                SendEmail = row["SendEmail"] != DBNull.Value && Convert.ToBoolean(row["SendEmail"])
            };
        }
        private AnnouncementRecipient MapAnnouncementRecipient(DataRow row)
        {
            return new AnnouncementRecipient
            {
                EmployeeId =Convert.ToInt32(row["EmployeeId"]),
                EmployeeCode =row["EmployeeCode"] == DBNull.Value? null: row["EmployeeCode"].ToString(),
                FirstName =row["FirstName"] == DBNull.Value? null: row["FirstName"].ToString(),
                MiddleName =row["MiddleName"] == DBNull.Value? null: row["MiddleName"].ToString(),
                LastName =row["LastName"] == DBNull.Value? null: row["LastName"].ToString(),
                OfficialEmail =row["OfficialEmail"] == DBNull.Value? null: row["OfficialEmail"].ToString(),
                CompanyId =Convert.ToInt32(row["CompanyId"]),
                CompanyName =row["CompanyName"] == DBNull.Value? null: row["CompanyName"].ToString(),
                RegionId =Convert.ToInt32(row["RegionId"]),
                RegionName =row["RegionName"] == DBNull.Value? null: row["RegionName"].ToString(),
                StateId =Convert.ToInt32(row["StateId"]),
                StateName =row["StateName"] == DBNull.Value? null: row["StateName"].ToString(),
                OfficeId =Convert.ToInt32(row["OfficeId"]),
                OfficeName =row["OfficeName"] == DBNull.Value? null: row["OfficeName"].ToString(),
                DepartmentId =row["DepartmentId"] == DBNull.Value? null: Convert.ToInt32(row["DepartmentId"]),
                DepartmentName =row["DepartmentName"] == DBNull.Value? null: row["DepartmentName"].ToString(),
                SendPush = row["SendPush"] != DBNull.Value&& Convert.ToBoolean(row["SendPush"]),
                SendEmail = row["SendEmail"] != DBNull.Value&& Convert.ToBoolean(row["SendEmail"])
            };
        }
        private ApiResponse MapDatabaseResponse(DataTable? dt,string defaultMessage)
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

            bool status = true;

            if (row.Table.Columns.Contains("Status") &&
                row["Status"] != DBNull.Value)
            {
                var statusValue = row["Status"];

                // Numeric status: 1 = success, 0 = failure
                if (statusValue is int ||
                    statusValue is long ||
                    statusValue is short ||
                    statusValue is byte)
                {
                    status = Convert.ToInt32(statusValue) == 1;
                }
                else
                {
                    // Announcement Status values such as:
                    // DRAFT, SCHEDULED, SENT, EXPIRED
                    string statusText = statusValue.ToString() ?? string.Empty;

                    status = !string.Equals(statusText,"FAILED",StringComparison.OrdinalIgnoreCase);
                }
            }

            string message =row.Table.Columns.Contains("Message") && row["Message"] != DBNull.Value? row["Message"].ToString()!: defaultMessage;

            if (!status)
            {
                return new ApiResponse
                {
                    Status = false,
                    Message = message
                };
            }

            var data = new Dictionary<string, object?>();

            foreach (DataColumn column in row.Table.Columns)
            {
                if (column.ColumnName.Equals("Status",StringComparison.OrdinalIgnoreCase) ||
                    column.ColumnName.Equals("Message",StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                data[column.ColumnName] =row[column] == DBNull.Value? null: row[column];
            }

            return new ApiResponse
            {
                Status = true,
                Message = message,
                Data = data.Count > 0 ? data : null
            };
        }
        #endregion
    }
}
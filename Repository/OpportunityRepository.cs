using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using static CRMWebServices.Models.OpportunityModels;

namespace CRMWebServices.Repository
{
    public class OpportunityRepository : IOpportunityRepository
    {
        private readonly IDatabaseService _database;

        public OpportunityRepository(IDatabaseService database)
        {
            _database = database;
        }

        #region  GET WON LEADS (LIST)
        public async Task<ApiResponse> GetWonLeads(string searchText = null)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "GET_WON_LEADS"),
                    new SqlParameter("@SearchText", (object?)searchText ?? DBNull.Value)
                );

                if (dt == null || dt.Rows.Count == 0)
                {
                    return new ApiResponse
                    {
                        Status = true,
                        Message = "No Won leads available for conversion.",
                        Data = new List<WonLeadResponse>()
                    };
                }

                var result = new List<WonLeadResponse>();
                foreach (DataRow row in dt.Rows)
                {
                    result.Add(new WonLeadResponse
                    {
                        LeadId = Convert.ToInt32(row["LeadId"]),
                        LeadNo = row["LeadNo"]?.ToString(),
                        LeadTitle = row["LeadTitle"]?.ToString(),
                        CompanyName = row["CompanyName"]?.ToString(),
                        ExpectedValue = row["ExpectedValue"] == DBNull.Value ? null : Convert.ToDecimal(row["ExpectedValue"]),
                        CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                        AssignedToEmployeeId = row["AssignedToEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(row["AssignedToEmployeeId"]),
                        AssignedToEmployee = row["AssignedToEmployee"]?.ToString(),
                        LeadStatus = row["LeadStatus"]?.ToString(),
                        IsConverted = Convert.ToBoolean(row["IsConverted"])
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


        #region GET WON LEAD DETAILS (SINGLE - READ-ONLY)
        public async Task<ApiResponse> GetWonLeadDetails(int leadId)
        {
            try
            {
                var ds = await _database.ExecuteDataSetAsync(
                    "USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "GET_WON_LEAD_DETAILS"),
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

                // Build Lead Info
                var leadInfo = new LeadInfo
                {
                    LeadId = Convert.ToInt32(leadRow["LeadId"]),
                    LeadNo = leadRow["LeadNo"]?.ToString(),
                    LeadTitle = leadRow["LeadTitle"]?.ToString(),
                    ExpectedValue = leadRow["ExpectedValue"] == DBNull.Value ? null : Convert.ToDecimal(leadRow["ExpectedValue"]),
                    LeadRemark = leadRow["LeadRemark"] == DBNull.Value ? null : leadRow["LeadRemark"].ToString(),
                    CreatedOn = Convert.ToDateTime(leadRow["CreatedOn"]),
                    AssignedToEmployeeId = leadRow["AssignedToEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(leadRow["AssignedToEmployeeId"]),
                    AssignedToEmployee = leadRow["AssignedToEmployee"]?.ToString(),
                    LeadStatus = leadRow["LeadStatus"]?.ToString(),
                    LeadSource = leadRow["LeadSource"] == DBNull.Value ? null : leadRow["LeadSource"].ToString(),
                    IsConverted = Convert.ToBoolean(leadRow["IsConverted"]),
                    Organization = new LeadOrganizationInfo
                    {
                        LeadOrganizationId = Convert.ToInt32(leadRow["LeadOrganizationId"]),
                        CompanyName = leadRow["CompanyName"]?.ToString(),
                        Address = leadRow["Address"] == DBNull.Value ? null : leadRow["Address"].ToString(),
                        CountryName = leadRow["CountryName"] == DBNull.Value ? null : leadRow["CountryName"].ToString(),
                        StateName = leadRow["StateName"] == DBNull.Value ? null : leadRow["StateName"].ToString(),
                        CityName = leadRow["CityName"] == DBNull.Value ? null : leadRow["CityName"].ToString(),
                        PinCode = leadRow["PinCode"] == DBNull.Value ? null : leadRow["PinCode"].ToString(),
                        GstNo = leadRow["GstNo"] == DBNull.Value ? null : leadRow["GstNo"].ToString(),
                        PanNo = leadRow["PanNo"] == DBNull.Value ? null : leadRow["PanNo"].ToString()
                    }
                };

                // Contacts
                var contacts = new List<LeadContactInfo>();
                if (ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[1].Rows)
                    {
                        contacts.Add(new LeadContactInfo
                        {
                            LeadContactId = Convert.ToInt32(row["LeadContactId"]),
                            ContactPerson = row["ContactPerson"]?.ToString(),
                            DesignationName = row["DesignationName"] == DBNull.Value ? null : row["DesignationName"].ToString(),
                            Mobile = row["Mobile"]?.ToString(),
                            Email = row["Email"] == DBNull.Value ? null : row["Email"].ToString()
                        });
                    }
                }

                // Requirements
                var requirements = new List<LeadRequirementInfo>();
                if (ds.Tables.Count > 2 && ds.Tables[2].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[2].Rows)
                    {
                        requirements.Add(new LeadRequirementInfo
                        {
                            LeadRequirementId = Convert.ToInt32(row["LeadRequirementId"]),
                            OfferingName = row["OfferingName"]?.ToString(),
                            RequirementRemark = row["RequirementRemark"] == DBNull.Value ? null : row["RequirementRemark"].ToString(),
                            Quantity = row["Quantity"] == DBNull.Value ? null : Convert.ToInt32(row["Quantity"]),
                            Unit = row["Unit"] == DBNull.Value ? null : row["Unit"].ToString()
                        });
                    }
                }

                // Attachments
                var attachments = new List<LeadAttachmentInfo>();
                if (ds.Tables.Count > 3 && ds.Tables[3].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[3].Rows)
                    {
                        attachments.Add(new LeadAttachmentInfo
                        {
                            AttachmentId = Convert.ToInt32(row["AttachmentId"]),
                            FileName = row["FileName"]?.ToString(),
                            FilePath = row["FilePath"]?.ToString(),
                            FileType = row["FileType"] == DBNull.Value ? null : row["FileType"].ToString(),
                            FileSize = row["FileSize"] == DBNull.Value ? null : Convert.ToInt64(row["FileSize"]),
                            CreatedOn = Convert.ToDateTime(row["CreatedOn"])
                        });
                    }
                }

                // Activities
                var activities = new List<LeadActivityInfo>();
                if (ds.Tables.Count > 4 && ds.Tables[4].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[4].Rows)
                    {
                        activities.Add(new LeadActivityInfo
                        {
                            ActivityId = Convert.ToInt32(row["ActivityId"]),
                            ActivityType = row["ActivityType"]?.ToString(),
                            ActivityDate = Convert.ToDateTime(row["ActivityDate"]),
                            Subject = row["Subject"]?.ToString(),
                            Description = row["Description"] == DBNull.Value ? null : row["Description"].ToString(),
                            Outcome = row["Outcome"] == DBNull.Value ? null : row["Outcome"].ToString(),
                            CreatedBy = row["CreatedBy"]?.ToString(),
                            ActivityStatus = row["ActivityStatus"] == DBNull.Value ? null : row["ActivityStatus"].ToString(),
                            ActivityStatusColor = row["ActivityStatusColor"] == DBNull.Value ? null : row["ActivityStatusColor"].ToString()
                        });
                    }
                }

                var responseData = new WonLeadDetailResponse
                {
                    Lead = leadInfo,
                    Contacts = contacts,
                    Requirements = requirements,
                    Attachments = attachments,
                    Activities = activities
                };

                return new ApiResponse
                {
                    Status = true,
                    Message = "Lead details fetched successfully.",
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


        #region  // CONVERT LEAD TO OPPORTUNITY
        public async Task<ApiResponse> ConvertLeadToOpportunity(ConvertLeadRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_CRM_OpportunityManagement",
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
                    var data = new ConvertLeadResponse
                    {
                        OpportunityId = Convert.ToInt32(row["OpportunityId"]),
                        CustomerId = Convert.ToInt32(row["CustomerId"]),
                        OpportunityNo = row["OpportunityNo"]?.ToString()
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
            var dt = await _database.ExecuteDataTableAsync(
                "USP_CRM_OpportunityManagement",
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
            response.Data = status
                ? new
                {
                    OpportunityId = Convert.ToInt32(row["OpportunityId"]),
                    AssignedToEmployeeId = Convert.ToInt32(row["AssignedToEmployeeId"])
                }
                : null;

            return response;
        }
        #endregion


        #region // GET OPPORTUNITY LIST (MULTIPLE - READ-ONLY)
        public async Task<ApiResponse> GetOpportunityList(OpportunityListRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_CRM_OpportunityManagement",
                    new SqlParameter("@Status", "GET_OPPORTUNITY_LIST"),
                    new SqlParameter("@SearchText", (object?)request.SearchText ?? DBNull.Value),
                    new SqlParameter("@FilterStageId", (object?)request.FilterStageId ?? DBNull.Value),
                    new SqlParameter("@FilterOwnerId", (object?)request.FilterOwnerId ?? DBNull.Value),
                    new SqlParameter("@FilterTypeId", (object?)request.FilterTypeId ?? DBNull.Value)
                );

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
                var ds = await _database.ExecuteDataSetAsync(
                    "USP_CRM_OpportunityManagement",
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

                // Build Opportunity Info
                var opportunityInfo = new OpportunityInfo
                {
                    OpportunityId = Convert.ToInt32(row["OpportunityId"]),
                    OpportunityNo = row["OpportunityNo"]?.ToString(),
                    Ref_LeadId = row["Ref_LeadId"] == DBNull.Value ? null : Convert.ToInt32(row["Ref_LeadId"]),
                    OpportunityName = row["OpportunityName"]?.ToString(),
                    ExpectedRevenue = row["ExpectedRevenue"] == DBNull.Value ? null : Convert.ToDecimal(row["ExpectedRevenue"]),
                    ExpectedCloseDate = row["ExpectedCloseDate"] == DBNull.Value ? null : Convert.ToDateTime(row["ExpectedCloseDate"]),
                    Probability = row["Probability"] == DBNull.Value ? null : Convert.ToInt32(row["Probability"]),
                    CompetitorName = row["CompetitorName"] == DBNull.Value ? null : row["CompetitorName"].ToString(),
                    OpportunityRemark = row["OpportunityRemark"] == DBNull.Value ? null : row["OpportunityRemark"].ToString(),
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
                        OpportunityTypeId = row["OpportunityTypeId"] == DBNull.Value ? null : Convert.ToInt32(row["OpportunityTypeId"]),
                        OpportunityTypeName = row["OpportunityTypeName"]?.ToString()
                    },

                    OpportunityStage = new OpportunityStageInfo
                    {
                        OpportunityStageId = row["OpportunityStageId"] == DBNull.Value ? null : Convert.ToInt32(row["OpportunityStageId"]),
                        OpportunityStage = row["OpportunityStage"]?.ToString(),
                        IsClosed = Convert.ToBoolean(row["IsClosed"]),
                        IsWon = Convert.ToBoolean(row["IsWon"]),
                        IsLost = Convert.ToBoolean(row["IsLost"])
                    },

                    Priority = new PriorityInfo
                    {
                        PriorityId = row["PriorityId"] == DBNull.Value ? null : Convert.ToInt32(row["PriorityId"]),
                        PriorityName = row["PriorityName"]?.ToString(),
                        PriorityLevel = row["PriorityLevel"] == DBNull.Value ? null : Convert.ToInt32(row["PriorityLevel"]),
                        //ColorCode = row["ColorCode"]?.ToString()
                    },

                    Owner = new OwnerInfo
                    {
                        OwnerEmployeeId = row["OwnerEmployeeId"] == DBNull.Value ? null : Convert.ToInt32(row["OwnerEmployeeId"]),
                        OwnerEmployeeName = row["OwnerEmployeeName"]?.ToString()
                    }
                };

                // Contacts
                var contacts = new List<CustomerContactInfo>();
                if (ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0)
                {
                    foreach (DataRow contactRow in ds.Tables[1].Rows)
                    {
                        contacts.Add(new CustomerContactInfo
                        {
                            CustomerContactId = Convert.ToInt32(contactRow["CustomerContactId"]),
                            ContactPerson = contactRow["ContactPerson"]?.ToString(),
                            DesignationName = contactRow["DesignationName"] == DBNull.Value ? null : contactRow["DesignationName"].ToString(),
                            Mobile = contactRow["Mobile"]?.ToString(),
                            Email = contactRow["Email"] == DBNull.Value ? null : contactRow["Email"].ToString(),
                            IsPrimary = Convert.ToBoolean(contactRow["IsPrimary"])
                        });
                    }
                }

                var responseData = new OpportunityDetailResponse
                {
                    Opportunity = opportunityInfo,
                    Contacts = contacts
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


        #region  // UPDATE OPPORTUNITY
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


        #region // DELETE OPPORTUNITY
        public async Task<ApiResponse> DeleteOpportunity(DeleteOpportunityRequest request)
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_CRM_OpportunityManagement",
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


        #region  DROPDOWNS
        public async Task<ApiResponse> GetOpportunityTypes()
        {
            try
            {
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_CRM_OpportunityManagement",
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
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_CRM_OpportunityManagement",
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
                var dt = await _database.ExecuteDataTableAsync(
                    "USP_CRM_OpportunityManagement",
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
    }
}
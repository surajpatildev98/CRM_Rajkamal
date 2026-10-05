using System;
using System.Collections.Generic;

namespace CRMWebServices.Models
{
    public class SchedulerModels
    {
        
        // MASTER DATA       

        public class SchedulerEntityTypeResponse
        {
            public string EntityType { get; set; } = string.Empty;
        }

        public class SchedulerScopeResponse
        {
            public string ScopeCode { get; set; } = string.Empty;
            public string ScopeName { get; set; } = string.Empty;
        }

        // CREATE JOB       

        //public class CreateSchedulerJobRequest
        //{
        //    public string JobCode { get; set; } = string.Empty;
        //    public string JobName { get; set; } = string.Empty;
        //    public string JobType { get; set; } = string.Empty;
        //    public string? Description { get; set; }
        //    public string EntityType { get; set; } = string.Empty;
        //    public string ScopeCode { get; set; } = string.Empty;
        //    public bool IsActive { get; set; } = true;
        //    public string FrequencyType { get; set; } = string.Empty;
        //    public int FrequencyValue { get; set; }
        //    public DateTime? StartDate { get; set; }
        //    public DateTime? EndDate { get; set; }
        //    public int? MaxAttempts { get; set; }
        //    public int? StopAfterMinutes { get; set; }
        //}

        public class CreateSchedulerJobRequest
        {
            public string? JobCode { get; set; }
            public string? JobName { get; set; }

            public string? JobType { get; set; } 
            public string? Description { get; set; }

            public string EntityType { get; set; } = string.Empty;
            public string ScopeCode { get; set; } = string.Empty;

            public bool IsActive { get; set; } = true;

            public string FrequencyType { get; set; } = string.Empty;
            public int FrequencyValue { get; set; }

            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }

            public int? MaxAttempts { get; set; }
            public int? StopAfterMinutes { get; set; }
        }

        // UPDATE JOB       

        public class UpdateSchedulerJobRequest
        {
            public int SchedulerJobId { get; set; }
            public string JobName { get; set; } = string.Empty;
            public string JobType { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string EntityType { get; set; } = string.Empty;
            public string ScopeCode { get; set; } = string.Empty;
            public bool? IsActive { get; set; }
            public string FrequencyType { get; set; } = string.Empty;
            public int FrequencyValue { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public int? MaxAttempts { get; set; }
            public int? StopAfterMinutes { get; set; }
        }
        
        // JOB LIST       

        public class SchedulerJobListResponse
        {
            public int SchedulerJobId { get; set; }
            public string JobCode { get; set; } = string.Empty;
            public string JobName { get; set; } = string.Empty;
            public string JobType { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string EntityType { get; set; } = string.Empty;
            public string ScopeCode { get; set; } = string.Empty;
            public string? ScopeName { get; set; }
            public bool IsActive { get; set; }
            public string FrequencyType { get; set; } = string.Empty;
            public int FrequencyValue { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public int? MaxAttempts { get; set; }
            public int? StopAfterMinutes { get; set; }
            public DateTime? NextRunOn { get; set; }
            public DateTime? LastRunOn { get; set; }
            public bool IsRunning { get; set; }
            public DateTime CreatedOn { get; set; }
            public int? CreatedBy { get; set; }
            public DateTime? UpdatedOn { get; set; }
            public int? UpdatedBy { get; set; }
        }
        
        // JOB DETAILS       

        public class SchedulerJobDetailsResponse
        {
            public int SchedulerJobId { get; set; }
            public string JobCode { get; set; } = string.Empty;
            public string JobName { get; set; } = string.Empty;
            public string JobType { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string EntityType { get; set; } = string.Empty;
            public string ScopeCode { get; set; } = string.Empty;
            public string? ScopeName { get; set; }
            public bool IsActive { get; set; }
            public string FrequencyType { get; set; } = string.Empty;
            public int FrequencyValue { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public int? MaxAttempts { get; set; }
            public int? StopAfterMinutes { get; set; }
            public DateTime? NextRunOn { get; set; }
            public DateTime? LastRunOn { get; set; }
            public bool IsRunning { get; set; }
            public DateTime CreatedOn { get; set; }
            public int? CreatedBy { get; set; }
            public DateTime? UpdatedOn { get; set; }
            public int? UpdatedBy { get; set; }
        }
    }
}
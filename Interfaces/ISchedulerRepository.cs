using System.Threading.Tasks;
using CRMWebServices.Helpers;
using static CRMWebServices.Models.SchedulerModels;

namespace CRMWebServices.Interfaces
{
    public interface ISchedulerRepository
    {
        Task<ApiResponse> GetSchedulerEntityTypes();
        Task<ApiResponse> GetSchedulerScopes(string entityType);
        Task<ApiResponse> GetJobs();
        Task<ApiResponse> GetJob(int schedulerJobId);
        Task<ApiResponse> CreateJob(CreateSchedulerJobRequest model,int createdBy);
        Task<ApiResponse> UpdateJob(UpdateSchedulerJobRequest model,int updatedBy);
        Task<ApiResponse> EnableJob(int schedulerJobId,int updatedBy);
        Task<ApiResponse> DisableJob(int schedulerJobId,int updatedBy);
    }
}
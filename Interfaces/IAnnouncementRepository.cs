using CRMWebServices.Helpers;
using CRMWebServices.Models.AnnouncementModels;

namespace CRMWebServices.Interfaces
{
    public interface IAnnouncementRepository
    {
        Task<ApiResponse> GetAnnouncements();
        Task<ApiResponse> GetAnnouncement(int announcementId);
        Task<ApiResponse> CreateAnnouncement(AnnouncementRequest model,int employeeId);
        Task<ApiResponse> UpdateAnnouncement(AnnouncementRequest model,int employeeId);
        Task<ApiResponse> AddAnnouncementTarget(int announcementId,AnnouncementTargetRequest model,int employeeId);
        Task<ApiResponse> GetAnnouncementTargets(int announcementId);
        Task<ApiResponse> GetAnnouncementRecipients(int announcementId);
        Task<ApiResponse> SendAnnouncement(int announcementId,int employeeId);

        Task<ApiResponse> GetOfficesByCompany(int companyId);

        Task<ApiResponse> GetDepartmentsByOffice(int officeId);
    }
}
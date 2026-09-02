using CRMWebServices.Helpers;
using System.Data;
using static CRMWebServices.Models.LogoutModels;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Interfaces
{
    public interface IAuthRepository
    {
        //Task<DataRow> GetEmployeeForLogin(string loginId);

        // IAuthRepository.cs
        Task<(DataRow? EmployeeRow, List<ModuleAccessInfo> Modules)> GetEmployeeForLogin(string loginId);

        Task<ApiResponse> RegisterEmployeeDevice(int employeeId,RegisterEmployeeDeviceRequest request);

        Task<ActiveDeviceInfo?> CheckActiveDevice(int employeeId,string deviceId);

        Task<ApiResponse> ForceLogoutDevice(int employeeId,string deviceId);

        Task<ApiResponse> Logout(int employeeId,string deviceId);

    }
}

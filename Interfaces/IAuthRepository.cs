using CRMWebServices.Helpers;
using System.Data;
using static CRMWebServices.Models.LogoutModels;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Interfaces
{
    public interface IAuthRepository
    {
        
        Task<(DataRow? EmployeeRow, List<SuperModuleAccessInfo> SuperModules)> GetEmployeeForLogin(string loginId);
        Task<ApiResponse> RegisterEmployeeDevice(int employeeId,RegisterEmployeeDeviceRequest request);
        Task<ActiveDeviceInfo?> CheckActiveDevice(int employeeId,string deviceId);
        Task<ApiResponse> ForceLogoutDevice(int employeeId,string deviceId);
        Task<ApiResponse> Logout(int employeeId,string deviceId);
        Task<ApiResponse> GenerateResetOtp(int employeeId, string otpHash);
        Task<DataRow?> GetResetOtp(int employeeId);
        Task<ApiResponse> IncrementOtpAttempt(int employeeId);
        Task<ApiResponse> VerifyResetOtp(int employeeId);
        Task<ApiResponse> ResetPassword(int employeeId,Guid resetToken,string newPasswordHash);
        Task<int?> GetResetEmployeeByToken(Guid resetToken);
    }
}

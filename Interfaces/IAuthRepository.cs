using CRMWebServices.Helpers;
using System.Data;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Interfaces
{
    public interface IAuthRepository
    {
        //Task<DataRow> GetEmployeeForLogin(string loginId);

        // IAuthRepository.cs
        Task<(DataRow? EmployeeRow, List<ModuleAccessInfo> Modules)> GetEmployeeForLogin(string loginId);

        Task<ApiResponse> RegisterEmployeeDevice(int employeeId,RegisterEmployeeDeviceRequest request);


        Task<ApiResponse> GenerateResetOtp(int employeeId, string otpHash);

        Task<DataRow?> GetResetOtp(int employeeId);

        Task<ApiResponse> IncrementOtpAttempt(int employeeId);

        Task<ApiResponse> VerifyResetOtp(int employeeId);

        Task<ApiResponse> ResetPassword(int employeeId,Guid resetToken,string newPasswordHash);
        Task<int?> GetResetEmployeeByToken(Guid resetToken);
    }
}

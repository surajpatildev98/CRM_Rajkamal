using CRMWebServices.Helpers;
using static CRMWebServices.Models.Employee_Probation.EmployeeProbationModels;

namespace CRMWebServices.Interfaces
{
    public interface IEmployeeProbationRepository
    {

        Task<ApiResponse> GetProbationEmployees();
        Task<ApiResponse> GetEmployeeProbation(int employeeId);
        Task<ApiResponse> GetProbationHistory(int employeeId);
        Task<ApiResponse> ExtendProbation(EmployeeProbationManagementRequest model,int employeeId);
        Task<ApiResponse> ConfirmProbation(EmployeeProbationManagementRequest model,int employeeId);
        Task<ApiResponse> FailProbation(EmployeeProbationManagementRequest model,int employeeId);
        Task<ApiResponse> CancelProbation(EmployeeProbationManagementRequest model,int employeeId);
    }
}

using System.Data;
using static CRMWebServices.Models.PermissionModels;

namespace CRMWebServices.Interfaces
{
    public interface IAuthRepository
    {
        //Task<DataRow> GetEmployeeForLogin(string loginId);

        // IAuthRepository.cs
        Task<(DataRow? EmployeeRow, List<ModuleAccessInfo> Modules)> GetEmployeeForLogin(string loginId);
    }
}

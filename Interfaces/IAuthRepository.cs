using System.Data;

namespace CRMWebServices.Interfaces
{
    public interface IAuthRepository
    {
        Task<DataRow> GetEmployeeForLogin(string loginId);
    }
}

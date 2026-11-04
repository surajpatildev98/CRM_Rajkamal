using CRMWebServices.Models;

namespace CRMWebServices.Interfaces
{
    public interface IPayslipRepository
    {
        // ----- employee (own, published only) -----
        Task<PayslipListResponse> GetMyPayslips(int loggedInEmployeeId);
        Task<PayslipDetailResponse> GetPayslip(int loggedInEmployeeId, int payslipId);

        // ----- HR -----
        Task<PayslipListResponse> GetHrPayslips(PayslipMonthRequest request);
        Task<PayslipActionResponse> SavePayslip(SavePayslipRequest request);
        Task<PayslipActionResponse> PublishPayslips(PayslipMonthRequest request);
        Task<PayslipActionResponse> UnpublishPayslip(int payslipId);
        Task<PayslipActionResponse> DeletePayslip(int payslipId);
    }
}

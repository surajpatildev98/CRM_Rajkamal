using CRMWebServices.Helpers;
using CRMWebServices.Models;
using CRMWebServices.Models.Shift;

namespace CRMWebServices.Interfaces
{
    public interface IShiftRepository
    {
        Task<ShiftResponse> GetShifts();
        Task<ApiResponse> AddShift(ShiftModel model);
        Task<ShiftResponse> UpdateShift(ShiftModel shift);
        Task<ApiResponse> DeleteShift(int shiftId);
    }
}

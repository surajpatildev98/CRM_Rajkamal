using CRMWebServices.Helpers;
using CRMWebServices.Models.holiday;

namespace CRMWebServices.Interfaces
{
    public interface IHolidayRepository
    {
        Task<ApiResponse> SaveHolidayCalendar(HolidayCalendarSaveRequest request);
        Task<HolidayCalendarResponse> GetCalendars();
        Task<HolidayResponse> GetHolidays(int holidayCalendarId);
    }
}

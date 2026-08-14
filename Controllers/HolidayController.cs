using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models.holiday;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRMWebServices.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/holiday")]
    public class HolidayController : ControllerBase
    {
        private readonly IHolidayRepository _holidayRepository;

        public HolidayController(
            IHolidayRepository holidayRepository)
        {
            _holidayRepository = holidayRepository;
        }

        [HttpPost("SaveCalendar")]
        public async Task<IActionResult> SaveCalendar(
           [FromBody] HolidayCalendarSaveRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new
                    {
                        Status = false,
                        Message = "Invalid request."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.CalendarName))
                {
                    return BadRequest(new
                    {
                        Status = false,
                        Message = "Calendar name is required."
                    });
                }

                if (request.Holidays == null || request.Holidays.Count == 0)
                {
                    return BadRequest(new
                    {
                        Status = false,
                        Message = "At least one holiday is required."
                    });
                }

                var response = await _holidayRepository.SaveHolidayCalendar(request);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpGet("GetCalendars")]
        public async Task<IActionResult> GetCalendars()
        {
            try
            {
                var response = await _holidayRepository.GetCalendars();

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse { Status = false, Message = ex.Message });
            }
        }

        [HttpGet("GetHolidays/{holidayCalendarId}")]
        public async Task<IActionResult> GetHolidays(int holidayCalendarId)
        {
            try
            {
                if (holidayCalendarId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Holiday calendar ID is required."
                    });
                }

                var response =
                    await _holidayRepository.GetHolidays(holidayCalendarId);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new ApiResponse
                    {
                        Status = false,
                        Message = ex.Message
                    });
            }
        }
    }
}

using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRMWebServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportsRepository _reportsRepository;

        public ReportsController(IReportsRepository reportsRepository)
        {
            _reportsRepository = reportsRepository;
        }

        [HttpPost("dashboard")]
        public async Task<IActionResult> GetDashboard([FromBody] ReportModels.CRMReportFilterRequest request)
        {
            var response = await _reportsRepository.GetDashboard(request);

            return Ok(response);
        }       

        [HttpPost("journey")]
        public async Task<IActionResult> GetJourney([FromBody] ReportModels.CRMReportJourneyRequest request)
        {
            var response =await _reportsRepository.GetJourney(request);

            return Ok(response);
        }

        [HttpGet]
        [Route("filters")]
        public async Task<IActionResult> GetReportFilters()
        {
            try
            {
                var response =await _reportsRepository.GetReportFilters();
                return response.Status? Ok(response): BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new ApiResponse
                    {
                        Status = false,
                        Message = $"Error: {ex.Message}"
                    }
                );
            }
        }
    }
}
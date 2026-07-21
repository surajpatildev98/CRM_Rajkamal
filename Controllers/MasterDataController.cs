using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRMWebServices.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/master")]
    public class MasterDataController : ControllerBase
    {
        private readonly IMasterDataRepository _repo;

        public MasterDataController(IMasterDataRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("companies")]
        public async Task<IActionResult> Companies()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetCompanies() });

        [HttpGet("regions")]
        public async Task<IActionResult> Regions([FromQuery] int companyId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetRegionsByCompany(companyId) });

        [HttpGet("states")]
        public async Task<IActionResult> States([FromQuery] int regionId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetStatesByRegion(regionId) });

        [HttpGet("offices")]
        public async Task<IActionResult> Offices([FromQuery] int stateId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetOfficesByState(stateId) });

        [HttpGet("departments")]
        public async Task<IActionResult> Departments([FromQuery] int officeId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetDepartmentsByOffice(officeId) });

        [HttpGet("designations")]
        public async Task<IActionResult> Designations()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetDesignations() });

        [HttpGet("roles")]
        public async Task<IActionResult> Roles()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetRoles() });

        [HttpGet("permissions")]
        public async Task<IActionResult> Permissions()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetPermissions() });

        [HttpGet("teams")]
        public async Task<IActionResult> Teams()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetTeams() });

        [HttpGet("lead-statuses")]
        public async Task<IActionResult> LeadStatuses()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetLeadStatuses() });

        [HttpGet("countries")]
        public async Task<IActionResult> Countries()
             => Ok(new ApiResponse { Status = true, Data = await _repo.GetCountries() });

        [HttpGet("states-by-country")]
        public async Task<IActionResult> StatesByCountry([FromQuery] int countryId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetStatesByCountry(countryId) });

        [HttpGet("cities-by-state")]
        public async Task<IActionResult> CitiesByState([FromQuery] int stateId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetCitiesByState(stateId) });

        [HttpGet("lead-sources")]
        public async Task<IActionResult> LeadSources()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetLeadSources() });

        [HttpGet("employees")]
        public async Task<IActionResult> Employees()
           => Ok(new ApiResponse { Status = true, Data = await _repo.GetEmployeesForAssignment() });

        [HttpGet("offerings")]
        public async Task<IActionResult> Offerings()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetOfferings() });

        [HttpGet("units")]
        public async Task<IActionResult> Units()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetUnits() });

        [HttpGet("activity-types")]
        public async Task<IActionResult> ActivityTypes()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetActivityTypes() });


        [HttpGet("lead-close-statuses")]
        public async Task<IActionResult> LeadCloseStatuses()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetLeadCloseStatuses() });

        [HttpGet("followup-statuses")]
        public async Task<IActionResult> FollowUpStatuses()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetFollowUpStatuses() });
    }
}
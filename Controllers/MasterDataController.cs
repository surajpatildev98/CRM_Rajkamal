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

        #region // GET: api/master/companies
        [HttpGet("companies")]
        public async Task<IActionResult> Companies()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetCompanies() });
        #endregion

        #region // GET: api/master/regions

        [HttpGet("regions")]
        public async Task<IActionResult> Regions([FromQuery] int companyId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetRegionsByCompany(companyId) });
        #endregion

        #region // GET: api/master/states

        [HttpGet("states")]
        public async Task<IActionResult> States([FromQuery] int regionId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetStatesByRegion(regionId) });
        #endregion

        #region // GET: api/master/offices
        [HttpGet("offices")]
        public async Task<IActionResult> Offices([FromQuery] int stateId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetOfficesByState(stateId) });
        #endregion

        #region // GET: api/master/departments
        [HttpGet("departments")]
        public async Task<IActionResult> Departments([FromQuery] int officeId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetDepartmentsByOffice(officeId) });
        #endregion

        #region // GET: api/master/designations
        [HttpGet("designations")]
        public async Task<IActionResult> Designations()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetDesignations() });
        #endregion

        #region // GET: api/master/roles
        [HttpGet("roles")]
        public async Task<IActionResult> Roles()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetRoles() });
        #endregion

        #region // GET: api/master/permissions
        [HttpGet("permissions")]
        public async Task<IActionResult> Permissions()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetPermissions() });
        #endregion

        #region // GET: api/master/teams
        [HttpGet("teams")]
        public async Task<IActionResult> Teams()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetTeams() });
        #endregion

        #region // GET: api/master/lead-statuses
        [HttpGet("lead-statuses")]
        public async Task<IActionResult> LeadStatuses()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetLeadStatuses() });
        #endregion

        #region // GET: api/master/countries
        [HttpGet("countries")]
        public async Task<IActionResult> Countries()
             => Ok(new ApiResponse { Status = true, Data = await _repo.GetCountries() });
        #endregion

        #region // GET: api/master/states-by-country
        [HttpGet("states-by-country")]
        public async Task<IActionResult> StatesByCountry([FromQuery] int countryId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetStatesByCountry(countryId) });
        #endregion

        #region // GET: api/master/cities-by-state
        [HttpGet("cities-by-state")]
        public async Task<IActionResult> CitiesByState([FromQuery] int stateId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetCitiesByState(stateId) });
        #endregion

        #region // GET: api/master/lead-sources
        [HttpGet("lead-sources")]
        public async Task<IActionResult> LeadSources()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetLeadSources() });
        #endregion

        //[HttpGet("employees")]
        //public async Task<IActionResult> Employees()
        //   => Ok(new ApiResponse { Status = true, Data = await _repo.GetEmployeesForAssignment() });

        #region // GET: api/master/employees
        [HttpGet("employees")]
        public async Task<IActionResult> Employees()
        {
            var employeeId = User.EmployeeId();

            return Ok(new ApiResponse { Status = true,Data = await _repo.GetEmployeesForAssignment(employeeId) });
        }
        #endregion

        #region // GET: api/master/offerings

        [HttpGet("offerings")]
        public async Task<IActionResult> Offerings()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetOfferings() });
        #endregion

        #region // GET: api/master/units
        [HttpGet("units")]
        public async Task<IActionResult> Units()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetUnits() });
        #endregion

        #region // GET: api/master/activity-types
        [HttpGet("activity-types")]
        public async Task<IActionResult> ActivityTypes()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetActivityTypes() });
        #endregion

        #region // GET: api/master/lead-close-statuses

        [HttpGet("lead-close-statuses")]
        public async Task<IActionResult> LeadCloseStatuses()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetLeadCloseStatuses() });
        #endregion

        #region // GET: api/master/followup-statuses
        [HttpGet("followup-statuses")]
        public async Task<IActionResult> FollowUpStatuses()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetFollowUpStatuses() });
        #endregion

        #region // GET: api/master/dashboard-statuses
        [HttpGet("dashboard-statuses")]
        public async Task<IActionResult> DashboardStatuses()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetStatusesForDashbord() });
        #endregion
    }
}
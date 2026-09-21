using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Repository;
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

        #region  companies
        [HttpGet("companies")]
        public async Task<IActionResult> Companies()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetCompanies() });
        #endregion

        #region  regions
        [HttpGet("regions")]
        public async Task<IActionResult> Regions([FromQuery] int companyId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetRegionsByCompany(companyId) });
        #endregion

        #region states

        [HttpGet("states")]
        public async Task<IActionResult> States([FromQuery] int regionId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetStatesByRegion(regionId) });
        #endregion

        #region offices
        [HttpGet("offices")]
        public async Task<IActionResult> Offices([FromQuery] int? stateId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetOfficesByState(stateId) });
        #endregion

        #region departments
        [HttpGet("departments")]
        public async Task<IActionResult> Departments([FromQuery] int officeId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetDepartmentsByOffice(officeId) });
        #endregion

        #region designations
        [HttpGet("designations")]
        public async Task<IActionResult> Designations()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetDesignations() });
        #endregion

        #region roles
        [HttpGet("roles")]
        public async Task<IActionResult> Roles()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetRoles() });
        #endregion

        #region permissions
        [HttpGet("permissions")]
        public async Task<IActionResult> Permissions()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetPermissions() });
        #endregion

        #region teams
        [HttpGet("teams")]
        public async Task<IActionResult> Teams()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetTeams() });
        #endregion

        #region lead-statuses
        [HttpGet("lead-statuses")]
        public async Task<IActionResult> LeadStatuses()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetLeadStatuses() });
        #endregion

        #region countries
        [HttpGet("countries")]
        public async Task<IActionResult> Countries()
             => Ok(new ApiResponse { Status = true, Data = await _repo.GetCountries() });
        #endregion

        #region states-by-country
        [HttpGet("states-by-country")]
        public async Task<IActionResult> StatesByCountry([FromQuery] int countryId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetStatesByCountry(countryId) });
        #endregion

        #region cities-by-state
        [HttpGet("cities-by-state")]
        public async Task<IActionResult> CitiesByState([FromQuery] int stateId)
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetCitiesByState(stateId) });
        #endregion

        #region lead-sources
        [HttpGet("lead-sources")]
        public async Task<IActionResult> LeadSources()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetLeadSources() });
        #endregion     

        #region employees
        [HttpGet("employees")]
        public async Task<IActionResult> Employees()
        {
            var employeeId = User.EmployeeId();

            return Ok(new ApiResponse { Status = true,Data = await _repo.GetEmployeesForAssignment(employeeId) });
        }
        #endregion

        #region categories

        [HttpGet("categories")]
        public async Task<IActionResult> Categories()
            => Ok(new ApiResponse
            {
                Status = true,
                Data = await _repo.GetCategories()
            });

        #endregion

        #region offerings
        [HttpGet("offerings")]      

        public async Task<IActionResult> Offerings([FromQuery] int categoryId)
            => Ok(new ApiResponse
            {
                Status = true,
                Data = await _repo.GetOfferings(categoryId)
            });
        #endregion

        #region products
        [HttpGet("products")]
        public async Task<IActionResult> Products([FromQuery] int offeringId)
        {
            return Ok(new ApiResponse
            {
                Status = true,
                Data = await _repo.GetProducts(offeringId)
            });
        }

        #endregion

        #region units
        [HttpGet("units")]
        public async Task<IActionResult> Units()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetUnits() });
        #endregion

        #region activity-types
        [HttpGet("activity-types")]
        public async Task<IActionResult> ActivityTypes()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetActivityTypes() });
        #endregion

        #region lead-close-statuses
        [HttpGet("lead-close-statuses")]
        public async Task<IActionResult> LeadCloseStatuses()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetLeadCloseStatuses() });
        #endregion

        #region followup-statuses
        [HttpGet("followup-statuses")]
        public async Task<IActionResult> FollowUpStatuses()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetFollowUpStatuses() });
        #endregion

        #region dashboard-statuses
        [HttpGet("dashboard-statuses")]
        public async Task<IActionResult> DashboardStatuses()
            => Ok(new ApiResponse { Status = true, Data = await _repo.GetStatusesForDashbord() });
        #endregion

        #region qualification
        [HttpGet("qualification")]
        public async Task<IActionResult> Qualification()
          => Ok(new ApiResponse { Status = true, Data = await _repo.GetQualifications() });
        #endregion

        #region banks
        [HttpGet("banks")]
        public async Task<IActionResult> Banks()
          => Ok(new ApiResponse { Status = true, Data = await _repo.GetBanks() });
        #endregion

        #region accountType
        [HttpGet("accountType")]
        public async Task<IActionResult> AccountTypes()
          => Ok(new ApiResponse { Status = true, Data = await _repo.GetAccountTypes() });
        #endregion

        #region familyRelationships
        [HttpGet("familyRelationships")]
        public async Task<IActionResult> FamilyRelationShips()
          => Ok(new ApiResponse { Status = true, Data = await _repo.GetFamilyRelationShips() });
        #endregion

        #region documentTypes
        [HttpGet("documentTypes")]
        public async Task<IActionResult> DocumentTypes()
          => Ok(new ApiResponse { Status = true, Data = await _repo.GetDocumentTypes() });
        #endregion

        #region managers
        [HttpGet("managers")]
        public async Task<IActionResult> Managers([FromQuery] int officeId,[FromQuery] int departmentId)
        {
            try
            {
                if (officeId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Office is required."
                    });
                }

                if (departmentId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Department is required."
                    });
                }

                var managers = await _repo.GetManagersAsync(officeId,departmentId);

                return Ok(new ApiResponse
                {
                    Status = true,
                    Message = "Managers retrieved successfully.",
                    Data = managers
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiResponse
                    {
                        Status = false,
                        Message = ex.Message
                    });
            }
        }
        #endregion

        #region leave-policies
        [HttpGet("leave-policies")]
        public async Task<IActionResult> GetLeavePolicies([FromQuery] int companyId)
         => Ok(new ApiResponse { Status = true, Data = await _repo.GetLeavePolicies(companyId) });
        #endregion
    }
}
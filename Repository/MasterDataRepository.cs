using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CRMWebServices.Repository
{
    public class MasterDataRepository : IMasterDataRepository
    {
        private readonly IDatabaseService _database;

        public MasterDataRepository(IDatabaseService database)
        {
            _database = database;
        }

        private List<DropdownItem> MapToDropdown(DataTable dt)
        {
            var list = new List<DropdownItem>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new DropdownItem
                {
                    Id = Convert.ToInt32(row[0]),
                    Name = row[1].ToString()
                });
            }
            return list;
        }

        public async Task<List<DropdownItem>> GetCompanies()
        {
            var dt = await _database.ExecuteDataTableAsync("USP_ORG_EmployeeManagement",new SqlParameter("@Status", "GET_COMPANIES"));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetRegionsByCompany(int companyId)
        {
            var dt = await _database.ExecuteDataTableAsync( "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "GET_REGIONS_BY_COMPANY"),
                new SqlParameter("@CompanyId", companyId));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetStatesByRegion(int regionId)
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "GET_STATES_BY_REGION"),
                new SqlParameter("@RegionId", regionId));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetOfficesByState(int stateId)
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "GET_OFFICES_BY_STATE"),
                new SqlParameter("@StateId", stateId));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetDepartmentsByOffice(int officeId)
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "GET_DEPARTMENTS_BY_OFFICE"),
                new SqlParameter("@OfficeId", officeId));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetDesignations()
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "GET_DESIGNATIONS"));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetRoles()
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "GET_ROLES"));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetPermissions()
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "GET_PERMISSIONS"));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetTeams()
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "GET_TEAMS"));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetLeadStatuses()
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "GET_LEAD_STATUSES"));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetCountries()
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "GET_COUNTRIES"));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetStatesByCountry(int countryId)
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "GET_STATES_BY_COUNTRY"),
                new SqlParameter("@CountryId", countryId));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetCitiesByState(int stateId)
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "GET_CITIES_BY_STATE"),
                new SqlParameter("@StateId", stateId));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetLeadSources()
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "GET_LEAD_SOURCES"));
            return MapToDropdown(dt);
        }

        //public async Task<List<DropdownItem>> GetEmployeesForAssignment()
        //{
        //    var dt = await _database.ExecuteDataTableAsync(
        //        "USP_ORG_EmployeeManagement",
        //        new SqlParameter("@Status", "GET_EMPLOYEES_FOR_ASSIGNMENT"));
        //    return MapToDropdown(dt);
        //}

        public async Task<List<DropdownItem>> GetEmployeesForAssignment(int loggedInEmployeeId)
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "GET_EMPLOYEES_FOR_ASSIGNMENT"),
                new SqlParameter("@LoggedInEmployeeId", loggedInEmployeeId));

            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetOfferings()
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "GET_OFFERINGS"));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetUnits()
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "GET_UNITS"));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetActivityTypes()
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_ORG_EmployeeManagement",
                new SqlParameter("@Status", "GET_ACTIVITY_TYPES"));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetLeadCloseStatuses()
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_CRM_LeadManagement",
                new SqlParameter("@Status", "GET_LEAD_CLOSE_STATUSES"));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetFollowUpStatuses()
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_CRM_LeadManagement",
                new SqlParameter("@Status", "GET_FOLLOWUP_STATUSES"));
            return MapToDropdown(dt);
        }

        public async Task<List<DropdownItem>> GetStatusesForDashbord()
        {
            var dt = await _database.ExecuteDataTableAsync(
                "USP_CRM_LeadManagement",
                new SqlParameter("@Status", "GET_LEAD_STATUSES_FOR_DASHBOARD"));
            return MapToDropdown(dt);
        }
    }
}
using CRMWebServices.Models;

namespace CRMWebServices.Interfaces
{
    public interface IMasterDataRepository
    {
        Task<List<DropdownItem>> GetCompanies();
        Task<List<DropdownItem>> GetRegionsByCompany(int companyId);
        Task<List<DropdownItem>> GetStatesByRegion(int regionId);
        Task<List<DropdownItem>> GetOfficesByState(int stateId);
        Task<List<DropdownItem>> GetDepartmentsByOffice(int officeId);
        Task<List<DropdownItem>> GetDesignations();
        Task<List<DropdownItem>> GetRoles();
        Task<List<DropdownItem>> GetPermissions();
        Task<List<DropdownItem>> GetTeams();
        Task<List<DropdownItem>> GetLeadStatuses();

        Task<List<DropdownItem>> GetCountries();
        Task<List<DropdownItem>> GetStatesByCountry(int countryId);
        Task<List<DropdownItem>> GetCitiesByState(int stateId);
        Task<List<DropdownItem>> GetLeadSources();

        //Task<List<DropdownItem>> GetEmployeesForAssignment();
        Task<List<DropdownItem>> GetEmployeesForAssignment(int loggedInEmployeeId);

        Task<List<DropdownItem>> GetOfferings();
        Task<List<DropdownItem>> GetUnits();

        Task<List<DropdownItem>> GetActivityTypes();


        /// Gets the close status dropdown for leads
        Task<List<DropdownItem>> GetLeadCloseStatuses();
        Task<List<DropdownItem>> GetFollowUpStatuses();

        Task<List<DropdownItem>> GetStatusesForDashbord();
    }
}

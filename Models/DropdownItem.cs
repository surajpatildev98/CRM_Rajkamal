namespace CRMWebServices.Models
{
    public class DropdownItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ManagerDropdownItem
    {
        public int EmployeeId { get; set; }

        public string? EmployeeCode { get; set; }

        public string? EmployeeName { get; set; }

        public int RoleId { get; set; }

        public string? RoleCode { get; set; }

        public string? RoleName { get; set; }
    }
}

using CRMWebServices.Interfaces;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using static CRMWebServices.Models.PermissionModels;
namespace CRMWebServices.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IDatabaseService _database;

        public AuthRepository(IDatabaseService database)
        {
            _database = database;
        }

        #region // IAuthRepository Implementation used for authentication and authorization
        //public async Task<DataRow> GetEmployeeForLogin(string loginId)
        //{

        //    var dt = await _database.ExecuteDataTableAsync("USP_ORG_Authentication",
        //        new SqlParameter("@Status", "LOGIN"),
        //        new SqlParameter("@LoginId", loginId));

        //    return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        //}

        // Repository — was likely ExecuteDataTableAsync returning one row; now needs the second table too
        public async Task<(DataRow? EmployeeRow, List<ModuleAccessInfo> Modules)> GetEmployeeForLogin(string loginId)
        {
            var ds = await _database.ExecuteDataSetAsync("USP_ORG_Authentication",
                new SqlParameter("@Status", "LOGIN"),
                new SqlParameter("@LoginId", loginId)
            );

            if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                return (null, new List<ModuleAccessInfo>());

            var employeeRow = ds.Tables[0].Rows[0];

            var modules = new List<ModuleAccessInfo>();

            if (ds.Tables.Count > 1)
            {
                foreach (DataRow r in ds.Tables[1].Rows)
                {
                    var module = new ModuleAccessInfo
                    {
                        ModuleId = Convert.ToInt32(r["ModuleId"]),
                        ModuleCode = r["ModuleCode"]?.ToString(),
                        ModuleName = r["ModuleName"]?.ToString(),
                        IconName = r["IconName"] == DBNull.Value ? null : r["IconName"].ToString(),
                        IsAllowed = Convert.ToBoolean(r["IsAllowed"])
                    };

                    if (r["PermissionsJson"] != DBNull.Value)
                    {
                        var json = r["PermissionsJson"]?.ToString();
                        if (!string.IsNullOrEmpty(json))
                        {
                            var parsed = JsonConvert.DeserializeObject<List<dynamic>>(json);
                            module.Permissions = parsed.Select(p => new PermissionAccessInfo
                            {
                                PermissionId = Convert.ToInt32(p.PermissionId),
                                PermissionCode = p.PermissionCode?.ToString(),
                                PermissionName = p.PermissionName?.ToString(),
                                IsAllowed = Convert.ToBoolean(p.IsAllowed)
                            }).ToList();
                        }
                    }

                    modules.Add(module);
                }
            }

            return (employeeRow, modules);
        }
        #endregion
    }
}
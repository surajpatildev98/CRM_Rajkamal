using BCrypt.Net;
using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace CRMWebServices.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepo;
        private readonly JwtHelper _jwt;

        public AuthController(IAuthRepository authRepo, JwtHelper jwt)
        {
            _authRepo = authRepo;
            _jwt = jwt;
        }

        #region // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = new ApiResponse();

            var row = await _authRepo.GetEmployeeForLogin(request.LoginId);

            if (row == null)
            {
                response.Status = false;
                response.Message = "Invalid Login Id or Password.";
                return BadRequest(response);
            }

            bool isActive = Convert.ToBoolean(row["IsActive"]);
            bool isLocked = Convert.ToBoolean(row["IsLocked"]);

            if (!isActive)
            {
                response.Status = false;
                response.Message = "Account is inactive.";
                return BadRequest(response);
            }

            if (isLocked)
            {
                response.Status = false;
                response.Message = "Account is locked. Contact administrator.";
                return BadRequest(response);
            }

            string storedHash = row["PasswordHash"]?.ToString();

            if (string.IsNullOrEmpty(storedHash) || !BCrypt.Net.BCrypt.Verify(request.Password, storedHash))
            {
                // TODO: increment FailedLoginAttempts via a small UPDATE/SP call here,
                // and lock the account after N attempts if you want that behavior.
                response.Status = false;
                response.Message = "Invalid Login Id or Password.";
                return BadRequest(response);
            }

            int employeeId = Convert.ToInt32(row["EmployeeId"]);
            string employeeCode = row["EmployeeCode"]?.ToString();
            string roleIds = row["RoleIds"] == DBNull.Value ? "" : row["RoleIds"].ToString();

            string token = _jwt.GenerateToken(employeeId, request.LoginId, employeeCode, roleIds);

            response.Status = true;
            response.Message = "Login Successful";
            response.Data = new
            {
                Token = token,
                EmployeeId = employeeId,
                EmployeeCode = employeeCode,
                FirstName = row["FirstName"]?.ToString(),
                LastName = row["LastName"]?.ToString(),
                RoleId = roleIds,
                RoleNames = row["RoleNames"] == DBNull.Value ? "" : row["RoleNames"].ToString(),
                DepartmentName = row["DepartmentName"] == DBNull.Value ? "" : row["DepartmentName"].ToString(),
                OfficeName = row["OfficeName"] == DBNull.Value ? "" : row["OfficeName"].ToString()
            };

            return Ok(response);
        }
        #endregion
    }
}
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CRMWebServices.Helpers
{
    public class JwtHelper
    {
        private readonly IConfiguration _config;

        public JwtHelper(IConfiguration config)
        {
            _config = config;
        }

        #region Generate JWT Token is used to generate JWT token for authentication and authorization
        public string GenerateToken(int employeeId, string loginId, string employeeCode, string roleIds)
        {
            var claims = new List<Claim>
            {
                new Claim("EmployeeId", employeeId.ToString()),
                new Claim("LoginId", loginId ?? ""),
                new Claim("EmployeeCode", employeeCode ?? ""),
                new Claim("RoleIds", roleIds ?? "")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpiryMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        #endregion
    }
}
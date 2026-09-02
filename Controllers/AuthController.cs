using BCrypt.Net;
using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using CRMWebServices.Repository;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using static CRMWebServices.Models.LogoutModels;
using static CRMWebServices.Models.PermissionModels;

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

            //var row = await _authRepo.GetEmployeeForLogin(request.LoginId);
            // NEW
            var (row, modules) = await _authRepo.GetEmployeeForLogin(request.LoginId);

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

            // DEVICE CHECK - ONE USER ONE DEVICE

            if (!string.IsNullOrWhiteSpace(request.DeviceId))
            {
                var activeDevice = await _authRepo.CheckActiveDevice(
                    employeeId,
                    request.DeviceId);

                // CASE 1: No active device

                if (activeDevice == null)
                {
                    // No device currently logged in.
                    // Register this device.

                    if (!string.IsNullOrWhiteSpace(request.DeviceToken))
                    {
                        var registerResponse =
                            await _authRepo.RegisterEmployeeDevice(
                                employeeId,
                                new RegisterEmployeeDeviceRequest
                                {
                                    DeviceId = request.DeviceId,
                                    DeviceToken = request.DeviceToken,
                                    DeviceType = request.DeviceType,
                                    AppVersion = request.AppVersion
                                });


                        if (!registerResponse.Status)
                        {
                            return BadRequest(registerResponse);
                        }
                    }
                }


                // CASE 2: Same device

                else if (activeDevice.IsSameDevice)
                {
                    // Same device is already logged in.
                    //
                    // We can update the FCM token and other
                    // device information.

                    if (!string.IsNullOrWhiteSpace(request.DeviceToken))
                    {
                        var registerResponse =
                            await _authRepo.RegisterEmployeeDevice(
                                employeeId,
                                new RegisterEmployeeDeviceRequest
                                {
                                    DeviceId = request.DeviceId,
                                    DeviceToken = request.DeviceToken,
                                    DeviceType = request.DeviceType,
                                    AppVersion = request.AppVersion
                                });


                        if (!registerResponse.Status)
                        {
                            return BadRequest(registerResponse);
                        }
                    }
                }

                // CASE 3: Different device

                else
                {
                    return Ok(new ApiResponse
                    {
                        Status = false,
                        Message =
                            "You are already logged in on another device. " +
                            "Do you want to logout from that device and continue?",

                        Data = new
                        {
                            Code = "ALREADY_LOGGED_IN",

                            EmployeeId = employeeId,

                            OldDeviceId = activeDevice.ActiveDeviceId
                        }
                    });
                }
            }

            // Register / Update Firebase device           

            //if (!string.IsNullOrWhiteSpace(request.DeviceId) &&!string.IsNullOrWhiteSpace(request.DeviceToken))
            //{
            //    await _authRepo.RegisterEmployeeDevice(employeeId,
            //        new RegisterEmployeeDeviceRequest
            //        {
            //            DeviceId = request.DeviceId,
            //            DeviceToken = request.DeviceToken,
            //            DeviceType = request.DeviceType,
            //            AppVersion = request.AppVersion
            //        });
            //}


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
                OfficeName = row["OfficeName"] == DBNull.Value ? "" : row["OfficeName"].ToString(),
                Modules = modules   // NEW
            };

            return Ok(response);
        }
        #endregion

        [HttpPost("ForceLogout")]
        public async Task<IActionResult> ForceLogout([FromBody] ForceLogoutRequest model)
        {
            var response = await _authRepo.ForceLogoutDevice(
                model.EmployeeId,
                model.DeviceId);

            return Ok(response);
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest model)
        {
            var response = await _authRepo.Logout(
                model.EmployeeId,
                model.DeviceId);

            return Ok(response);
        }
    }
}
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
using System.Security.Cryptography;

namespace CRMWebServices.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepo;
        private readonly JwtHelper _jwt;
        private readonly IEmailService _emailService;

        public AuthController(IAuthRepository authRepo, JwtHelper jwt, IEmailService emailService)
        {
            _authRepo = authRepo;
            _jwt = jwt;
            _emailService = emailService;
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

        #region // POST: api/auth/forgot-password
        [HttpPost("forgot_password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.LoginId))
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Login Id or email is required."
                    });
                }

                string loginId = request.LoginId.Trim();

                var (employeeRow, _) = await _authRepo.GetEmployeeForLogin(loginId);

                if (employeeRow == null)
                {
                    return Ok(new ApiResponse
                    {
                        Status = true,
                        //Message = "If the account exists, an OTP has been sent to the registered email address."
                        Message = "Login Id not found. Please enter a valid Login Id."
                    });
                }

                bool isActive = employeeRow["IsActive"] != DBNull.Value &&
                                Convert.ToBoolean(employeeRow["IsActive"]);

                bool isLocked = employeeRow["IsLocked"] != DBNull.Value &&
                                Convert.ToBoolean(employeeRow["IsLocked"]);

                //if (!isActive || isLocked)
                //{
                //    return Ok(new ApiResponse
                //    {
                //        Status = true,
                //        Message = "If the account exists, an OTP has been sent to the registered email address."
                //    });
                //}

                if (!isActive)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Your account is inactive. Please contact the administrator."
                    });
                }

                if (isLocked)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Your account is locked. Please contact the administrator."
                    });
                }

                string email = employeeRow["OfficialEmail"] != DBNull.Value? employeeRow["OfficialEmail"]?.ToString(): null;

                if (string.IsNullOrWhiteSpace(email))
                {
                    return Ok(new ApiResponse
                    {
                        Status = true,
                        //Message = "If the account exists, an OTP has been sent to the registered email address."
                        Message = "No registered email address is associated with this account. Please contact the administrator."
                    });
                }

                int employeeId = Convert.ToInt32(employeeRow["EmployeeId"]);

                string firstName = employeeRow["FirstName"] != DBNull.Value? employeeRow["FirstName"]?.ToString() ?? "User": "User";

                // Generate 6 digit OTP
                string otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

                // Hash OTP before saving
                string otpHash = BCrypt.Net.BCrypt.HashPassword(otp);

                // Save OTP hash in database
                var saveResponse = await _authRepo.GenerateResetOtp(employeeId,otpHash);

                if (!saveResponse.Status)
                {
                    return BadRequest(saveResponse);
                }

                string subject = "CRM - Password Reset OTP";

                string body = $@"
                    <html>
                    <body style=""margin:0;padding:0;background-color:#f4f4f7;font-family:Segoe UI,Arial,sans-serif;"">
                        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f4f7;padding:30px 0;"">
                            <tr>
                                <td align=""center"">
                                    <table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 6px rgba(0,0,0,0.08);"">

                                        <!-- Header -->                                                           
                                         <tr>
                                             <td style=""padding:24px 32px;border-bottom:3px solid #C8161D;text-align:center;"">
                                                 <img src=""cid:companylogo"" alt=""Rajkamal Bar Scan Systems Pvt. Ltd."" width=""260"" style=""display:block;height:auto;max-width:260px;margin:0 auto;"" />
                                             </td>
                                         </tr>

                                        <!-- Body -->
                                        <tr>
                                            <td style=""padding:32px;"">
                                                <p style=""margin:0 0 16px 0;font-size:15px;color:#333333;"">
                                                    Hello <strong>{firstName}</strong>,
                                                </p>

                                                <p style=""margin:0 0 24px 0;font-size:15px;color:#333333;line-height:1.5;"">
                                                    We received a request to reset your CRM password. Use the OTP below to continue.
                                                </p>

                                                <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                                    <tr>
                                                        <td align=""center"" style=""padding:20px 0;"">
                                                            <span style=""display:inline-block;background-color:#FCEBEC;border:1px dashed #C8161D;border-radius:6px;padding:14px 32px;font-size:28px;font-weight:700;letter-spacing:6px;color:#C8161D;"">
                                                                {otp}
                                                            </span>
                                                        </td>
                                                    </tr>
                                                </table>

                                                <p style=""margin:24px 0 0 0;font-size:14px;color:#666666;line-height:1.5;"">
                                                    This OTP is valid for <strong>10 minutes</strong>. Do not share it with anyone.
                                                </p>

                                                <p style=""margin:16px 0 0 0;font-size:14px;color:#666666;line-height:1.5;"">
                                                    If you did not request a password reset, you can safely ignore this email — your password will remain unchanged.
                                                </p>
                                            </td>
                                        </tr>

                                        <!-- Footer -->
                                        <tr>
                                            <td style=""background-color:#fafafa;padding:20px 32px;border-top:1px solid #eeeeee;"">
                                                <p style=""margin:0;font-size:12px;color:#999999;"">
                                                    Rajkamal Bar Scan Systems Pvt. Ltd. — This is an automated message. Please do not reply to this email.
                                                </p>
                                            </td>
                                        </tr>

                                    </table>
                                </td>
                            </tr>
                        </table>
                    </body>
                    </html>";

                var emailResult = await _emailService.SendEmailAsync(
                    email,
                    subject,
                    body);

                if (!emailResult.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Unable to send OTP email."
                    });
                }

                return Ok(new ApiResponse
                {
                    Status = true,
                    Message = "OTP has been sent successfully to your registered email address."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }
        #endregion

        #region // POST: api/auth/verify-reset-otp
        [HttpPost("verify_otp")]
        public async Task<IActionResult> VerifyResetOtp(
            [FromBody] VerifyResetOtpRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.LoginId))
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Login Id or email is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.OTP))
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "OTP is required."
                    });
                }

                string loginId = request.LoginId.Trim();

                var (employeeRow, _) = await _authRepo.GetEmployeeForLogin(loginId);

                if (employeeRow == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Invalid OTP request."
                    });
                }

                int employeeId = Convert.ToInt32(employeeRow["EmployeeId"]);

                var otpRow = await _authRepo.GetResetOtp(employeeId);

                if (otpRow == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "OTP request not found or expired."
                    });
                }

                bool isUsed = otpRow["IsUsed"] != DBNull.Value &&Convert.ToBoolean(otpRow["IsUsed"]);
                bool isVerified = otpRow["IsVerified"] != DBNull.Value &&Convert.ToBoolean(otpRow["IsVerified"]);
                DateTime expiresOn = Convert.ToDateTime(otpRow["ExpiresOn"]);
                int attemptCount = otpRow["AttemptCount"] != DBNull.Value? Convert.ToInt32(otpRow["AttemptCount"]): 0;

                if (isUsed)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "OTP has expired or has already been used."
                    });
                }

                if (isVerified)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "OTP has already been verified."
                    });
                }

                if (expiresOn <= DateTime.Now)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "OTP has expired. Please request a new OTP."
                    });
                }

                if (attemptCount >= 5)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Maximum OTP attempts exceeded. Please request a new OTP."
                    });
                }

                string otpHash = otpRow["OTPHash"]?.ToString();

                if (string.IsNullOrWhiteSpace(otpHash))
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Invalid OTP request."
                    });
                }

                bool isOtpValid = BCrypt.Net.BCrypt.Verify(
                    request.OTP.Trim(),
                    otpHash);

                if (!isOtpValid)
                {
                    await _authRepo.IncrementOtpAttempt(employeeId);

                    int remainingAttempts = Math.Max(0, 4 - attemptCount);

                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = remainingAttempts > 0
                            ? $"Invalid OTP. {remainingAttempts} attempt(s) remaining."
                            : "Invalid OTP. Maximum attempts exceeded."
                    });
                }

                var verifyResponse =
                    await _authRepo.VerifyResetOtp(employeeId);

                return verifyResponse.Status
                    ? Ok(verifyResponse)
                    : BadRequest(verifyResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }
        #endregion

        #region // POST: api/auth/reset-password
        [HttpPost("Reset_password")]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Invalid request."
                    });
                }

                if (request.ResetToken == Guid.Empty)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Reset token is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "New password is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Confirm password is required."
                    });
                }

                if (request.NewPassword != request.ConfirmPassword)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "New password and confirm password do not match."
                    });
                }

                // Password policy
                if (request.NewPassword.Length < 8)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Password must contain at least 8 characters."
                    });
                }

                if (!request.NewPassword.Any(char.IsUpper))
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Password must contain at least one uppercase letter."
                    });
                }

                if (!request.NewPassword.Any(char.IsLower))
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Password must contain at least one lowercase letter."
                    });
                }

                if (!request.NewPassword.Any(char.IsDigit))
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Password must contain at least one number."
                    });
                }

                if (!request.NewPassword.Any(ch => !char.IsLetterOrDigit(ch)))
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Password must contain at least one special character."
                    });
                }

                // Find employee from reset token
                // We need EmployeeId from the verified reset session.
                // This will be handled by the repository/SP.
                var employeeIdResponse =await _authRepo.GetResetEmployeeByToken(request.ResetToken);

                if (employeeIdResponse == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Invalid or expired reset token."
                    });
                }

                int employeeId = employeeIdResponse.Value;

                // Hash new password using BCrypt
                string newPasswordHash =BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

                var response = await _authRepo.ResetPassword(
                    employeeId,request.ResetToken,newPasswordHash);

                return response.Status? Ok(response): BadRequest(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse
                {
                    Status = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }
        #endregion
    }
}
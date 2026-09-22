using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRMWebServices.Controllers
{
    [ApiController]
    [Route("api/email-test")]
    [Authorize]
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailTestController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] EmailMessage request)
        {
            var result =await _emailService.SendAsync(request);
            if (!result)
            {
                return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Email could not be sent."
                    });
            }

            return Ok(
                new ApiResponse
                {
                    Status = true,
                    Message = "Email sent successfully."
                });
        }
    }
}
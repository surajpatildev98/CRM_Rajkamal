using CRMWebServices.Interfaces;
using CRMWebServices.Models.AnnouncementModels;
using Microsoft.AspNetCore.Mvc;

namespace CRMWebServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnnouncementController : ControllerBase
    {
        private readonly IAnnouncementRepository _announcementRepository;

        public AnnouncementController(IAnnouncementRepository announcementRepository)
        {
            _announcementRepository = announcementRepository;
        }

        // GET: api/Announcement
        [HttpGet]
        public async Task<IActionResult> GetAnnouncements()
        {
            var response =await _announcementRepository.GetAnnouncements();

            return Ok(response);
        }


        // GET: api/Announcement/1
        [HttpGet("{announcementId:int}")]
        public async Task<IActionResult> GetAnnouncement(
            int announcementId)
        {
            var response =await _announcementRepository.GetAnnouncement(announcementId);
            return Ok(response);
        }


        // POST: api/Announcement
        [HttpPost("CreateAnnouncement")]
        public async Task<IActionResult> CreateAnnouncement([FromBody] AnnouncementRequest model)
        {
            if (model == null)
            {
                return BadRequest(new
                {
                    Status = false,
                    Message = "Announcement request is required."
                });
            }

            var response =await _announcementRepository.CreateAnnouncement(model,model.CreatedBy);
            return Ok(response);
        }


        // PUT: api/Announcement
        [HttpPut]
        public async Task<IActionResult> UpdateAnnouncement([FromBody] AnnouncementRequest model)
        {
            if (model == null)
            {
                return BadRequest(new
                {
                    Status = false,
                    Message = "Announcement request is required."
                });
            }

            int employeeId =model.UpdatedBy ?? model.CreatedBy;
            var response =await _announcementRepository.UpdateAnnouncement(model,employeeId);
            return Ok(response);
        }


        [HttpPost("{announcementId:int}/target")]
        public async Task<IActionResult> AddAnnouncementTarget(int announcementId,[FromBody] AnnouncementTargetRequest model,[FromQuery] int employeeId)
        {
            if (model == null)
            {
                return BadRequest(new
                {
                    Status = false,
                    Message = "Announcement target is required."
                });
            }

            var response =await _announcementRepository.AddAnnouncementTarget(announcementId,model,employeeId);

            return Ok(response);
        }

        // GET: api/Announcement/1/targets
        [HttpGet("{announcementId:int}/targets")]
        public async Task<IActionResult> GetAnnouncementTargets(
            int announcementId)
        {
            var response =await _announcementRepository.GetAnnouncementTargets(announcementId);
            return Ok(response);
        }


        // GET: api/Announcement/1/recipients
        [HttpGet("{announcementId:int}/recipients")]
        public async Task<IActionResult> GetAnnouncementRecipients(
            int announcementId)
        {
            var response =await _announcementRepository.GetAnnouncementRecipients(announcementId);
            return Ok(response);
        }


        // POST: api/Announcement/1/send
        [HttpPost("{announcementId:int}/send")]
        public async Task<IActionResult> SendAnnouncement(int announcementId,[FromQuery] int employeeId)
        {
            var response =await _announcementRepository.SendAnnouncement(announcementId,employeeId);
            return Ok(response);
        }

        [HttpGet("offices/company/{companyId:int}")]
        public async Task<IActionResult> GetOfficesByCompany(int companyId)
        {
            var response = await _announcementRepository.GetOfficesByCompany(companyId);
            return Ok(response);
        }

        [HttpGet("departments/office/{officeId:int}")]
        public async Task<IActionResult> GetDepartmentsByOffice(int officeId)
        {
            var response = await _announcementRepository.GetDepartmentsByOffice(officeId);
            return Ok(response);
        }
    }
}
using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CRMWebServices.Models.AmcModels;

namespace CRMWebServices.Controllers
{
    /// <summary>
    /// AMC module (ModuleCode AMC, super module SUPPORT).
    /// Access is permission based (Role Access + User Override), no hard-coded roles:
    /// AMC_View, AMC_Create (also Excel upload), AMC_Edit (also invoice), AMC_Renew, AMC_Cancel, AMC_Coverage.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AmcController : ControllerBase
    {
        private readonly IAmcRepository _repository;
        private readonly IPermissionService _permission;

        public AmcController(IAmcRepository repository, IPermissionService permission)
        {
            _repository = repository;
            _permission = permission;
        }

        private int EmployeeId => User.EmployeeId();

        private async Task<IActionResult> Run(Func<Task<ApiResponse>> action, params string[] anyOf)
        {
            if (!await _permission.HasAnyPermission(User, anyOf))
                return StatusCode(403, new ApiResponse { Status = false, Message = "You do not have access to this action." });
            var response = await action();
            return response.Status ? Ok(response) : BadRequest(response);
        }

        // Screens that need a read-only view: AMC list/details, Renewals screen, ticket badge.
        private static readonly string[] ViewCodes = { "AMC_View", "AMC_Renew", "AMC_Cancel" };

        /// <summary>Duration dropdown + coverage chips.</summary>
        [HttpGet("masters")]
        public Task<IActionResult> Masters() => Run(_repository.GetMasters, "AMC_View", "AMC_Create", "AMC_Edit", "AMC_Renew");

        /// <summary>filter: ALL | ACTIVE | EXPIRING | EXPIRED | RENEWED | CANCELLED; amcType: HARDWARE | SOFTWARE.</summary>
        [HttpGet("list")]
        public Task<IActionResult> List(string? filter, string? amcType, string? search) =>
            Run(() => _repository.GetList(filter, amcType, search), ViewCodes);

        /// <summary>Expiring / Expired / Active counts (Renewals screen cards).</summary>
        [HttpGet("counts")]
        public Task<IActionResult> Counts(string? amcType, string? search) =>
            Run(() => _repository.GetCounts(amcType, search), ViewCodes);

        [HttpGet("details/{amcContractId:int}")]
        public Task<IActionResult> Details(int amcContractId) =>
            Run(() => _repository.GetDetails(amcContractId), ViewCodes);

        [HttpPost("create")]
        public Task<IActionResult> Create([FromBody] AmcSaveRequest model) =>
            Run(() => _repository.Save(model, "CREATE", EmployeeId), "AMC_Create");

        [HttpPost("update")]
        public Task<IActionResult> Update([FromBody] AmcSaveRequest model) =>
            Run(() => _repository.Save(model, "UPDATE", EmployeeId), "AMC_Edit");

        [HttpPost("invoice")]
        public Task<IActionResult> Invoice([FromBody] AmcInvoiceRequest model) =>
            Run(() => _repository.AttachInvoice(model, EmployeeId), "AMC_Edit");

        /// <summary>Excel rows (read by the app) → one AMC per Customer + Invoice No.</summary>
        [HttpPost("upload")]
        public Task<IActionResult> Upload([FromBody] AmcUploadRequest model) =>
            Run(() => _repository.Upload(model, EmployeeId), "AMC_Create");

        /// <summary>Renewals screen: new contract copied from the old one; old becomes RENEWED.</summary>
        [HttpPost("renew")]
        public Task<IActionResult> Renew([FromBody] AmcSaveRequest model) =>
            Run(() => _repository.Save(model, "RENEW", EmployeeId), "AMC_Renew");

        [HttpPost("cancel")]
        public Task<IActionResult> Cancel([FromBody] AmcCancelRequest model) =>
            Run(() => _repository.Cancel(model, EmployeeId), "AMC_Cancel");

        /// <summary>AMC badge on the ticket screen. Any logged-in user (ticket raisers need it).</summary>
        [HttpGet("coverage-check")]
        public async Task<IActionResult> CoverageCheck(string? customerSource, int sourceCustomerId, string? serialNo, long? productId)
        {
            var response = await _repository.CheckCoverage(customerSource, sourceCustomerId, serialNo, productId);
            return response.Status ? Ok(response) : BadRequest(response);
        }

        [HttpGet("coverage-services")]
        public Task<IActionResult> CoverageServices() => Run(_repository.GetCoverageServices, "AMC_Coverage");

        [HttpPost("coverage-services/save")]
        public Task<IActionResult> SaveCoverageService([FromBody] AmcCoverageSaveRequest model) =>
            Run(() => _repository.SaveCoverageService(model, EmployeeId), "AMC_Coverage");
    }
}

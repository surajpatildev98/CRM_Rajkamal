using CRMWebServices.Helpers;
using CRMWebServices.Interfaces;
using CRMWebServices.Models;
using CRMWebServices.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CRMWebServices.Models.FreightPolicyModels;

namespace CRMWebServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FreightPolicyController : ControllerBase
    {
        private readonly IFreightPolicyRepository _repository;

        public FreightPolicyController(IFreightPolicyRepository repository)
        {
            _repository = repository;
        }

        #region API : api/FreightPolicy/CreatePolicy
        [HttpPost]
        [Route("CreatePolicy")]
        public async Task<IActionResult> CreatePolicy([FromBody] CreateFreightPolicyRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Status = false,
                        Message = "Request is required."
                    });
                }

                var employeeId = User.EmployeeId();

                var response = await _repository.CreateFreightPolicy(request, employeeId);

                return response.Status
                    ? Ok(response)
                    : BadRequest(response);
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

        #region API : api/FreightPolicy/UpdatePolicy
        [HttpPost("UpdatePolicy")]
        public async Task<IActionResult> UpdateFreightPolicy([FromBody] UpdateFreightPolicyRequest model)
        {
            var employeeId = User.EmployeeId();
            var response = await _repository.UpdateFreightPolicy(model, employeeId);

            return Ok(response);
        }
        #endregion

        #region API : api/FreightPolicy/DeactivatePolicy
        [HttpPost("DeactivatePolicy")]
        public async Task<IActionResult> DeactivateFreightPolicy([FromBody] DeactivateFreightPolicyRequest model)
        {
            var employeeId = User.EmployeeId();
            var response = await _repository.DeactivateFreightPolicy(model.FreightPolicyId, employeeId);

            return Ok(response);
        }
        #endregion

        #region API : api/FreightPolicy/GetPolicyList
        [HttpGet]
        [Route("GetPolicyList")]
        public async Task<IActionResult> GetPolicyList()
        {
            try
            {
                var response = await _repository.GetFreightPolicyList();

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

        #region API : api/FreightPolicy/GetPolicyDetails
        [HttpGet]
        [Route("GetPolicyDetails")]
        public async Task<IActionResult> GetPolicyDetails(int freightPolicyId)
        {
            try
            {
                var response = await _repository.GetFreightPolicyDetails(freightPolicyId);

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

        #region API : api/FreightPolicy/GetApplicableFreight
        [HttpGet]
        [Route("GetApplicableFreight")]
        public async Task<IActionResult> GetApplicableFreight(decimal orderValue)
        {
            try
            {
                var response = await _repository.GetApplicableFreight(orderValue);
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

        #region API : api/FreightPolicy/GetActivePolicy
        [HttpGet]
        [Route("GetActivePolicy")]
        public async Task<IActionResult> GetActivePolicy()
        {
            try
            {
                var response = await _repository.GetActiveFreightPolicy();
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

        #region GET Freight Delivery Partners

        [HttpGet]
        [Route("delivery-partners")]
        public async Task<IActionResult> GetFreightDeliveryPartners()
        {
            try
            {
                var response = await _repository.GetFreightDeliveryPartners();

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
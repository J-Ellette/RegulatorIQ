using Microsoft.AspNetCore.Mvc;
using RegulatorIQ.Services;
using RegulatorIQ.DTOs;

namespace RegulatorIQ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgenciesController : ControllerBase
    {
        private readonly IAgencyService _agencyService;
        private readonly ILogger<AgenciesController> _logger;

        public AgenciesController(
            IAgencyService agencyService,
            ILogger<AgenciesController> logger)
        {
            _agencyService = agencyService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<AgencyDto>>> GetAgencies(
            [FromQuery] GetAgenciesRequest request)
        {
            try
            {
                var agencies = await _agencyService.GetAgenciesAsync(request);
                return Ok(agencies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving agencies");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AgencyDto>> GetAgency(Guid id)
        {
            try
            {
                var agency = await _agencyService.GetAgencyByIdAsync(id);
                if (agency == null)
                {
                    return NotFound($"Agency with ID {id} not found");
                }

                return Ok(agency);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving agency {AgencyId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}

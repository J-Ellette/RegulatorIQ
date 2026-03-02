using Microsoft.AspNetCore.Mvc;
using RegulatorIQ.Services;
using RegulatorIQ.DTOs;

namespace RegulatorIQ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComplianceFrameworksController : ControllerBase
    {
        private readonly IComplianceFrameworkService _frameworkService;
        private readonly IChangeImpactService _impactService;
        private readonly ILogger<ComplianceFrameworksController> _logger;

        public ComplianceFrameworksController(
            IComplianceFrameworkService frameworkService,
            IChangeImpactService impactService,
            ILogger<ComplianceFrameworksController> logger)
        {
            _frameworkService = frameworkService;
            _impactService = impactService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<ComplianceFrameworkDto>>> GetFrameworks(
            [FromQuery] Guid companyId)
        {
            try
            {
                var frameworks = await _frameworkService.GetFrameworksByCompanyAsync(companyId);
                return Ok(frameworks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving frameworks for company {CompanyId}", companyId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<ComplianceFrameworkDto>> CreateFramework(
            [FromBody] CreateFrameworkRequest request)
        {
            try
            {
                var framework = await _frameworkService.CreateFrameworkAsync(request);
                return CreatedAtAction(nameof(GetFramework), new { id = framework.Id }, framework);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating compliance framework");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ComplianceFrameworkDetailDto>> GetFramework(Guid id)
        {
            try
            {
                var framework = await _frameworkService.GetFrameworkByIdAsync(id);
                if (framework == null)
                {
                    return NotFound($"Framework with ID {id} not found");
                }

                return Ok(framework);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving framework {FrameworkId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ComplianceFrameworkDto>> UpdateFramework(
            Guid id,
            [FromBody] UpdateFrameworkRequest request)
        {
            try
            {
                var framework = await _frameworkService.UpdateFrameworkAsync(id, request);
                if (framework == null)
                {
                    return NotFound($"Framework with ID {id} not found");
                }

                return Ok(framework);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating framework {FrameworkId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}/lifecycle")]
        public async Task<ActionResult<ComplianceFrameworkDto>> UpdateLifecycle(
            Guid id,
            [FromBody] FrameworkLifecycleUpdateRequest request)
        {
            try
            {
                var framework = await _frameworkService.UpdateLifecycleAsync(id, request);
                if (framework == null)
                {
                    return NotFound($"Framework with ID {id} not found");
                }

                return Ok(framework);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lifecycle for framework {FrameworkId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{id}/impact-assessment")]
        public async Task<ActionResult<ChangeImpactAssessmentDto>> AssessImpact(
            Guid id,
            [FromBody] ImpactAssessmentRequest request)
        {
            try
            {
                var assessment = await _impactService.AssessChangeImpactAsync(id, request.DocumentId);
                return Ok(assessment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assessing impact for framework {FrameworkId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{id}/sync")]
        public async Task<ActionResult<FrameworkSyncResult>> SyncFramework(Guid id)
        {
            try
            {
                var result = await _frameworkService.SyncWithLatestRegulationsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing framework {FrameworkId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}/impact-assessments")]
        public async Task<ActionResult<List<ChangeImpactAssessmentDto>>> GetImpactAssessments(Guid id)
        {
            try
            {
                var assessments = await _impactService.GetFrameworkAssessmentsAsync(id);
                return Ok(assessments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving impact assessments for framework {FrameworkId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}

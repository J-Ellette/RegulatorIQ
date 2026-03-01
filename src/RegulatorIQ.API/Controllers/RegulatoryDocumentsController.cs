using Microsoft.AspNetCore.Mvc;
using RegulatorIQ.Services;
using RegulatorIQ.DTOs;

namespace RegulatorIQ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegulatoryDocumentsController : ControllerBase
    {
        private readonly IRegulatoryDocumentService _documentService;
        private readonly IDocumentAnalysisService _analysisService;
        private readonly ILogger<RegulatoryDocumentsController> _logger;

        public RegulatoryDocumentsController(
            IRegulatoryDocumentService documentService,
            IDocumentAnalysisService analysisService,
            ILogger<RegulatoryDocumentsController> logger)
        {
            _documentService = documentService;
            _analysisService = analysisService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<RegulatoryDocumentDto>>> GetDocuments(
            [FromQuery] DocumentSearchRequest request)
        {
            try
            {
                var result = await _documentService.SearchDocumentsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving regulatory documents");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RegulatoryDocumentDetailDto>> GetDocument(Guid id)
        {
            try
            {
                var document = await _documentService.GetDocumentByIdAsync(id);
                if (document == null)
                {
                    return NotFound($"Document with ID {id} not found");
                }

                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document {DocumentId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{id}/analyze")]
        public async Task<ActionResult<DocumentAnalysisDto>> AnalyzeDocument(Guid id)
        {
            try
            {
                var analysis = await _analysisService.AnalyzeDocumentAsync(id);
                if (analysis == null)
                {
                    return NotFound($"Document with ID {id} not found");
                }

                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing document {DocumentId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}/analysis")]
        public async Task<ActionResult<DocumentAnalysisDto>> GetDocumentAnalysis(Guid id)
        {
            try
            {
                var analysis = await _analysisService.GetLatestAnalysisAsync(id);
                if (analysis == null)
                {
                    return NotFound($"Analysis for document {id} not found");
                }

                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analysis for document {DocumentId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<RegulatoryDocumentDto>>> SearchDocuments(
            [FromQuery] string query,
            [FromQuery] DocumentFilter filter)
        {
            try
            {
                var results = await _documentService.FullTextSearchAsync(query, filter);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents with query: {Query}", query);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("alerts")]
        public async Task<ActionResult<List<RegulatoryAlertDto>>> GetAlerts(
            [FromQuery] AlertFilter filter)
        {
            try
            {
                var alerts = await _documentService.GetRegulatoryAlertsAsync(filter);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving regulatory alerts");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("bulk-analyze")]
        public async Task<ActionResult<BulkAnalysisResult>> BulkAnalyzeDocuments(
            [FromBody] BulkAnalysisRequest request)
        {
            try
            {
                var result = await _analysisService.BulkAnalyzeAsync(request.DocumentIds);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk analysis");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("recent")]
        public async Task<ActionResult<List<RegulatoryDocumentDto>>> GetRecentDocuments(
            [FromQuery] int count = 20)
        {
            try
            {
                var request = new DocumentSearchRequest { PageSize = count, SortBy = "date", SortDesc = true };
                var result = await _documentService.SearchDocumentsAsync(request);
                return Ok(result.Items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent documents");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStats>> GetDashboardStats(
            [FromQuery] string timeframe = "month")
        {
            try
            {
                var stats = await _documentService.GetDashboardStatsAsync(timeframe);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard stats");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}

using RegulatorIQ.Data;
using RegulatorIQ.Models;
using RegulatorIQ.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace RegulatorIQ.Services
{
    public interface IDocumentAnalysisService
    {
        Task<DocumentAnalysisDto?> AnalyzeDocumentAsync(Guid documentId);
        Task<DocumentAnalysisDto?> GetLatestAnalysisAsync(Guid documentId);
        Task<BulkAnalysisResult> BulkAnalyzeAsync(List<Guid> documentIds);
    }

    public class DocumentAnalysisService : IDocumentAnalysisService
    {
        private readonly RegulatorIQContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DocumentAnalysisService> _logger;

        public DocumentAnalysisService(
            RegulatorIQContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<DocumentAnalysisService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<DocumentAnalysisDto?> AnalyzeDocumentAsync(Guid documentId)
        {
            var document = await _context.RegulatoryDocuments.FindAsync(documentId);
            if (document == null) return null;

            try
            {
                var mlServiceUrl = _configuration["MLServices:BaseUrl"] ?? "http://ml-services:8000";
                var client = _httpClientFactory.CreateClient("MLServices");

                var requestPayload = new
                {
                    document_id = documentId.ToString(),
                    content = document.ProcessedContent ?? document.RawContent ?? document.Title,
                    document_type = document.DocumentType,
                    title = document.Title
                };

                var response = await client.PostAsJsonAsync(
                    $"{mlServiceUrl}/analyze",
                    requestPayload);

                DocumentAnalysis analysis;

                if (response.IsSuccessStatusCode)
                {
                    var mlResult = await response.Content.ReadFromJsonAsync<MlAnalysisResult>();
                    analysis = MapMlResultToAnalysis(documentId, mlResult);
                }
                else
                {
                    _logger.LogWarning("ML service returned {StatusCode} for document {DocumentId}",
                        response.StatusCode, documentId);
                    analysis = CreateDefaultAnalysis(documentId, document);
                }

                // Remove old analysis if exists
                var existing = await _context.DocumentAnalyses
                    .Where(a => a.DocumentId == documentId)
                    .ToListAsync();
                _context.DocumentAnalyses.RemoveRange(existing);

                _context.DocumentAnalyses.Add(analysis);
                await _context.SaveChangesAsync();

                return MapToDto(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing document {DocumentId}", documentId);

                // Create a basic analysis on failure
                var fallback = CreateDefaultAnalysis(documentId, document);
                _context.DocumentAnalyses.Add(fallback);
                await _context.SaveChangesAsync();

                return MapToDto(fallback);
            }
        }

        public async Task<DocumentAnalysisDto?> GetLatestAnalysisAsync(Guid documentId)
        {
            var analysis = await _context.DocumentAnalyses
                .Where(a => a.DocumentId == documentId)
                .OrderByDescending(a => a.AnalysisDate)
                .FirstOrDefaultAsync();

            return analysis != null ? MapToDto(analysis) : null;
        }

        public async Task<BulkAnalysisResult> BulkAnalyzeAsync(List<Guid> documentIds)
        {
            var result = new BulkAnalysisResult { TotalRequested = documentIds.Count };

            foreach (var id in documentIds)
            {
                try
                {
                    await AnalyzeDocumentAsync(id);
                    result.Queued++;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Errors.Add($"Document {id}: {ex.Message}");
                }
            }

            return result;
        }

        private DocumentAnalysis MapMlResultToAnalysis(Guid documentId, MlAnalysisResult? mlResult)
        {
            return new DocumentAnalysis
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                AnalysisVersion = 1,
                Classification = mlResult?.Classification != null
                    ? JsonSerializer.Serialize(mlResult.Classification) : null,
                EntitiesExtracted = mlResult?.Entities != null
                    ? JsonSerializer.Serialize(mlResult.Entities) : null,
                ComplianceRequirements = mlResult?.ComplianceRequirements != null
                    ? JsonSerializer.Serialize(mlResult.ComplianceRequirements) : null,
                ImpactAssessment = mlResult?.ImpactAssessment != null
                    ? JsonSerializer.Serialize(mlResult.ImpactAssessment) : null,
                TimelineAnalysis = mlResult?.TimelineAnalysis != null
                    ? JsonSerializer.Serialize(mlResult.TimelineAnalysis) : null,
                AffectedParties = mlResult?.AffectedParties?.ToArray(),
                Summary = mlResult?.Summary,
                ActionableItems = mlResult?.ActionableItems != null
                    ? JsonSerializer.Serialize(mlResult.ActionableItems) : null,
                RelatedRegulations = mlResult?.RelatedRegulations?.ToArray(),
                ConfidenceScore = (decimal)(mlResult?.ConfidenceScore ?? 0.5),
                AnalyzerVersion = "1.0",
                AnalysisDate = DateTime.UtcNow
            };
        }

        private static DocumentAnalysis CreateDefaultAnalysis(Guid documentId, RegulatoryDocument document)
        {
            return new DocumentAnalysis
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                AnalysisVersion = 1,
                Summary = $"Document: {document.Title}. Automated analysis pending.",
                ConfidenceScore = 0.1m,
                AnalyzerVersion = "1.0",
                AnalysisDate = DateTime.UtcNow
            };
        }

        private static DocumentAnalysisDto MapToDto(DocumentAnalysis a)
        {
            object? DeserializeOrNull(string? json)
            {
                if (string.IsNullOrEmpty(json)) return null;
                try { return JsonSerializer.Deserialize<object>(json); }
                catch { return null; }
            }

            return new DocumentAnalysisDto
            {
                Id = a.Id,
                DocumentId = a.DocumentId,
                AnalysisVersion = a.AnalysisVersion,
                Classification = DeserializeOrNull(a.Classification),
                EntitiesExtracted = DeserializeOrNull(a.EntitiesExtracted),
                ImpactAssessment = DeserializeOrNull(a.ImpactAssessment),
                TimelineAnalysis = DeserializeOrNull(a.TimelineAnalysis),
                AffectedParties = a.AffectedParties,
                Summary = a.Summary,
                ActionableItems = DeserializeOrNull(a.ActionableItems),
                RelatedRegulations = a.RelatedRegulations,
                ConfidenceScore = a.ConfidenceScore,
                AnalysisDate = a.AnalysisDate,
                AnalyzerVersion = a.AnalyzerVersion
            };
        }
    }

    internal class MlAnalysisResult
    {
        public object? Classification { get; set; }
        public object? Entities { get; set; }
        public List<object>? ComplianceRequirements { get; set; }
        public object? ImpactAssessment { get; set; }
        public object? TimelineAnalysis { get; set; }
        public List<string>? AffectedParties { get; set; }
        public string? Summary { get; set; }
        public object? ActionableItems { get; set; }
        public List<string>? RelatedRegulations { get; set; }
        public double? ConfidenceScore { get; set; }
    }
}

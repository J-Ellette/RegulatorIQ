using RegulatorIQ.Data;
using RegulatorIQ.Models;
using RegulatorIQ.DTOs;
using RegulatorIQ.Services.Analysis;
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
        private readonly IAIAnalysisProvider _aiAnalysisProvider;
        private readonly IRulesAnalysisProvider _rulesAnalysisProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DocumentAnalysisService> _logger;

        public DocumentAnalysisService(
            RegulatorIQContext context,
            IAIAnalysisProvider aiAnalysisProvider,
            IRulesAnalysisProvider rulesAnalysisProvider,
            IConfiguration configuration,
            ILogger<DocumentAnalysisService> logger)
        {
            _context = context;
            _aiAnalysisProvider = aiAnalysisProvider;
            _rulesAnalysisProvider = rulesAnalysisProvider;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<DocumentAnalysisDto?> AnalyzeDocumentAsync(Guid documentId)
        {
            var document = await _context.RegulatoryDocuments.FindAsync(documentId);
            if (document == null) return null;

            try
            {
                DocumentAnalysis analysis;
                var providerResult = await AnalyzeWithConfiguredModeAsync(document, documentId);
                analysis = providerResult != null
                    ? MapProviderResultToAnalysis(documentId, providerResult)
                    : CreateDefaultAnalysis(documentId, document);

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

        private async Task<AnalysisProviderResult?> AnalyzeWithConfiguredModeAsync(RegulatoryDocument document, Guid documentId)
        {
            var mode = (_configuration["Analysis:Mode"] ?? "Auto").Trim().ToLowerInvariant();

            switch (mode)
            {
                case "ai":
                    return await TryAiAsync(document, documentId);

                case "rules":
                    return await _rulesAnalysisProvider.AnalyzeAsync(document);

                default:
                    var aiResult = await TryAiAsync(document, documentId);
                    if (aiResult != null)
                    {
                        return aiResult;
                    }

                    _logger.LogInformation("Falling back to rules analysis for document {DocumentId}", documentId);
                    return await _rulesAnalysisProvider.AnalyzeAsync(document);
            }
        }

        private async Task<AnalysisProviderResult?> TryAiAsync(RegulatoryDocument document, Guid documentId)
        {
            try
            {
                return await _aiAnalysisProvider.AnalyzeAsync(document);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI analysis failed for document {DocumentId}", documentId);
                return null;
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

        private DocumentAnalysis MapProviderResultToAnalysis(Guid documentId, AnalysisProviderResult providerResult)
        {
            return new DocumentAnalysis
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                AnalysisVersion = 1,
                Classification = providerResult.Classification != null
                    ? JsonSerializer.Serialize(providerResult.Classification) : null,
                EntitiesExtracted = providerResult.Entities != null
                    ? JsonSerializer.Serialize(providerResult.Entities) : null,
                ComplianceRequirements = providerResult.ComplianceRequirements != null
                    ? JsonSerializer.Serialize(providerResult.ComplianceRequirements) : null,
                ImpactAssessment = providerResult.ImpactAssessment != null
                    ? JsonSerializer.Serialize(providerResult.ImpactAssessment) : null,
                TimelineAnalysis = providerResult.TimelineAnalysis != null
                    ? JsonSerializer.Serialize(providerResult.TimelineAnalysis) : null,
                AffectedParties = providerResult.AffectedParties?.ToArray(),
                Summary = providerResult.Summary,
                ActionableItems = providerResult.ActionableItems != null
                    ? JsonSerializer.Serialize(providerResult.ActionableItems) : null,
                RelatedRegulations = providerResult.RelatedRegulations?.ToArray(),
                ConfidenceScore = (decimal)(providerResult.ConfidenceScore ?? 0.5),
                AnalyzerVersion = providerResult.ProviderName,
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
                AnalysisProvider = a.AnalyzerVersion,
                AnalyzerVersion = a.AnalyzerVersion
            };
        }
    }

}

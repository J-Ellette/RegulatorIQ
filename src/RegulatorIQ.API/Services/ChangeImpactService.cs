using RegulatorIQ.Data;
using RegulatorIQ.Models;
using RegulatorIQ.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RegulatorIQ.Hubs;

namespace RegulatorIQ.Services
{
    public interface IChangeImpactService
    {
        Task<ChangeImpactAssessmentDto> AssessChangeImpactAsync(Guid frameworkId, Guid documentId);
        Task<List<ChangeImpactAssessmentDto>> GetFrameworkAssessmentsAsync(Guid frameworkId);
    }

    public class ChangeImpactService : IChangeImpactService
    {
        private readonly RegulatorIQContext _context;
        private readonly ILogger<ChangeImpactService> _logger;
        private readonly IHubContext<NotificationsHub> _hubContext;

        public ChangeImpactService(
            RegulatorIQContext context,
            ILogger<ChangeImpactService> logger,
            IHubContext<NotificationsHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task<ChangeImpactAssessmentDto> AssessChangeImpactAsync(Guid frameworkId, Guid documentId)
        {
            var framework = await _context.ComplianceFrameworks
                .Include(f => f.RegulationMappings)
                .FirstOrDefaultAsync(f => f.Id == frameworkId)
                ?? throw new KeyNotFoundException($"Framework {frameworkId} not found");

            var document = await _context.RegulatoryDocuments
                .Include(d => d.Analyses)
                .Include(d => d.ComplianceRequirements)
                .FirstOrDefaultAsync(d => d.Id == documentId)
                ?? throw new KeyNotFoundException($"Document {documentId} not found");

            var latestAnalysis = document.Analyses
                .OrderByDescending(a => a.AnalysisDate)
                .FirstOrDefault();

            // Calculate impact score based on priority and analysis data
            decimal impactScore = CalculateImpactScore(document, latestAnalysis, framework);

            var affectedProcesses = DetermineAffectedProcesses(document, framework);
            var requiredUpdates = DetermineRequiredUpdates(document, framework);
            var riskLevel = DetermineRiskLevel(impactScore);

            var assessment = new ChangeImpactAssessment
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                FrameworkId = frameworkId,
                ImpactScore = impactScore,
                AffectedProcesses = JsonSerializer.Serialize(affectedProcesses),
                RequiredUpdates = JsonSerializer.Serialize(requiredUpdates),
                TimelineConflicts = JsonSerializer.Serialize(new List<object>()),
                EstimatedCost = EstimateCost(impactScore),
                ImplementationComplexity = (int)Math.Ceiling((double)impactScore / 2),
                RiskLevel = riskLevel,
                AssessmentDate = DateTime.UtcNow,
                AssessedBy = "system"
            };

            _context.ChangeImpactAssessments.Add(assessment);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("impactAssessmentCompleted", new
            {
                frameworkId,
                documentId,
                assessmentId = assessment.Id,
                impactScore = assessment.ImpactScore,
                riskLevel = assessment.RiskLevel,
                assessmentDate = assessment.AssessmentDate,
                documentTitle = document.Title
            });

            return MapToDto(assessment);
        }

        public async Task<List<ChangeImpactAssessmentDto>> GetFrameworkAssessmentsAsync(Guid frameworkId)
        {
            var assessments = await _context.ChangeImpactAssessments
                .Where(a => a.FrameworkId == frameworkId)
                .OrderByDescending(a => a.AssessmentDate)
                .ToListAsync();

            return assessments.Select(MapToDto).ToList();
        }

        private static decimal CalculateImpactScore(
            RegulatoryDocument document,
            DocumentAnalysis? analysis,
            ComplianceFramework framework)
        {
            decimal score = document.PriorityScore / 2.0m;

            if (document.EffectiveDate.HasValue)
            {
                var daysUntilEffective = (document.EffectiveDate.Value - DateTime.UtcNow).Days;
                if (daysUntilEffective <= 30) score += 3;
                else if (daysUntilEffective <= 90) score += 2;
                else score += 1;
            }

            if (document.ComplianceRequirements?.Any() == true)
                score += Math.Min(document.ComplianceRequirements.Count, 5);

            return Math.Min(score, 10m);
        }

        private static List<string> DetermineAffectedProcesses(
            RegulatoryDocument document,
            ComplianceFramework framework)
        {
            var processes = new List<string>();

            if (document.DocumentType?.Contains("safety", StringComparison.OrdinalIgnoreCase) == true)
                processes.Add("Safety Management System");

            if (document.DocumentType?.Contains("environmental", StringComparison.OrdinalIgnoreCase) == true ||
                document.Title.Contains("environmental", StringComparison.OrdinalIgnoreCase))
                processes.Add("Environmental Compliance Program");

            if (document.Title.Contains("reporting", StringComparison.OrdinalIgnoreCase))
                processes.Add("Regulatory Reporting");

            if (document.Title.Contains("pipeline", StringComparison.OrdinalIgnoreCase))
                processes.Add("Pipeline Operations");

            if (!processes.Any())
                processes.Add("General Compliance Operations");

            return processes;
        }

        private static List<string> DetermineRequiredUpdates(
            RegulatoryDocument document,
            ComplianceFramework framework)
        {
            return new List<string>
            {
                $"Review and update compliance framework to incorporate {document.Title}",
                "Update internal policies and procedures",
                "Train relevant staff on new requirements",
                "Update compliance tracking documentation"
            };
        }

        private static string DetermineRiskLevel(decimal impactScore) => impactScore switch
        {
            >= 8 => "critical",
            >= 6 => "high",
            >= 4 => "medium",
            _ => "low"
        };

        private static decimal EstimateCost(decimal impactScore) =>
            impactScore * 15000m;

        private static ChangeImpactAssessmentDto MapToDto(ChangeImpactAssessment a)
        {
            object? DeserializeOrNull(string? json)
            {
                if (string.IsNullOrEmpty(json)) return null;
                try { return JsonSerializer.Deserialize<object>(json); }
                catch { return null; }
            }

            return new ChangeImpactAssessmentDto
            {
                Id = a.Id,
                DocumentId = a.DocumentId,
                FrameworkId = a.FrameworkId,
                ImpactScore = a.ImpactScore,
                AffectedProcesses = DeserializeOrNull(a.AffectedProcesses),
                RequiredUpdates = DeserializeOrNull(a.RequiredUpdates),
                TimelineConflicts = DeserializeOrNull(a.TimelineConflicts),
                EstimatedCost = a.EstimatedCost,
                ImplementationComplexity = a.ImplementationComplexity,
                RiskLevel = a.RiskLevel,
                AssessmentDate = a.AssessmentDate,
                AssessedBy = a.AssessedBy
            };
        }
    }
}

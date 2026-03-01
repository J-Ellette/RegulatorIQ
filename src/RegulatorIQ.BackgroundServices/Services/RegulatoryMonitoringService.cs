using Hangfire;
using RegulatorIQ.Data;
using RegulatorIQ.Models;
using RegulatorIQ.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace RegulatorIQ.Services.BackgroundServices
{
    public interface IRegulatoryMonitoringService
    {
        Task MonitorFederalRegulationsAsync();
        Task MonitorStateRegulationsAsync();
        Task ProcessPendingDocumentsAsync();
        Task GenerateComplianceAlertsAsync();
        Task UpdateComplianceFrameworksAsync();
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public class RegulatoryMonitoringService : IRegulatoryMonitoringService
    {
        private readonly RegulatorIQContext _context;
        private readonly IDocumentAnalysisService _analysisService;
        private readonly IChangeImpactService _changeImpactService;
        private readonly ILogger<RegulatoryMonitoringService> _logger;

        public RegulatoryMonitoringService(
            RegulatorIQContext context,
            IDocumentAnalysisService analysisService,
            IChangeImpactService changeImpactService,
            ILogger<RegulatoryMonitoringService> logger)
        {
            _context = context;
            _analysisService = analysisService;
            _changeImpactService = changeImpactService;
            _logger = logger;
        }

        public async Task MonitorFederalRegulationsAsync()
        {
            _logger.LogInformation("Starting federal regulatory monitoring");

            try
            {
                // Monitor Federal Register API
                var federalRegisterDocs = await FetchFederalRegisterDocumentsAsync();
                await ProcessNewDocuments(federalRegisterDocs, "Federal Register");

                _logger.LogInformation("Federal regulatory monitoring completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during federal regulatory monitoring");
                throw;
            }
        }

        public async Task MonitorStateRegulationsAsync()
        {
            _logger.LogInformation("Starting state regulatory monitoring");

            try
            {
                var states = new[] { "Texas", "Oklahoma", "Louisiana", "New Mexico", "Arkansas" };

                foreach (var state in states)
                {
                    _logger.LogInformation("Monitoring {State} regulations", state);
                    // Placeholder: In production, implement state-specific scrapers
                    await Task.Delay(100);
                }

                _logger.LogInformation("State regulatory monitoring completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during state regulatory monitoring");
                throw;
            }
        }

        public async Task ProcessPendingDocumentsAsync()
        {
            _logger.LogInformation("Processing pending documents");

            try
            {
                var pendingDocuments = await _context.RegulatoryDocuments
                    .Where(d => !_context.DocumentAnalyses.Any(a => a.DocumentId == d.Id))
                    .OrderByDescending(d => d.PriorityScore)
                    .ThenBy(d => d.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                foreach (var document in pendingDocuments)
                {
                    try
                    {
                        _logger.LogInformation("Analyzing document {DocumentId}: {Title}",
                            document.Id, document.Title);

                        BackgroundJob.Enqueue<IDocumentAnalysisService>(
                            service => service.AnalyzeDocumentAsync(document.Id));

                        await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error queuing document {DocumentId}", document.Id);
                    }
                }

                _logger.LogInformation("Document processing queuing completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during document processing");
                throw;
            }
        }

        public async Task GenerateComplianceAlertsAsync()
        {
            _logger.LogInformation("Generating compliance alerts");

            try
            {
                await CheckApproachingDeadlinesAsync();
                await CheckHighImpactRegulationsAsync();
                await CheckComplianceGapsAsync();

                _logger.LogInformation("Compliance alert generation completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during compliance alert generation");
                throw;
            }
        }

        public async Task UpdateComplianceFrameworksAsync()
        {
            _logger.LogInformation("Updating compliance frameworks");

            try
            {
                var frameworks = await _context.ComplianceFrameworks
                    .Where(f => f.LastUpdated < DateTime.UtcNow.AddDays(-1))
                    .ToListAsync();

                foreach (var framework in frameworks)
                {
                    try
                    {
                        await UpdateFrameworkWithNewRegulations(framework);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating framework {FrameworkId}", framework.Id);
                    }
                }

                _logger.LogInformation("Framework updates completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during framework updates");
                throw;
            }
        }

        private async Task<List<Dictionary<string, object>>> FetchFederalRegisterDocumentsAsync()
        {
            // Placeholder for Federal Register API integration
            return new List<Dictionary<string, object>>();
        }

        private async Task ProcessNewDocuments(
            List<Dictionary<string, object>> documents,
            string source)
        {
            foreach (var docData in documents)
            {
                try
                {
                    var documentId = docData["document_id"]?.ToString();
                    if (string.IsNullOrEmpty(documentId)) continue;

                    var existingDoc = await _context.RegulatoryDocuments
                        .FirstOrDefaultAsync(d => d.DocumentId == documentId);

                    if (existingDoc == null)
                    {
                        var agency = await _context.RegulatoryAgencies
                            .FirstOrDefaultAsync(a => a.Name.Contains(source) || a.Abbreviation == source);

                        var newDocument = new RegulatoryDocument
                        {
                            Id = Guid.NewGuid(),
                            DocumentId = documentId,
                            Title = docData.GetValueOrDefault("title")?.ToString() ?? "Untitled",
                            DocumentType = docData.GetValueOrDefault("document_type")?.ToString(),
                            PublicationDate = ParseDate(docData.GetValueOrDefault("publication_date")),
                            EffectiveDate = ParseDate(docData.GetValueOrDefault("effective_date")),
                            SourceUrl = docData.GetValueOrDefault("url")?.ToString(),
                            PdfUrl = docData.GetValueOrDefault("pdf_url")?.ToString(),
                            RawContent = docData.GetValueOrDefault("raw_content")?.ToString(),
                            DocketNumber = docData.GetValueOrDefault("docket_number")?.ToString(),
                            PriorityScore = Convert.ToInt32(docData.GetValueOrDefault("priority_score") ?? 0),
                            AgencyId = agency?.Id
                        };

                        _context.RegulatoryDocuments.Add(newDocument);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Added new document {DocumentId} from {Source}",
                            documentId, source);

                        if (newDocument.PriorityScore >= 10)
                        {
                            BackgroundJob.Enqueue<IDocumentAnalysisService>(
                                service => service.AnalyzeDocumentAsync(newDocument.Id));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing document from {Source}", source);
                }
            }
        }

        private async Task CheckApproachingDeadlinesAsync()
        {
            var upcomingDeadlines = await _context.ComplianceRequirements
                .Include(cr => cr.Document)
                .ThenInclude(d => d!.Agency)
                .Where(cr => cr.Deadline.HasValue &&
                             cr.Deadline.Value <= DateTime.UtcNow.AddDays(90) &&
                             cr.Deadline.Value > DateTime.UtcNow)
                .ToListAsync();

            foreach (var requirement in upcomingDeadlines)
            {
                var daysUntilDeadline = (requirement.Deadline!.Value - DateTime.UtcNow.Date).Days;

                // Check if an alert already exists
                var existingAlert = await _context.RegulatoryAlerts
                    .AnyAsync(a => a.DocumentId == requirement.DocumentId &&
                                   a.AlertType == "deadline_approaching" &&
                                   a.Status == "active");

                if (!existingAlert)
                {
                    var alert = new RegulatoryAlert
                    {
                        Id = Guid.NewGuid(),
                        AlertType = "deadline_approaching",
                        DocumentId = requirement.DocumentId,
                        Severity = GetDeadlineSeverity(daysUntilDeadline),
                        Title = $"Compliance Deadline Approaching: {TruncateText(requirement.RequirementText, 100)}",
                        Message = $"Deadline in {daysUntilDeadline} days: {requirement.Deadline:yyyy-MM-dd}",
                        AlertData = JsonSerializer.Serialize(new
                        {
                            RequirementId = requirement.Id,
                            DaysRemaining = daysUntilDeadline,
                            Severity = requirement.Severity
                        }),
                        Status = "active"
                    };

                    _context.RegulatoryAlerts.Add(alert);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task CheckHighImpactRegulationsAsync()
        {
            var recentHighImpactDocs = await _context.RegulatoryDocuments
                .Include(d => d.Analyses)
                .Include(d => d.Agency)
                .Where(d => d.CreatedAt >= DateTime.UtcNow.AddHours(-24) &&
                            d.PriorityScore >= 15)
                .ToListAsync();

            foreach (var document in recentHighImpactDocs)
            {
                var latestAnalysis = document.Analyses
                    .OrderByDescending(a => a.AnalysisDate)
                    .FirstOrDefault();

                if (latestAnalysis?.ImpactAssessment != null)
                {
                    try
                    {
                        var impactData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                            latestAnalysis.ImpactAssessment);
                        var impactScore = impactData?.TryGetValue("impact_score", out var scoreEl) == true
                            ? scoreEl.GetDouble()
                            : 0.0;

                        if (impactScore >= 7.0)
                        {
                            var alert = new RegulatoryAlert
                            {
                                Id = Guid.NewGuid(),
                                AlertType = "high_impact_regulation",
                                DocumentId = document.Id,
                                Severity = "high",
                                Title = $"High-Impact Regulation: {TruncateText(document.Title, 200)}",
                                Message = $"New regulation with impact score {impactScore:F1} requires immediate review",
                                AlertData = JsonSerializer.Serialize(new
                                {
                                    ImpactScore = impactScore,
                                    DocumentType = document.DocumentType,
                                    Agency = document.Agency?.Name
                                }),
                                Status = "active"
                            };

                            _context.RegulatoryAlerts.Add(alert);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing impact data for document {DocumentId}", document.Id);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task CheckComplianceGapsAsync()
        {
            var gapCount = await _context.FrameworkRegulationMappings
                .CountAsync(m => m.ComplianceStatus == "non-compliant" ||
                                 m.ComplianceStatus == "needs_review");

            if (gapCount > 0)
            {
                _logger.LogInformation("Found {GapCount} compliance gaps requiring attention", gapCount);
            }
        }

        private async Task UpdateFrameworkWithNewRegulations(ComplianceFramework framework)
        {
            var newRegulations = await _context.RegulatoryDocuments
                .Where(d => d.CreatedAt > framework.LastUpdated)
                .ToListAsync();

            foreach (var regulation in newRegulations)
            {
                BackgroundJob.Enqueue<IChangeImpactService>(
                    service => service.AssessChangeImpactAsync(framework.Id, regulation.Id));
            }

            framework.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private static string GetDeadlineSeverity(int daysUntilDeadline) => daysUntilDeadline switch
        {
            <= 7 => "critical",
            <= 30 => "high",
            <= 60 => "medium",
            _ => "low"
        };

        private static string TruncateText(string text, int maxLength) =>
            text.Length <= maxLength ? text : text[..maxLength];

        private static DateTime? ParseDate(object? dateValue)
        {
            if (dateValue == null) return null;
            return DateTime.TryParse(dateValue.ToString(), out var result) ? result : null;
        }
    }

    public static class HangfireExtensions
    {
        // Support both Windows ('Central Standard Time') and Linux/macOS ('America/Chicago')
        private static TimeZoneInfo GetCentralTimeZone()
        {
            foreach (var id in new[] { "Central Standard Time", "America/Chicago" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (TimeZoneNotFoundException) { }
            }
            return TimeZoneInfo.Utc;
        }

        public static void ConfigureRegulatoryMonitoring(this IRecurringJobManager recurringJobs)
        {
            var centralTimeZone = GetCentralTimeZone();

            recurringJobs.AddOrUpdate<IRegulatoryMonitoringService>(
                "federal-monitoring",
                service => service.MonitorFederalRegulationsAsync(),
                "0 */4 * * *",
                new RecurringJobOptions { TimeZone = centralTimeZone });

            recurringJobs.AddOrUpdate<IRegulatoryMonitoringService>(
                "state-monitoring",
                service => service.MonitorStateRegulationsAsync(),
                "0 2,14 * * *",
                new RecurringJobOptions { TimeZone = centralTimeZone });

            recurringJobs.AddOrUpdate<IRegulatoryMonitoringService>(
                "document-processing",
                service => service.ProcessPendingDocumentsAsync(),
                "*/15 * * * *");

            recurringJobs.AddOrUpdate<IRegulatoryMonitoringService>(
                "compliance-alerts",
                service => service.GenerateComplianceAlertsAsync(),
                "0 8,16 * * *",
                new RecurringJobOptions { TimeZone = centralTimeZone });

            recurringJobs.AddOrUpdate<IRegulatoryMonitoringService>(
                "framework-updates",
                service => service.UpdateComplianceFrameworksAsync(),
                "0 3 * * *",
                new RecurringJobOptions { TimeZone = centralTimeZone });
        }
    }
}

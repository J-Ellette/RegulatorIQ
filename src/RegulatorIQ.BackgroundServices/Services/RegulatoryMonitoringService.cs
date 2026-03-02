using Hangfire;
using RegulatorIQ.Data;
using RegulatorIQ.Models;
using RegulatorIQ.Services;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegulatoryMonitoringService> _logger;

        public RegulatoryMonitoringService(
            RegulatorIQContext context,
            IDocumentAnalysisService analysisService,
            IChangeImpactService changeImpactService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<RegulatoryMonitoringService> logger)
        {
            _context = context;
            _analysisService = analysisService;
            _changeImpactService = changeImpactService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task MonitorFederalRegulationsAsync()
        {
            if (IsJobPaused("federal-monitoring"))
            {
                _logger.LogInformation("Skipping federal monitoring because job is paused");
                return;
            }

            _logger.LogInformation("Starting federal regulatory monitoring");

            var run = await StartMonitoringRunAsync("federal");
            var fetchedCount = 0;
            IngestionResult? ingestion = null;

            try
            {
                var federalRegisterDocs = await FetchFederalRegisterDocumentsAsync();
                fetchedCount = federalRegisterDocs.Count;
                ingestion = await ProcessNewDocuments(federalRegisterDocs, "Federal Register");

                await CompleteMonitoringRunAsync(
                    run,
                    status: "completed",
                    fetchedCount,
                    ingestion.DocumentsAdded,
                    ingestion.DocumentsSkipped,
                    ingestion.FailureCount,
                    ingestion.SourceMetrics,
                    null);

                _logger.LogInformation("Federal regulatory monitoring completed successfully");
            }
            catch (Exception ex)
            {
                await CompleteMonitoringRunAsync(
                    run,
                    status: "failed",
                    fetchedCount,
                    ingestion?.DocumentsAdded ?? 0,
                    ingestion?.DocumentsSkipped ?? 0,
                    (ingestion?.FailureCount ?? 0) + 1,
                    ingestion?.SourceMetrics,
                    ex.Message);

                _logger.LogError(ex, "Error during federal regulatory monitoring");
                throw;
            }
        }

        public async Task MonitorStateRegulationsAsync()
        {
            if (IsJobPaused("state-monitoring"))
            {
                _logger.LogInformation("Skipping state monitoring because job is paused");
                return;
            }

            _logger.LogInformation("Starting state regulatory monitoring");

            var run = await StartMonitoringRunAsync("state");
            var fetchedCount = 0;
            IngestionResult? ingestion = null;

            try
            {
                var stateDocuments = await FetchStateDocumentsAsync();
                fetchedCount = stateDocuments.Count;
                ingestion = await ProcessNewDocuments(stateDocuments, "State Monitor");

                await CompleteMonitoringRunAsync(
                    run,
                    status: "completed",
                    fetchedCount,
                    ingestion.DocumentsAdded,
                    ingestion.DocumentsSkipped,
                    ingestion.FailureCount,
                    ingestion.SourceMetrics,
                    null);

                _logger.LogInformation("State regulatory monitoring completed successfully");
            }
            catch (Exception ex)
            {
                await CompleteMonitoringRunAsync(
                    run,
                    status: "failed",
                    fetchedCount,
                    ingestion?.DocumentsAdded ?? 0,
                    ingestion?.DocumentsSkipped ?? 0,
                    (ingestion?.FailureCount ?? 0) + 1,
                    ingestion?.SourceMetrics,
                    ex.Message);

                _logger.LogError(ex, "Error during state regulatory monitoring");
                throw;
            }
        }

        public async Task ProcessPendingDocumentsAsync()
        {
            if (IsJobPaused("document-processing"))
            {
                _logger.LogInformation("Skipping document processing because job is paused");
                return;
            }

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
            if (IsJobPaused("compliance-alerts"))
            {
                _logger.LogInformation("Skipping compliance alerts generation because job is paused");
                return;
            }

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
            if (IsJobPaused("framework-updates"))
            {
                _logger.LogInformation("Skipping framework updates because job is paused");
                return;
            }

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

        private async Task<List<Dictionary<string, object?>>> FetchFederalRegisterDocumentsAsync()
        {
            var defaultSources = new[] { "federal_register", "ferc", "doe", "epa", "phmsa" };
            return await FetchDocumentsFromMonitoringEndpointAsync("/monitor/federal", defaultSources, "federal");
        }

        private async Task<List<Dictionary<string, object?>>> FetchStateDocumentsAsync()
        {
            var defaultSources = new[] { "texas" };
            return await FetchDocumentsFromMonitoringEndpointAsync("/monitor/state", defaultSources, "state");
        }

        private async Task<List<Dictionary<string, object?>>> FetchDocumentsFromMonitoringEndpointAsync(
            string endpoint,
            string[] defaultSources,
            string sourceType)
        {
            try
            {
                var configuredSources = _configuration.GetSection($"Monitoring:{sourceType}:Sources").Get<string[]>();
                var sources = configuredSources is { Length: > 0 } ? configuredSources : defaultSources;

                var mlServiceUrl = (_configuration["MLServices:BaseUrl"] ?? "http://ml-services:8000").TrimEnd('/');
                var timeoutSeconds = _configuration.GetValue<int>("Monitoring:RequestTimeoutSeconds", 60);

                var client = _httpClientFactory.CreateClient("MLServices");
                client.Timeout = TimeSpan.FromSeconds(Math.Max(10, timeoutSeconds));

                var payload = new { sources };
                var response = await client.PostAsJsonAsync($"{mlServiceUrl}{endpoint}", payload);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Monitoring endpoint {Endpoint} returned status {StatusCode}", endpoint, response.StatusCode);
                    return new List<Dictionary<string, object?>>();
                }

                var body = await response.Content.ReadAsStringAsync();
                using var json = JsonDocument.Parse(body);

                if (!json.RootElement.TryGetProperty("documents", out var documentsNode) ||
                    documentsNode.ValueKind != JsonValueKind.Object)
                {
                    _logger.LogWarning("Monitoring endpoint {Endpoint} did not return a valid documents payload", endpoint);
                    return new List<Dictionary<string, object?>>();
                }

                var flattened = new List<Dictionary<string, object?>>();
                foreach (var sourceBucket in documentsNode.EnumerateObject())
                {
                    if (sourceBucket.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var docEl in sourceBucket.Value.EnumerateArray())
                    {
                        if (docEl.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(docEl.GetRawText())
                            ?? new Dictionary<string, JsonElement>();

                        var record = new Dictionary<string, object?>();
                        foreach (var kvp in parsed)
                        {
                            record[kvp.Key] = ConvertJsonElement(kvp.Value);
                        }

                        if (!record.ContainsKey("source") || string.IsNullOrWhiteSpace(record["source"]?.ToString()))
                        {
                            record["source"] = sourceBucket.Name;
                        }

                        flattened.Add(record);
                    }
                }

                _logger.LogInformation(
                    "Fetched {Count} {SourceType} documents from {Endpoint}",
                    flattened.Count,
                    sourceType,
                    endpoint);

                return flattened;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching documents from endpoint {Endpoint}", endpoint);
                return new List<Dictionary<string, object?>>();
            }
        }

        private async Task<IngestionResult> ProcessNewDocuments(
            List<Dictionary<string, object?>> documents,
            string source)
        {
            var result = new IngestionResult();

            foreach (var docData in documents)
            {
                var docSource = GetStringValue(docData, "source") ?? source;
                result.GetMetrics(docSource).Fetched++;

                try
                {
                    var title = GetStringValue(docData, "title") ?? "Untitled";
                    var sourceUrl = GetStringValue(docData, "url") ?? GetStringValue(docData, "source_url");

                    var publicationDate = ParseDate(GetValue(docData, "publication_date"));
                    var documentId =
                        GetStringValue(docData, "document_id") ??
                        GetStringValue(docData, "document_number") ??
                        GetStringValue(docData, "id");

                    if (string.IsNullOrWhiteSpace(documentId))
                    {
                        documentId = CreateSyntheticDocumentId(docSource, title, sourceUrl, publicationDate);
                    }

                    var existingDoc = await _context.RegulatoryDocuments
                        .FirstOrDefaultAsync(d => d.DocumentId == documentId);

                    if (existingDoc == null)
                    {
                        var agency = await _context.RegulatoryAgencies
                            .FirstOrDefaultAsync(a => a.Name.Contains(docSource) || a.Abbreviation == docSource);

                        var newDocument = new RegulatoryDocument
                        {
                            Id = Guid.NewGuid(),
                            DocumentId = documentId,
                            Title = title,
                            DocumentType = GetStringValue(docData, "document_type"),
                            PublicationDate = publicationDate,
                            EffectiveDate = ParseDate(GetValue(docData, "effective_date")),
                            SourceUrl = sourceUrl,
                            PdfUrl = GetStringValue(docData, "pdf_url"),
                            RawContent = GetStringValue(docData, "raw_content") ?? GetStringValue(docData, "summary"),
                            DocketNumber = GetStringValue(docData, "docket_number") ?? GetStringValue(docData, "docket_id"),
                            PriorityScore = ParsePriorityScore(GetValue(docData, "priority_score")),
                            AgencyId = agency?.Id
                        };

                        _context.RegulatoryDocuments.Add(newDocument);
                        await _context.SaveChangesAsync();
                        result.DocumentsAdded++;
                        result.GetMetrics(docSource).Added++;

                        _logger.LogInformation("Added new document {DocumentId} from {Source}",
                            documentId, docSource);

                        if (newDocument.PriorityScore >= 10)
                        {
                            BackgroundJob.Enqueue<IDocumentAnalysisService>(
                                service => service.AnalyzeDocumentAsync(newDocument.Id));
                        }
                    }
                    else
                    {
                        result.DocumentsSkipped++;
                        result.GetMetrics(docSource).Skipped++;
                    }
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.GetMetrics(docSource).Failures++;
                    _logger.LogError(ex, "Error processing document from {Source}", source);
                }
            }

            return result;
        }

        private async Task<MonitoringRun> StartMonitoringRunAsync(string runType)
        {
            var run = new MonitoringRun
            {
                Id = Guid.NewGuid(),
                RunType = runType,
                TriggeredBy = "hangfire",
                Status = "running",
                StartedAt = DateTime.UtcNow
            };

            _context.MonitoringRuns.Add(run);
            await _context.SaveChangesAsync();
            return run;
        }

        private async Task CompleteMonitoringRunAsync(
            MonitoringRun run,
            string status,
            int documentsFetched,
            int documentsAdded,
            int documentsSkipped,
            int failureCount,
            Dictionary<string, SourceMetric>? sourceMetrics,
            string? errorSummary)
        {
            run.Status = status;
            run.CompletedAt = DateTime.UtcNow;
            run.DocumentsFetched = documentsFetched;
            run.DocumentsAdded = documentsAdded;
            run.DocumentsSkipped = documentsSkipped;
            run.FailureCount = failureCount;
            run.SourceMetrics = sourceMetrics != null ? JsonSerializer.Serialize(sourceMetrics) : null;
            run.ErrorSummary = errorSummary;

            await _context.SaveChangesAsync();
        }

        private static object? GetValue(Dictionary<string, object?> data, string key) =>
            data.TryGetValue(key, out var value) ? value : null;

        private static string? GetStringValue(Dictionary<string, object?> data, string key) =>
            data.TryGetValue(key, out var value) ? value?.ToString() : null;

        private static int ParsePriorityScore(object? priorityScore)
        {
            if (priorityScore == null) return 0;
            if (priorityScore is int pInt) return pInt;
            if (priorityScore is long pLong) return (int)Math.Clamp(pLong, int.MinValue, int.MaxValue);
            if (priorityScore is double pDouble) return (int)Math.Round(pDouble);
            if (int.TryParse(priorityScore.ToString(), out var parsed)) return parsed;
            return 0;
        }

        private static string CreateSyntheticDocumentId(
            string source,
            string title,
            string? url,
            DateTime? publicationDate)
        {
            var seed = $"{source}|{title}|{url}|{publicationDate:yyyy-MM-dd}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
            var id = Convert.ToHexString(hash)[..16];
            return $"AUTO-{id}";
        }

        private static object? ConvertJsonElement(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var i) => i,
            JsonValueKind.Number when element.TryGetDouble(out var d) => d,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };

        private sealed class IngestionResult
        {
            public int DocumentsAdded { get; set; }
            public int DocumentsSkipped { get; set; }
            public int FailureCount { get; set; }
            public Dictionary<string, SourceMetric> SourceMetrics { get; } = new(StringComparer.OrdinalIgnoreCase);

            public SourceMetric GetMetrics(string source)
            {
                if (!SourceMetrics.TryGetValue(source, out var metric))
                {
                    metric = new SourceMetric();
                    SourceMetrics[source] = metric;
                }

                return metric;
            }
        }

        private sealed class SourceMetric
        {
            public int Fetched { get; set; }
            public int Added { get; set; }
            public int Skipped { get; set; }
            public int Failures { get; set; }
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

        private static bool IsJobPaused(string jobId)
        {
            try
            {
                using var connection = JobStorage.Current.GetConnection();
                var pausedJobs = connection.GetAllItemsFromSet(MonitoringJobControl.PausedJobsSetKey);
                return pausedJobs.Any(paused => paused.Equals(jobId, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
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

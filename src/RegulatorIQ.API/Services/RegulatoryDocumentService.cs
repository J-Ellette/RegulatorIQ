using RegulatorIQ.Data;
using RegulatorIQ.Models;
using RegulatorIQ.DTOs;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using RegulatorIQ.Hubs;
using System.Text.Json;

namespace RegulatorIQ.Services
{
    public interface IRegulatoryDocumentService
    {
        Task<PagedResult<RegulatoryDocumentDto>> SearchDocumentsAsync(DocumentSearchRequest request);
        Task<RegulatoryDocumentDetailDto?> GetDocumentByIdAsync(Guid id);
        Task<List<RegulatoryDocumentDto>> FullTextSearchAsync(string query, DocumentFilter filter);
        Task<List<RegulatoryAlertDto>> GetRegulatoryAlertsAsync(AlertFilter filter);
        Task<RegulatoryAlertDto?> AcknowledgeAlertAsync(Guid alertId, string? acknowledgedBy);
        Task<RegulatoryAlertDto?> ResolveAlertAsync(Guid alertId, string? resolvedBy, string? resolutionNotes);
        Task<RegulatoryDocument> CreateDocumentAsync(CreateDocumentRequest request);
        Task UpdateDocumentAsync(Guid id, UpdateDocumentRequest request);
        Task<DashboardStats> GetDashboardStatsAsync(string timeframe);
    }

    public class RegulatoryDocumentService : IRegulatoryDocumentService
    {
        private readonly RegulatorIQContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<RegulatoryDocumentService> _logger;
        private readonly IHubContext<NotificationsHub> _hubContext;

        public RegulatoryDocumentService(
            RegulatorIQContext context,
            IMapper mapper,
            ILogger<RegulatoryDocumentService> logger,
            IHubContext<NotificationsHub> hubContext)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task<PagedResult<RegulatoryDocumentDto>> SearchDocumentsAsync(
            DocumentSearchRequest request)
        {
            var query = _context.RegulatoryDocuments
                .Include(d => d.Agency)
                .AsQueryable();

            if (request.AgencyIds?.Any() == true)
                query = query.Where(d => d.AgencyId.HasValue && request.AgencyIds.Contains(d.AgencyId.Value));

            if (request.DocumentTypes?.Any() == true)
                query = query.Where(d => d.DocumentType != null && request.DocumentTypes.Contains(d.DocumentType));

            if (request.FromDate.HasValue)
                query = query.Where(d => d.PublicationDate >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(d => d.PublicationDate <= request.ToDate.Value);

            if (request.MinPriority.HasValue)
                query = query.Where(d => d.PriorityScore >= request.MinPriority.Value);

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(d =>
                    d.Title.ToLower().Contains(term) ||
                    (d.ProcessedContent != null && d.ProcessedContent.ToLower().Contains(term)));
            }

            query = request.SortBy?.ToLower() switch
            {
                "date" => request.SortDesc
                    ? query.OrderByDescending(d => d.PublicationDate)
                    : query.OrderBy(d => d.PublicationDate),
                "priority" => request.SortDesc
                    ? query.OrderByDescending(d => d.PriorityScore)
                    : query.OrderBy(d => d.PriorityScore),
                _ => query.OrderByDescending(d => d.PublicationDate)
            };

            var totalCount = await query.CountAsync();

            var documents = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var latestAnalysisByDocumentId = await GetLatestAnalysisByDocumentIdsAsync(documents.Select(d => d.Id));
            var documentDtos = documents
                .Select(d => MapToDto(
                    d,
                    latestAnalysisByDocumentId.TryGetValue(d.Id, out var latest) ? latest.Provider : null,
                    latestAnalysisByDocumentId.ContainsKey(d.Id)))
                .ToList();

            return new PagedResult<RegulatoryDocumentDto>
            {
                Items = documentDtos,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };
        }

        public async Task<RegulatoryDocumentDetailDto?> GetDocumentByIdAsync(Guid id)
        {
            var document = await _context.RegulatoryDocuments
                .Include(d => d.Agency)
                .Include(d => d.Analyses)
                .Include(d => d.ComplianceRequirements)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null) return null;

            return new RegulatoryDocumentDetailDto
            {
                Id = document.Id,
                DocumentId = document.DocumentId,
                Title = document.Title,
                DocumentType = document.DocumentType,
                PublicationDate = document.PublicationDate,
                EffectiveDate = document.EffectiveDate,
                CommentDeadline = document.CommentDeadline,
                ComplianceDate = document.ComplianceDate,
                SourceUrl = document.SourceUrl,
                PdfUrl = document.PdfUrl,
                ProcessedContent = document.ProcessedContent,
                DocketNumber = document.DocketNumber,
                FederalRegisterNumber = document.FederalRegisterNumber,
                CfrCitation = document.CfrCitation,
                Status = document.Status,
                PriorityScore = document.PriorityScore,
                CreatedAt = document.CreatedAt,
                Agency = document.Agency != null ? new AgencyDto
                {
                    Id = document.Agency.Id,
                    Name = document.Agency.Name,
                    Abbreviation = document.Agency.Abbreviation,
                    AgencyType = document.Agency.AgencyType,
                    Jurisdiction = document.Agency.Jurisdiction
                } : null,
                AnalysisStatus = document.Analyses.Any() ? "completed" : "pending",
                AnalysisProvider = document.Analyses
                    .OrderByDescending(a => a.AnalysisDate)
                    .Select(a => a.AnalyzerVersion)
                    .FirstOrDefault()
            };
        }

        public async Task<List<RegulatoryDocumentDto>> FullTextSearchAsync(
            string query,
            DocumentFilter filter)
        {
            var searchQuery = _context.RegulatoryDocuments
                .Include(d => d.Agency)
                .Where(d =>
                    d.Title.ToLower().Contains(query.ToLower()) ||
                    (d.ProcessedContent != null && d.ProcessedContent.ToLower().Contains(query.ToLower())));

            if (filter.AgencyIds?.Any() == true)
                searchQuery = searchQuery.Where(d => d.AgencyId.HasValue && filter.AgencyIds.Contains(d.AgencyId.Value));

            if (filter.EffectiveDateFrom.HasValue)
                searchQuery = searchQuery.Where(d => d.EffectiveDate >= filter.EffectiveDateFrom);

            if (filter.EffectiveDateTo.HasValue)
                searchQuery = searchQuery.Where(d => d.EffectiveDate <= filter.EffectiveDateTo);

            var results = await searchQuery
                .OrderByDescending(d => d.PriorityScore)
                .ThenByDescending(d => d.PublicationDate)
                .Take(100)
                .ToListAsync();

            var latestAnalysisByDocumentId = await GetLatestAnalysisByDocumentIdsAsync(results.Select(d => d.Id));
            return results
                .Select(d => MapToDto(
                    d,
                    latestAnalysisByDocumentId.TryGetValue(d.Id, out var latest) ? latest.Provider : null,
                    latestAnalysisByDocumentId.ContainsKey(d.Id)))
                .ToList();
        }

        public async Task<List<RegulatoryAlertDto>> GetRegulatoryAlertsAsync(AlertFilter filter)
        {
            var query = _context.RegulatoryAlerts
                .Include(a => a.Document)
                .ThenInclude(d => d!.Agency)
                .AsQueryable();

            if (filter.Severity?.Any() == true)
                query = query.Where(a => a.Severity != null && filter.Severity.Contains(a.Severity));

            if (filter.Status?.Any() == true)
                query = query.Where(a => filter.Status.Contains(a.Status));

            if (filter.AlertTypes?.Any() == true)
                query = query.Where(a => a.AlertType != null && filter.AlertTypes.Contains(a.AlertType));

            var alerts = await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(100)
                .ToListAsync();

            return alerts.Select(a => new RegulatoryAlertDto
            {
                Id = a.Id,
                AlertType = a.AlertType,
                DocumentId = a.DocumentId,
                FrameworkId = a.FrameworkId,
                Severity = a.Severity,
                Title = a.Title,
                Message = a.Message,
                Status = a.Status,
                CreatedAt = a.CreatedAt,
                AcknowledgedAt = a.AcknowledgedAt,
                AcknowledgedBy = a.AcknowledgedBy,
                ResolvedAt = a.ResolvedAt,
                ResolvedBy = a.ResolvedBy,
                ResolutionNotes = a.ResolutionNotes,
                Document = a.Document != null ? MapToDto(a.Document) : null
            }).ToList();
        }

        public async Task<RegulatoryAlertDto?> AcknowledgeAlertAsync(Guid alertId, string? acknowledgedBy)
        {
            var alert = await _context.RegulatoryAlerts
                .Include(a => a.Document)
                .ThenInclude(d => d!.Agency)
                .FirstOrDefaultAsync(a => a.Id == alertId);

            if (alert == null)
            {
                return null;
            }

            if (alert.Status == "acknowledged")
            {
                return MapAlertToDto(alert);
            }

            var oldData = JsonSerializer.Serialize(new
            {
                alert.Status,
                alert.AcknowledgedAt,
                alert.AcknowledgedBy
            });

            alert.Status = "acknowledged";
            alert.AcknowledgedAt = DateTime.UtcNow;
            alert.AcknowledgedBy = string.IsNullOrWhiteSpace(acknowledgedBy) ? "system" : acknowledgedBy.Trim();

            var newData = JsonSerializer.Serialize(new
            {
                alert.Status,
                alert.AcknowledgedAt,
                alert.AcknowledgedBy
            });

            _context.RegulatoryAuditLogs.Add(new RegulatoryAuditLog
            {
                Id = Guid.NewGuid(),
                Action = "alert_acknowledged",
                EntityType = "regulatory_alert",
                EntityId = alert.Id,
                OldData = oldData,
                NewData = newData,
                ChangedBy = alert.AcknowledgedBy,
                ChangeReason = "Alert acknowledged by user",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("alertAcknowledged", new
            {
                alertId = alert.Id,
                title = alert.Title,
                frameworkId = alert.FrameworkId,
                acknowledgedBy = alert.AcknowledgedBy,
                acknowledgedAt = alert.AcknowledgedAt
            });

            return MapAlertToDto(alert);
        }

        public async Task<RegulatoryAlertDto?> ResolveAlertAsync(Guid alertId, string? resolvedBy, string? resolutionNotes)
        {
            var alert = await _context.RegulatoryAlerts
                .Include(a => a.Document)
                .ThenInclude(d => d!.Agency)
                .FirstOrDefaultAsync(a => a.Id == alertId);

            if (alert == null)
            {
                return null;
            }

            if (alert.Status == "resolved")
            {
                return MapAlertToDto(alert);
            }

            var oldData = JsonSerializer.Serialize(new
            {
                alert.Status,
                alert.ResolvedAt,
                alert.ResolvedBy,
                alert.ResolutionNotes
            });

            alert.Status = "resolved";
            alert.ResolvedAt = DateTime.UtcNow;
            alert.ResolvedBy = string.IsNullOrWhiteSpace(resolvedBy) ? "system" : resolvedBy.Trim();
            alert.ResolutionNotes = string.IsNullOrWhiteSpace(resolutionNotes)
                ? "Resolved via alert workflow"
                : resolutionNotes.Trim();

            var newData = JsonSerializer.Serialize(new
            {
                alert.Status,
                alert.ResolvedAt,
                alert.ResolvedBy,
                alert.ResolutionNotes
            });

            _context.RegulatoryAuditLogs.Add(new RegulatoryAuditLog
            {
                Id = Guid.NewGuid(),
                Action = "alert_resolved",
                EntityType = "regulatory_alert",
                EntityId = alert.Id,
                OldData = oldData,
                NewData = newData,
                ChangedBy = alert.ResolvedBy,
                ChangeReason = alert.ResolutionNotes,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("alertResolved", new
            {
                alertId = alert.Id,
                title = alert.Title,
                frameworkId = alert.FrameworkId,
                resolvedBy = alert.ResolvedBy,
                resolvedAt = alert.ResolvedAt,
                resolutionNotes = alert.ResolutionNotes
            });

            return MapAlertToDto(alert);
        }

        public async Task<RegulatoryDocument> CreateDocumentAsync(CreateDocumentRequest request)
        {
            var document = new RegulatoryDocument
            {
                Id = Guid.NewGuid(),
                AgencyId = request.AgencyId,
                DocumentId = request.DocumentId,
                Title = request.Title,
                DocumentType = request.DocumentType,
                PublicationDate = request.PublicationDate,
                EffectiveDate = request.EffectiveDate,
                SourceUrl = request.SourceUrl,
                PdfUrl = request.PdfUrl,
                RawContent = request.RawContent,
                DocketNumber = request.DocketNumber,
                PriorityScore = request.PriorityScore
            };

            _context.RegulatoryDocuments.Add(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task UpdateDocumentAsync(Guid id, UpdateDocumentRequest request)
        {
            var document = await _context.RegulatoryDocuments.FindAsync(id);
            if (document == null) return;

            if (request.Status != null) document.Status = request.Status;
            if (request.PriorityScore.HasValue) document.PriorityScore = request.PriorityScore.Value;
            if (request.ProcessedContent != null) document.ProcessedContent = request.ProcessedContent;

            document.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<DashboardStats> GetDashboardStatsAsync(string timeframe)
        {
            var cutoff = timeframe switch
            {
                "week" => DateTime.UtcNow.AddDays(-7),
                "quarter" => DateTime.UtcNow.AddDays(-90),
                _ => DateTime.UtcNow.AddDays(-30)
            };

            var newRegulations = await _context.RegulatoryDocuments
                .CountAsync(d => d.CreatedAt >= cutoff);

            var pendingDeadlines = await _context.ComplianceRequirements
                .CountAsync(cr => cr.Deadline.HasValue &&
                                  cr.Deadline.Value <= DateTime.UtcNow.AddDays(90) &&
                                  cr.Deadline.Value > DateTime.UtcNow);

            var impactAssessments = await _context.ChangeImpactAssessments
                .CountAsync(c => c.AssessmentDate >= cutoff);

            var complianceGaps = await _context.FrameworkRegulationMappings
                .CountAsync(m => m.ComplianceStatus == "non-compliant" || m.ComplianceStatus == "needs_review");

            return new DashboardStats
            {
                NewRegulations = newRegulations,
                PendingDeadlines = pendingDeadlines,
                ImpactAssessments = impactAssessments,
                ComplianceGaps = complianceGaps
            };
        }

        private static RegulatoryAlertDto MapAlertToDto(RegulatoryAlert alert) => new()
        {
            Id = alert.Id,
            AlertType = alert.AlertType,
            DocumentId = alert.DocumentId,
            FrameworkId = alert.FrameworkId,
            Severity = alert.Severity,
            Title = alert.Title,
            Message = alert.Message,
            Status = alert.Status,
            CreatedAt = alert.CreatedAt,
            AcknowledgedAt = alert.AcknowledgedAt,
            AcknowledgedBy = alert.AcknowledgedBy,
            ResolvedAt = alert.ResolvedAt,
            ResolvedBy = alert.ResolvedBy,
            ResolutionNotes = alert.ResolutionNotes,
            Document = alert.Document != null ? MapToDto(alert.Document) : null
        };

        private async Task<Dictionary<Guid, (string? Provider, DateTime AnalysisDate)>> GetLatestAnalysisByDocumentIdsAsync(IEnumerable<Guid> documentIds)
        {
            var ids = documentIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<Guid, (string? Provider, DateTime AnalysisDate)>();
            }

            var analyses = await _context.DocumentAnalyses
                .Where(a => ids.Contains(a.DocumentId))
                .OrderByDescending(a => a.AnalysisDate)
                .Select(a => new { a.DocumentId, a.AnalyzerVersion, a.AnalysisDate })
                .ToListAsync();

            var latestByDocumentId = new Dictionary<Guid, (string? Provider, DateTime AnalysisDate)>();
            foreach (var analysis in analyses)
            {
                if (!latestByDocumentId.ContainsKey(analysis.DocumentId))
                {
                    latestByDocumentId[analysis.DocumentId] = (analysis.AnalyzerVersion, analysis.AnalysisDate);
                }
            }

            return latestByDocumentId;
        }

        private static RegulatoryDocumentDto MapToDto(
            RegulatoryDocument d,
            string? analysisProvider = null,
            bool? hasAnalysis = null) => new()
        {
            Id = d.Id,
            DocumentId = d.DocumentId,
            Title = d.Title,
            DocumentType = d.DocumentType,
            PublicationDate = d.PublicationDate,
            EffectiveDate = d.EffectiveDate,
            CommentDeadline = d.CommentDeadline,
            ComplianceDate = d.ComplianceDate,
            SourceUrl = d.SourceUrl,
            PdfUrl = d.PdfUrl,
            DocketNumber = d.DocketNumber,
            FederalRegisterNumber = d.FederalRegisterNumber,
            CfrCitation = d.CfrCitation,
            Status = d.Status,
            PriorityScore = d.PriorityScore,
            CreatedAt = d.CreatedAt,
            Agency = d.Agency != null ? new AgencyDto
            {
                Id = d.Agency.Id,
                Name = d.Agency.Name,
                Abbreviation = d.Agency.Abbreviation,
                AgencyType = d.Agency.AgencyType,
                Jurisdiction = d.Agency.Jurisdiction
            } : null,
            AnalysisStatus = (hasAnalysis ?? (d.Analyses?.Any() == true)) ? "completed" : "pending",
            AnalysisProvider = analysisProvider
        };
    }
}

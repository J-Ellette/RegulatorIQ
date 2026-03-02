using RegulatorIQ.Data;
using RegulatorIQ.Models;
using RegulatorIQ.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RegulatorIQ.Hubs;

namespace RegulatorIQ.Services
{
    public interface IComplianceFrameworkService
    {
        Task<List<ComplianceFrameworkDto>> GetFrameworksByCompanyAsync(Guid companyId);
        Task<ComplianceFrameworkDto> CreateFrameworkAsync(CreateFrameworkRequest request);
        Task<ComplianceFrameworkDetailDto?> GetFrameworkByIdAsync(Guid id);
        Task<ComplianceFrameworkDto?> UpdateFrameworkAsync(Guid id, UpdateFrameworkRequest request);
        Task<ComplianceFrameworkDto?> UpdateLifecycleAsync(Guid id, FrameworkLifecycleUpdateRequest request);
        Task<FrameworkSyncResult> SyncWithLatestRegulationsAsync(Guid id);
    }

    public class ComplianceFrameworkService : IComplianceFrameworkService
    {
        private readonly RegulatorIQContext _context;
        private readonly ILogger<ComplianceFrameworkService> _logger;
        private readonly IHubContext<NotificationsHub> _hubContext;

        public ComplianceFrameworkService(
            RegulatorIQContext context,
            ILogger<ComplianceFrameworkService> logger,
            IHubContext<NotificationsHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task<List<ComplianceFrameworkDto>> GetFrameworksByCompanyAsync(Guid companyId)
        {
            var frameworks = await _context.ComplianceFrameworks
                .Where(f => f.CompanyId == companyId)
                .OrderByDescending(f => f.LastUpdated)
                .ToListAsync();

            return frameworks.Select(MapToDto).ToList();
        }

        public async Task<ComplianceFrameworkDto> CreateFrameworkAsync(CreateFrameworkRequest request)
        {
            var framework = new ComplianceFramework
            {
                Id = Guid.NewGuid(),
                CompanyId = request.CompanyId,
                FrameworkName = request.FrameworkName,
                FrameworkVersion = request.FrameworkVersion,
                Description = request.Description,
                IndustrySegments = request.IndustrySegments,
                GeographicScope = request.GeographicScope,
                Status = "active",
                Owner = null,
                NextReviewDate = DateTime.UtcNow.AddDays(30),
                FrameworkData = request.FrameworkData != null
                    ? JsonSerializer.Serialize(request.FrameworkData)
                    : null,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            _context.ComplianceFrameworks.Add(framework);
            await _context.SaveChangesAsync();

            return MapToDto(framework);
        }

        public async Task<ComplianceFrameworkDetailDto?> GetFrameworkByIdAsync(Guid id)
        {
            var framework = await _context.ComplianceFrameworks
                .Include(f => f.RegulationMappings)
                    .ThenInclude(m => m.Document)
                        .ThenInclude(d => d!.Agency)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (framework == null) return null;

            return new ComplianceFrameworkDetailDto
            {
                Id = framework.Id,
                CompanyId = framework.CompanyId,
                FrameworkName = framework.FrameworkName,
                FrameworkVersion = framework.FrameworkVersion,
                Description = framework.Description,
                IndustrySegments = framework.IndustrySegments,
                GeographicScope = framework.GeographicScope,
                Status = framework.Status,
                Owner = framework.Owner,
                NextReviewDate = framework.NextReviewDate,
                FrameworkData = framework.FrameworkData != null
                    ? JsonSerializer.Deserialize<object>(framework.FrameworkData)
                    : null,
                LastUpdated = framework.LastUpdated,
                CreatedAt = framework.CreatedAt,
                RegulationMappings = framework.RegulationMappings.Select(m => new FrameworkRegulationMappingDto
                {
                    Id = m.Id,
                    FrameworkId = m.FrameworkId,
                    DocumentId = m.DocumentId,
                    RequirementId = m.RequirementId,
                    MappingType = m.MappingType,
                    ComplianceStatus = m.ComplianceStatus,
                    ImplementationStatus = m.ImplementationStatus,
                    Notes = m.Notes,
                    AssignedTo = m.AssignedTo,
                    DueDate = m.DueDate,
                    Document = m.Document != null ? MapDocumentToDto(m.Document) : null
                }).ToList()
            };
        }

        public async Task<ComplianceFrameworkDto?> UpdateFrameworkAsync(Guid id, UpdateFrameworkRequest request)
        {
            var framework = await _context.ComplianceFrameworks.FindAsync(id);
            if (framework == null) return null;

            if (request.FrameworkName != null) framework.FrameworkName = request.FrameworkName;
            if (request.FrameworkVersion != null) framework.FrameworkVersion = request.FrameworkVersion;
            if (request.Description != null) framework.Description = request.Description;
            if (request.IndustrySegments != null) framework.IndustrySegments = request.IndustrySegments;
            if (request.GeographicScope != null) framework.GeographicScope = request.GeographicScope;
            if (request.FrameworkData != null)
                framework.FrameworkData = JsonSerializer.Serialize(request.FrameworkData);
            if (request.Status != null) framework.Status = request.Status;
            if (request.Owner != null) framework.Owner = request.Owner;
            if (request.NextReviewDate.HasValue) framework.NextReviewDate = request.NextReviewDate;

            framework.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToDto(framework);
        }

        public async Task<ComplianceFrameworkDto?> UpdateLifecycleAsync(Guid id, FrameworkLifecycleUpdateRequest request)
        {
            var framework = await _context.ComplianceFrameworks.FindAsync(id);
            if (framework == null) return null;

            framework.Status = request.Status;
            framework.Owner = request.Owner;
            framework.NextReviewDate = request.NextReviewDate;
            framework.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("frameworkLifecycleUpdated", new
            {
                frameworkId = framework.Id,
                frameworkName = framework.FrameworkName,
                status = framework.Status,
                owner = framework.Owner,
                nextReviewDate = framework.NextReviewDate,
                updatedAt = framework.LastUpdated
            });

            return MapToDto(framework);
        }

        public async Task<FrameworkSyncResult> SyncWithLatestRegulationsAsync(Guid id)
        {
            var framework = await _context.ComplianceFrameworks
                .Include(f => f.RegulationMappings)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (framework == null)
                throw new KeyNotFoundException($"Framework {id} not found");

            var result = new FrameworkSyncResult
            {
                FrameworkId = id,
                SyncedAt = DateTime.UtcNow
            };

            // Find new regulations since last update that match framework scope
            var newRegulations = await _context.RegulatoryDocuments
                .Include(d => d.Analyses)
                .Where(d => d.CreatedAt > framework.LastUpdated)
                .ToListAsync();

            var existingDocumentIds = framework.RegulationMappings
                .Where(m => m.DocumentId.HasValue)
                .Select(m => m.DocumentId!.Value)
                .ToHashSet();

            foreach (var regulation in newRegulations)
            {
                if (!existingDocumentIds.Contains(regulation.Id))
                {
                    var mapping = new FrameworkRegulationMapping
                    {
                        Id = Guid.NewGuid(),
                        FrameworkId = id,
                        DocumentId = regulation.Id,
                        MappingType = "potential",
                        ComplianceStatus = "needs_review",
                        ImplementationStatus = "planned",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.FrameworkRegulationMappings.Add(mapping);
                    result.NewRegulationsFound++;
                }
            }

            framework.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("frameworkSynced", new
            {
                frameworkId = framework.Id,
                frameworkName = framework.FrameworkName,
                newRegulationsFound = result.NewRegulationsFound,
                syncedAt = result.SyncedAt
            });

            return result;
        }

        private static ComplianceFrameworkDto MapToDto(ComplianceFramework f) => new()
        {
            Id = f.Id,
            CompanyId = f.CompanyId,
            FrameworkName = f.FrameworkName,
            FrameworkVersion = f.FrameworkVersion,
            Description = f.Description,
            IndustrySegments = f.IndustrySegments,
            GeographicScope = f.GeographicScope,
            Status = f.Status,
            Owner = f.Owner,
            NextReviewDate = f.NextReviewDate,
            LastUpdated = f.LastUpdated,
            CreatedAt = f.CreatedAt
        };

        private static RegulatoryDocumentDto MapDocumentToDto(RegulatoryDocument d) => new()
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
                Jurisdiction = d.Agency.Jurisdiction,
                WebsiteUrl = d.Agency.WebsiteUrl
            } : null,
            AnalysisStatus = d.Analyses.Any() ? "completed" : "pending"
        };
    }
}

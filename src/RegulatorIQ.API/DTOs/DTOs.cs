namespace RegulatorIQ.DTOs
{
    public class RegulatoryDocumentDto
    {
        public Guid Id { get; set; }
        public string? DocumentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? DocumentType { get; set; }
        public DateTime? PublicationDate { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public DateTime? CommentDeadline { get; set; }
        public DateTime? ComplianceDate { get; set; }
        public string? SourceUrl { get; set; }
        public string? PdfUrl { get; set; }
        public string? DocketNumber { get; set; }
        public string? FederalRegisterNumber { get; set; }
        public string? CfrCitation { get; set; }
        public string Status { get; set; } = "active";
        public int PriorityScore { get; set; }
        public DateTime CreatedAt { get; set; }
        public AgencyDto? Agency { get; set; }
        public string? AnalysisStatus { get; set; }
    }

    public class RegulatoryDocumentDetailDto : RegulatoryDocumentDto
    {
        public string? ProcessedContent { get; set; }
        public List<DocumentAnalysisDto>? Analyses { get; set; }
        public List<ComplianceRequirementDto>? ComplianceRequirements { get; set; }
    }

    public class AgencyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Abbreviation { get; set; }
        public string? AgencyType { get; set; }
        public string? Jurisdiction { get; set; }
    }

    public class DocumentAnalysisDto
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public int AnalysisVersion { get; set; }
        public object? Classification { get; set; }
        public object? EntitiesExtracted { get; set; }
        public List<ComplianceRequirementDto>? ComplianceRequirements { get; set; }
        public object? ImpactAssessment { get; set; }
        public object? TimelineAnalysis { get; set; }
        public string[]? AffectedParties { get; set; }
        public string? Summary { get; set; }
        public object? ActionableItems { get; set; }
        public string[]? RelatedRegulations { get; set; }
        public decimal ConfidenceScore { get; set; }
        public DateTime AnalysisDate { get; set; }
        public string? AnalyzerVersion { get; set; }
    }

    public class ComplianceRequirementDto
    {
        public Guid Id { get; set; }
        public string RequirementId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? RequirementType { get; set; }
        public string[]? Applicability { get; set; }
        public string? Deadline { get; set; }
        public string? Severity { get; set; }
        public string? ImplementationGuidance { get; set; }
        public decimal? EstimatedCostImpact { get; set; }
        public int? ComplexityLevel { get; set; }
        public string? Citation { get; set; }
    }

    public class ComplianceFrameworkDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string FrameworkName { get; set; } = string.Empty;
        public string? FrameworkVersion { get; set; }
        public string? Description { get; set; }
        public string[]? IndustrySegments { get; set; }
        public string[]? GeographicScope { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Status { get; set; }
    }

    public class ComplianceFrameworkDetailDto : ComplianceFrameworkDto
    {
        public object? FrameworkData { get; set; }
        public List<FrameworkRegulationMappingDto>? RegulationMappings { get; set; }
    }

    public class FrameworkRegulationMappingDto
    {
        public Guid Id { get; set; }
        public Guid? FrameworkId { get; set; }
        public Guid? DocumentId { get; set; }
        public Guid? RequirementId { get; set; }
        public string? MappingType { get; set; }
        public string? ComplianceStatus { get; set; }
        public string? ImplementationStatus { get; set; }
        public string? Notes { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime? DueDate { get; set; }
        public RegulatoryDocumentDto? Document { get; set; }
    }

    public class ChangeImpactAssessmentDto
    {
        public Guid Id { get; set; }
        public Guid? DocumentId { get; set; }
        public Guid? FrameworkId { get; set; }
        public decimal? ImpactScore { get; set; }
        public object? AffectedProcesses { get; set; }
        public object? RequiredUpdates { get; set; }
        public object? TimelineConflicts { get; set; }
        public decimal? EstimatedCost { get; set; }
        public int? ImplementationComplexity { get; set; }
        public string? RiskLevel { get; set; }
        public DateTime AssessmentDate { get; set; }
        public string? AssessedBy { get; set; }
    }

    public class RegulatoryAlertDto
    {
        public Guid Id { get; set; }
        public string? AlertType { get; set; }
        public Guid? DocumentId { get; set; }
        public Guid? FrameworkId { get; set; }
        public string? Severity { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public object? AlertData { get; set; }
        public string Status { get; set; } = "active";
        public DateTime CreatedAt { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string? AcknowledgedBy { get; set; }
        public RegulatoryDocumentDto? Document { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class DocumentSearchRequest
    {
        public string? SearchTerm { get; set; }
        public List<Guid>? AgencyIds { get; set; }
        public List<string>? DocumentTypes { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? MinPriority { get; set; }
        public string? SortBy { get; set; }
        public bool SortDesc { get; set; } = true;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class DocumentFilter
    {
        public List<Guid>? AgencyIds { get; set; }
        public List<string>? DocumentTypes { get; set; }
        public DateTime? EffectiveDateFrom { get; set; }
        public DateTime? EffectiveDateTo { get; set; }
        public int? MinPriority { get; set; }
    }

    public class AlertFilter
    {
        public Guid? CompanyId { get; set; }
        public List<string>? Severity { get; set; }
        public List<string>? Status { get; set; }
        public List<string>? AlertTypes { get; set; }
    }

    public class CreateFrameworkRequest
    {
        public Guid CompanyId { get; set; }
        public string FrameworkName { get; set; } = string.Empty;
        public string? FrameworkVersion { get; set; }
        public string? Description { get; set; }
        public string[]? IndustrySegments { get; set; }
        public string[]? GeographicScope { get; set; }
        public object? FrameworkData { get; set; }
    }

    public class UpdateFrameworkRequest
    {
        public string? FrameworkName { get; set; }
        public string? FrameworkVersion { get; set; }
        public string? Description { get; set; }
        public string[]? IndustrySegments { get; set; }
        public string[]? GeographicScope { get; set; }
        public object? FrameworkData { get; set; }
    }

    public class ImpactAssessmentRequest
    {
        public Guid DocumentId { get; set; }
    }

    public class CreateDocumentRequest
    {
        public Guid? AgencyId { get; set; }
        public string? DocumentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? DocumentType { get; set; }
        public DateTime? PublicationDate { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string? SourceUrl { get; set; }
        public string? PdfUrl { get; set; }
        public string? RawContent { get; set; }
        public string? DocketNumber { get; set; }
        public int PriorityScore { get; set; }
    }

    public class UpdateDocumentRequest
    {
        public string? Status { get; set; }
        public int? PriorityScore { get; set; }
        public string? ProcessedContent { get; set; }
    }

    public class BulkAnalysisRequest
    {
        public List<Guid> DocumentIds { get; set; } = new();
    }

    public class BulkAnalysisResult
    {
        public int TotalRequested { get; set; }
        public int Queued { get; set; }
        public int Failed { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class FrameworkSyncResult
    {
        public Guid FrameworkId { get; set; }
        public int NewRegulationsFound { get; set; }
        public int ImpactAssessmentsCreated { get; set; }
        public int AlertsGenerated { get; set; }
        public DateTime SyncedAt { get; set; }
    }

    public class DashboardStats
    {
        public int NewRegulations { get; set; }
        public int PendingDeadlines { get; set; }
        public int ImpactAssessments { get; set; }
        public int ComplianceGaps { get; set; }
    }
}

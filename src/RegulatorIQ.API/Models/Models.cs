using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegulatorIQ.Models
{
    public class RegulatoryAgency
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Abbreviation { get; set; }

        [MaxLength(50)]
        public string? AgencyType { get; set; }

        [MaxLength(100)]
        public string? Jurisdiction { get; set; }

        [MaxLength(500)]
        public string? WebsiteUrl { get; set; }

        [MaxLength(500)]
        public string? ApiEndpoint { get; set; }

        [Column(TypeName = "jsonb")]
        public string? ContactInfo { get; set; }

        public bool MonitoringEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class RegulatoryDocument
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? AgencyId { get; set; }
        public RegulatoryAgency? Agency { get; set; }

        [MaxLength(100)]
        public string? DocumentId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? DocumentType { get; set; }

        public DateTime? PublicationDate { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public DateTime? CommentDeadline { get; set; }
        public DateTime? ComplianceDate { get; set; }

        [MaxLength(1000)]
        public string? SourceUrl { get; set; }

        [MaxLength(1000)]
        public string? PdfUrl { get; set; }

        public string? RawContent { get; set; }
        public string? ProcessedContent { get; set; }

        [MaxLength(100)]
        public string? DocketNumber { get; set; }

        [MaxLength(50)]
        public string? FederalRegisterNumber { get; set; }

        [MaxLength(100)]
        public string? CfrCitation { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "active";

        public int PriorityScore { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<DocumentAnalysis> Analyses { get; set; } = new List<DocumentAnalysis>();
        public ICollection<ComplianceRequirement> ComplianceRequirements { get; set; } = new List<ComplianceRequirement>();
        public ICollection<ChangeImpactAssessment> ImpactAssessments { get; set; } = new List<ChangeImpactAssessment>();
    }

    public class DocumentAnalysis
    {
        [Key]
        public Guid Id { get; set; }

        public Guid DocumentId { get; set; }
        public RegulatoryDocument? Document { get; set; }

        public int AnalysisVersion { get; set; } = 1;

        [Column(TypeName = "jsonb")]
        public string? Classification { get; set; }

        [Column(TypeName = "jsonb")]
        public string? EntitiesExtracted { get; set; }

        [Column(TypeName = "jsonb")]
        public string? ComplianceRequirements { get; set; }

        [Column(TypeName = "jsonb")]
        public string? ImpactAssessment { get; set; }

        [Column(TypeName = "jsonb")]
        public string? TimelineAnalysis { get; set; }

        public string[]? AffectedParties { get; set; }

        public string? Summary { get; set; }

        [Column(TypeName = "jsonb")]
        public string? ActionableItems { get; set; }

        public string[]? RelatedRegulations { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal ConfidenceScore { get; set; }

        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string? AnalyzerVersion { get; set; }
    }

    public class ComplianceRequirement
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? DocumentId { get; set; }
        public RegulatoryDocument? Document { get; set; }

        [Required]
        public string RequirementText { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? RequirementType { get; set; }

        public string[]? Applicability { get; set; }

        public DateTime? Deadline { get; set; }

        [MaxLength(20)]
        public string? Severity { get; set; }

        public string? ImplementationGuidance { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal? EstimatedCostImpact { get; set; }

        public int? ComplexityLevel { get; set; }

        [MaxLength(200)]
        public string? Citation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ComplianceFramework
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CompanyId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FrameworkName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? FrameworkVersion { get; set; }

        public string? Description { get; set; }

        public string[]? IndustrySegments { get; set; }
        public string[]? GeographicScope { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "active";

        [MaxLength(255)]
        public string? Owner { get; set; }

        public DateTime? NextReviewDate { get; set; }

        [Column(TypeName = "jsonb")]
        public string? FrameworkData { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<FrameworkRegulationMapping> RegulationMappings { get; set; } = new List<FrameworkRegulationMapping>();
        public ICollection<ChangeImpactAssessment> ImpactAssessments { get; set; } = new List<ChangeImpactAssessment>();
    }

    public class FrameworkRegulationMapping
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? FrameworkId { get; set; }
        public ComplianceFramework? Framework { get; set; }

        public Guid? DocumentId { get; set; }
        public RegulatoryDocument? Document { get; set; }

        public Guid? RequirementId { get; set; }
        public ComplianceRequirement? Requirement { get; set; }

        [MaxLength(50)]
        public string? MappingType { get; set; }

        [MaxLength(50)]
        public string? ComplianceStatus { get; set; }

        [MaxLength(50)]
        public string? ImplementationStatus { get; set; }

        public string? Notes { get; set; }

        [MaxLength(255)]
        public string? AssignedTo { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ChangeImpactAssessment
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? DocumentId { get; set; }
        public RegulatoryDocument? Document { get; set; }

        public Guid? FrameworkId { get; set; }
        public ComplianceFramework? Framework { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? ImpactScore { get; set; }

        [Column(TypeName = "jsonb")]
        public string? AffectedProcesses { get; set; }

        [Column(TypeName = "jsonb")]
        public string? RequiredUpdates { get; set; }

        [Column(TypeName = "jsonb")]
        public string? TimelineConflicts { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal? EstimatedCost { get; set; }

        public int? ImplementationComplexity { get; set; }

        [MaxLength(20)]
        public string? RiskLevel { get; set; }

        public DateTime AssessmentDate { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? AssessedBy { get; set; }
    }

    public class RegulatoryAlert
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(100)]
        public string? AlertType { get; set; }

        public Guid? DocumentId { get; set; }
        public RegulatoryDocument? Document { get; set; }

        public Guid? FrameworkId { get; set; }
        public ComplianceFramework? Framework { get; set; }

        [MaxLength(20)]
        public string? Severity { get; set; }

        [MaxLength(500)]
        public string? Title { get; set; }

        public string? Message { get; set; }

        [Column(TypeName = "jsonb")]
        public string? AlertData { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcknowledgedAt { get; set; }

        [MaxLength(255)]
        public string? AcknowledgedBy { get; set; }

        public DateTime? ResolvedAt { get; set; }

        [MaxLength(255)]
        public string? ResolvedBy { get; set; }

        public string? ResolutionNotes { get; set; }
    }

    public class RegulatoryAuditLog
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EntityType { get; set; }

        public Guid? EntityId { get; set; }

        [Column(TypeName = "jsonb")]
        public string? OldData { get; set; }

        [Column(TypeName = "jsonb")]
        public string? NewData { get; set; }

        [MaxLength(255)]
        public string? ChangedBy { get; set; }

        public string? ChangeReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class MonitoringRun
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string RunType { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? TriggeredBy { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "running";

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public int DocumentsFetched { get; set; }
        public int DocumentsAdded { get; set; }
        public int DocumentsSkipped { get; set; }
        public int FailureCount { get; set; }

        [Column(TypeName = "jsonb")]
        public string? SourceMetrics { get; set; }

        public string? ErrorSummary { get; set; }
    }
}

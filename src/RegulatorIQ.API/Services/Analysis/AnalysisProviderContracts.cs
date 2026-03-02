using RegulatorIQ.Models;

namespace RegulatorIQ.Services.Analysis
{
    public sealed class AnalysisProviderResult
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
        public string ProviderName { get; set; } = string.Empty;
    }

    public interface IAIAnalysisProvider
    {
        Task<AnalysisProviderResult?> AnalyzeAsync(RegulatoryDocument document, CancellationToken cancellationToken = default);
    }

    public interface IRulesAnalysisProvider
    {
        Task<AnalysisProviderResult?> AnalyzeAsync(RegulatoryDocument document, CancellationToken cancellationToken = default);
    }
}

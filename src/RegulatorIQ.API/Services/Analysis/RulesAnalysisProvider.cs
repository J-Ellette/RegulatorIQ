using System.Text.RegularExpressions;
using RegulatorIQ.Models;

namespace RegulatorIQ.Services.Analysis
{
    public class RulesAnalysisProvider : IRulesAnalysisProvider
    {
        private static readonly string[] RequirementKeywords =
        [
            "shall",
            "must",
            "required to",
            "operator shall",
            "company must"
        ];

        private static readonly string[] FacilityKeywords =
        [
            "pipeline",
            "compressor",
            "storage",
            "lng",
            "distribution",
            "transmission"
        ];

        public Task<AnalysisProviderResult?> AnalyzeAsync(RegulatoryDocument document, CancellationToken cancellationToken = default)
        {
            var content = document.ProcessedContent ?? document.RawContent ?? document.Title;
            var normalizedContent = content.ToLowerInvariant();

            var requirements = ExtractRequirements(content);
            var facilities = FacilityKeywords.Where(keyword => normalizedContent.Contains(keyword)).ToList();

            var urgency = document.PriorityScore switch
            {
                >= 15 => "critical",
                >= 10 => "high",
                >= 5 => "medium",
                _ => "low"
            };

            var impactScore = Math.Min(10, Math.Round(document.PriorityScore / 2.0, 1));

            var timeline = new
            {
                effective_date = document.EffectiveDate,
                compliance_date = document.ComplianceDate,
                publication_date = document.PublicationDate
            };

            var related = new List<string>();
            if (!string.IsNullOrWhiteSpace(document.CfrCitation))
            {
                related.Add(document.CfrCitation);
            }

            var summary = $"Rules-based analysis: {document.Title}. {requirements.Count} potential compliance requirement(s) detected.";

            var result = new AnalysisProviderResult
            {
                Classification = new
                {
                    primary_category = InferCategory(normalizedContent, document.DocumentType),
                    regulatory_type = document.DocumentType ?? "unknown",
                    urgency_level = urgency,
                    scope = "targeted"
                },
                Entities = new
                {
                    facilities,
                    organizations = ExtractOrganizations(content),
                    citations = ExtractCitations(content)
                },
                ComplianceRequirements = requirements.Cast<object>().ToList(),
                ImpactAssessment = new
                {
                    impact_score = impactScore,
                    factors = new
                    {
                        priority = document.PriorityScore,
                        requirement_count = requirements.Count,
                        has_effective_date = document.EffectiveDate.HasValue
                    },
                    priority_level = urgency
                },
                TimelineAnalysis = timeline,
                AffectedParties = InferAffectedParties(normalizedContent),
                Summary = summary,
                ActionableItems = new[]
                {
                    new { item = "Review extracted requirements", priority = urgency },
                    new { item = "Validate impact with compliance owner", priority = "medium" }
                },
                RelatedRegulations = related,
                ConfidenceScore = 0.35,
                ProviderName = "rules"
            };

            return Task.FromResult<AnalysisProviderResult?>(result);
        }

        private static string InferCategory(string normalizedContent, string? docType)
        {
            if ((docType ?? string.Empty).Contains("safety", StringComparison.OrdinalIgnoreCase) ||
                normalizedContent.Contains("safety"))
            {
                return "safety";
            }

            if (normalizedContent.Contains("environment") || normalizedContent.Contains("emission"))
            {
                return "environmental";
            }

            if (normalizedContent.Contains("report"))
            {
                return "reporting";
            }

            return "general_compliance";
        }

        private static List<object> ExtractRequirements(string content)
        {
            var sentences = Regex.Split(content, @"(?<=[.!?])\s+")
                .Where(sentence => !string.IsNullOrWhiteSpace(sentence))
                .ToList();

            var matches = sentences
                .Where(sentence => RequirementKeywords.Any(keyword => sentence.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                .Take(10)
                .Select((sentence, index) => new
                {
                    requirement_id = $"RULE_{index + 1:000}",
                    description = sentence.Trim(),
                    severity = "medium"
                })
                .Cast<object>()
                .ToList();

            return matches;
        }

        private static List<string> ExtractOrganizations(string content)
        {
            var matches = Regex.Matches(content, @"\b[A-Z]{2,}\b")
                .Select(match => match.Value)
                .Distinct()
                .Take(10)
                .ToList();

            return matches;
        }

        private static List<string> ExtractCitations(string content)
        {
            var regex = new Regex(@"\b\d+\s+CFR\s+\d+(?:\.\d+)?\b", RegexOptions.IgnoreCase);
            return regex.Matches(content)
                .Select(match => match.Value)
                .Distinct()
                .Take(20)
                .ToList();
        }

        private static List<string> InferAffectedParties(string normalizedContent)
        {
            var affected = new List<string>();

            if (normalizedContent.Contains("operator")) affected.Add("pipeline operator");
            if (normalizedContent.Contains("utility")) affected.Add("gas utility");
            if (normalizedContent.Contains("facility") || normalizedContent.Contains("lng")) affected.Add("facility owner");

            if (!affected.Any())
            {
                affected.Add("all entities");
            }

            return affected;
        }
    }
}

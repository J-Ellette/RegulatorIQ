using RegulatorIQ.Models;
using System.Net.Http.Json;

namespace RegulatorIQ.Services.Analysis
{
    public class AIAnalysisProvider : IAIAnalysisProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIAnalysisProvider> _logger;

        public AIAnalysisProvider(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AIAnalysisProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AnalysisProviderResult?> AnalyzeAsync(RegulatoryDocument document, CancellationToken cancellationToken = default)
        {
            var mlServiceUrl = _configuration["MLServices:BaseUrl"] ?? "http://ml-services:8000";
            var client = _httpClientFactory.CreateClient("MLServices");

            var requestPayload = new
            {
                document_id = document.Id.ToString(),
                content = document.ProcessedContent ?? document.RawContent ?? document.Title,
                document_type = document.DocumentType,
                title = document.Title
            };

            var response = await client.PostAsJsonAsync(
                $"{mlServiceUrl}/analyze",
                requestPayload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI analysis provider received non-success status {StatusCode} for document {DocumentId}",
                    response.StatusCode, document.Id);
                return null;
            }

            var mlResult = await response.Content.ReadFromJsonAsync<AnalysisProviderResult>(cancellationToken: cancellationToken);
            if (mlResult == null)
            {
                return null;
            }

            mlResult.ProviderName = "ai";
            return mlResult;
        }
    }
}

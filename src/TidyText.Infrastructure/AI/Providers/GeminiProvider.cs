using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using TidyText.Domain.AI;
namespace TidyText.Infrastructure.AI.Providers
{
    public class GeminiProvider : IAIProvider
    {
        public string Name => "Gemini";
        public System.Collections.Generic.IReadOnlyList<string> AvailableModels { get; } = new[]
        {
            "gemini-3.5-flash", "gemini-3.5-pro", "gemini-3.1-pro", "gemini-3.1-flash-lite"
        };
        
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public GeminiProvider(string apiKey, HttpClient httpClient)
        {
            _apiKey = apiKey;
            _httpClient = httpClient;
        }

        public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);

        public async Task<AIResponse> CompleteAsync(string prompt, AIOptions options, CancellationToken ct = default)
        {
            if (!IsAvailable) return AIResponse.Error("Gemini API key is not configured.");

            string model = string.IsNullOrEmpty(options.Model) ? "gemini-1.5-flash" : options.Model;
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                },
                systemInstruction = string.IsNullOrEmpty(options.SystemPrompt) ? null : new
                {
                    parts = new[] { new { text = options.SystemPrompt } }
                },
                generationConfig = new
                {
                    temperature = options.Temperature,
                    maxOutputTokens = options.MaxTokens
                }
            };

            using var response = await _httpClient.PostAsJsonAsync(url, requestBody, ct);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync(ct);
                return AIResponse.Error($"Gemini API Error ({response.StatusCode}): {errorContent}");
            }

            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

            try
            {
                string text = jsonResponse
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                return AIResponse.Success(text);
            }
            catch (Exception ex)
            {
                return AIResponse.Error($"Failed to parse Gemini response: {ex.Message}");
            }
        }
    }
}

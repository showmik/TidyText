using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using TidyText.Domain.AI;
namespace TidyText.Infrastructure.AI.Providers
{
    public class OpenAIProvider : IAIProvider
    {
        public string Name => "OpenAI";
        public System.Collections.Generic.IReadOnlyList<string> AvailableModels { get; } = new[]
        {
            "gpt-5.5-instant", "gpt-5.5-pro", "gpt-5.4-mini", "gpt-5.4-pro", "gpt-5.4-nano"
        };
        
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public OpenAIProvider(string apiKey, HttpClient httpClient)
        {
            _apiKey = apiKey;
            _httpClient = httpClient;
        }

        public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);

        public async Task<AIResponse> CompleteAsync(string prompt, AIOptions options, CancellationToken ct = default)
        {
            if (!IsAvailable) return AIResponse.Error("OpenAI API key is not configured.");

            string model = string.IsNullOrEmpty(options.Model) ? "gpt-4o-mini" : options.Model;
            string url = "https://api.openai.com/v1/chat/completions";

            var requestBody = new
            {
                model = model,
                messages = new object[]
                {
                    new { role = "system", content = string.IsNullOrEmpty(options.SystemPrompt) ? "You are a helpful assistant." : options.SystemPrompt },
                    new { role = "user", content = prompt }
                },
                temperature = options.Temperature,
                max_tokens = options.MaxTokens
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(requestBody)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync(ct);
                return AIResponse.Error($"OpenAI API Error ({response.StatusCode}): {errorContent}");
            }

            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

            try
            {
                string text = jsonResponse
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                return AIResponse.Success(text);
            }
            catch (Exception ex)
            {
                return AIResponse.Error($"Failed to parse OpenAI response: {ex.Message}");
            }
        }
    }
}

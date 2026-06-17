using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TidyText.Core.AI.Providers
{
    public class AnthropicProvider : IAIProvider
    {
        public string Name => "Anthropic";
        
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public AnthropicProvider(string apiKey, HttpClient httpClient)
        {
            _apiKey = apiKey;
            _httpClient = httpClient;
        }

        public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);

        public async Task<AIResponse> CompleteAsync(string prompt, AIOptions options, CancellationToken ct = default)
        {
            if (!IsAvailable) return AIResponse.Error("Anthropic API key is not configured.");

            string model = string.IsNullOrEmpty(options.Model) ? "claude-3-haiku-20240307" : options.Model;
            string url = "https://api.anthropic.com/v1/messages";

            var requestBody = new
            {
                model = model,
                system = string.IsNullOrEmpty(options.SystemPrompt) ? "You are a helpful assistant." : options.SystemPrompt,
                messages = new object[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = options.Temperature,
                max_tokens = options.MaxTokens
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(requestBody)
            };
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync(ct);
                return AIResponse.Error($"Anthropic API Error ({response.StatusCode}): {errorContent}");
            }

            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

            try
            {
                string text = jsonResponse
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                return AIResponse.Success(text);
            }
            catch (Exception ex)
            {
                return AIResponse.Error($"Failed to parse Anthropic response: {ex.Message}");
            }
        }
    }
}

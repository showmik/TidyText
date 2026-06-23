using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using TidyText.Domain.AI;
namespace TidyText.Infrastructure.AI.Providers
{
    public class OllamaProvider : IAIProvider
    {
        public string Name => "Ollama";
        public System.Collections.Generic.IReadOnlyList<string> AvailableModels { get; } = new[]
        {
            "llama3", "phi3", "mistral", "gemma2"
        };
        
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public OllamaProvider(HttpClient httpClient, string baseUrl = "http://localhost:11434")
        {
            _httpClient = httpClient;
            _baseUrl = baseUrl.TrimEnd('/');
        }

        // Ollama is always considered available since it runs locally.
        public bool IsAvailable => true;

        public async Task<AIResponse> CompleteAsync(string prompt, AIOptions options, CancellationToken ct = default)
        {
            string model = string.IsNullOrEmpty(options.Model) ? "llama3" : options.Model;
            string url = $"{_baseUrl}/api/generate";

            var requestBody = new
            {
                model = model,
                prompt = prompt,
                system = string.IsNullOrEmpty(options.SystemPrompt) ? "You are a helpful assistant." : options.SystemPrompt,
                stream = false,
                options = new
                {
                    temperature = options.Temperature
                }
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, requestBody, ct);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync(ct);
                    return AIResponse.Error($"Ollama API Error ({response.StatusCode}): {errorContent}");
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

                string text = jsonResponse
                    .GetProperty("response")
                    .GetString() ?? string.Empty;

                return AIResponse.Success(text);
            }
            catch (HttpRequestException)
            {
                return AIResponse.Error("Could not connect to Ollama. Please ensure the Ollama app is running locally (http://localhost:11434).");
            }
            catch (Exception ex)
            {
                return AIResponse.Error($"Failed to parse Ollama response: {ex.Message}");
            }
        }
    }
}

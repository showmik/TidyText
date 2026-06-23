using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using TidyText.Domain.Security;
using TidyText.Domain.AI;

namespace TidyText.Infrastructure.AI.Providers
{
    public class LocalLMProvider : IAIProvider
    {
        public string Name => "Local LM";
        public System.Collections.Generic.IReadOnlyList<string> AvailableModels { get; } = new[]
        {
            "local-model", "lmstudio-default"
        };
        
        private readonly HttpClient _httpClient;
        private readonly ISecureKeyVault _keyVault;

        public LocalLMProvider(HttpClient httpClient, ISecureKeyVault keyVault)
        {
            _httpClient = httpClient;
            _keyVault = keyVault;
        }

        // Local LM is always considered available since it runs locally.
        public bool IsAvailable => true;

        public async Task<AIResponse> CompleteAsync(string prompt, AIOptions options, CancellationToken ct = default)
        {
            string baseUrl = _keyVault.GetKey("LocalLM_Url");
            if (string.IsNullOrWhiteSpace(baseUrl)) 
            {
                baseUrl = "http://localhost:1234/v1";
            }
            else
            {
                baseUrl = baseUrl.TrimEnd('/');
                if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                    !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    baseUrl = "http://" + baseUrl;
                }
            }

            string model = string.IsNullOrEmpty(options.Model) ? "local-model" : options.Model;
            string url = $"{baseUrl}/chat/completions";

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
            
            // Local LMs usually don't need a real key, but some require a dummy key
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "lm-studio");

            try
            {
                var response = await _httpClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync(ct);
                    return AIResponse.Error($"Local LM API Error ({response.StatusCode}): {errorContent}");
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

                if (jsonResponse.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) && 
                        message.TryGetProperty("content", out var content))
                    {
                        string text = content.GetString() ?? string.Empty;
                        return AIResponse.Success(text);
                    }
                }

                return AIResponse.Error("Local LM returned an unexpected response format.");
            }
            catch (HttpRequestException)
            {
                return AIResponse.Error($"Could not connect to Local LM at {baseUrl}. Please ensure your local server is running.");
            }
            catch (UriFormatException)
            {
                return AIResponse.Error($"Invalid Local LM Base URL format: {baseUrl}");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return AIResponse.Error($"Local LM Error: {ex.Message}");
            }
        }

        public async System.Collections.Generic.IAsyncEnumerable<string> StreamAsync(string prompt, AIOptions options, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            string baseUrl = _keyVault.GetKey("LocalLM_Url");
            if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = "http://localhost:1234/v1";
            else
            {
                baseUrl = baseUrl.TrimEnd('/');
                if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                    !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    baseUrl = "http://" + baseUrl;
                }
            }

            string model = string.IsNullOrEmpty(options.Model) ? "local-model" : options.Model;
            string url = $"{baseUrl}/chat/completions";

            var requestBody = new
            {
                model = model,
                messages = new object[]
                {
                    new { role = "system", content = string.IsNullOrEmpty(options.SystemPrompt) ? "You are a helpful assistant." : options.SystemPrompt },
                    new { role = "user", content = prompt }
                },
                temperature = options.Temperature,
                max_tokens = options.MaxTokens,
                stream = true
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(requestBody)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "lm-studio");

            // Cannot yield inside catch — use sentinel for connection errors
            HttpResponseMessage? response = null;
            string? connectError = null;
            try
            {
                response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                connectError = $"[Error] Local LM Error: {ex.Message}";
            }

            if (connectError != null)
            {
                yield return connectError;
                yield break;
            }

            if (!response!.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                yield return $"[Error] Local LM API Error ({response.StatusCode}): {errorContent}";
                yield break;
            }

            using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var reader = new System.IO.StreamReader(stream);

            // ReadLineAsync returns null at end-of-stream (avoids CA2024 EndOfStream)
            string? line;
            while ((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) != null)
            {
                if (ct.IsCancellationRequested) break;
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!line.StartsWith("data: ")) continue;

                var data = line.Substring(6).Trim();
                if (data == "[DONE]") break;

                // Parse outside try-catch so we can yield the result
                // (C# forbids yield inside try blocks that have catch clauses)
                string? parsedChunk = null;
                try
                {
                    var json = JsonSerializer.Deserialize<JsonElement>(data);
                    if (json.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var firstChoice = choices[0];
                        if (firstChoice.TryGetProperty("delta", out var delta) && delta.TryGetProperty("content", out var content))
                        {
                            parsedChunk = content.GetString();
                        }
                    }
                }
                catch
                {
                    // Ignore parse errors for partial SSE chunks
                }

                if (!string.IsNullOrEmpty(parsedChunk))
                {
                    yield return parsedChunk;
                }
            }
        }
    }
}

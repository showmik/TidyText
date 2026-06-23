using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TidyText.Domain.AI
{
    public class AIProviderRouter : IAIProviderRouter
    {
        private readonly IEnumerable<IAIProvider> _providers;

        public AIProviderRouter(IEnumerable<IAIProvider> providers)
        {
            _providers = providers;
        }

        public Task<AIResponse> RouteAsync(string providerName, string prompt, AIOptions options, CancellationToken ct = default)
        {
            var provider = GetProvider(providerName);

            if (provider == null)
            {
                return Task.FromResult(AIResponse.Error($"Provider '{providerName}' not found."));
            }

            if (!provider.IsAvailable)
            {
                return Task.FromResult(AIResponse.Error($"Provider '{providerName}' is not available. Please check API keys or connection."));
            }

            try
            {
                return provider.CompleteAsync(prompt, options, ct);
            }
            catch (Exception ex)
            {
                return Task.FromResult(AIResponse.Error($"An error occurred with provider '{providerName}': {ex.Message}"));
            }
        }

        public IReadOnlyList<string> GetProviderNames()
        {
            return _providers.Select(p => p.Name).ToList();
        }

        public async IAsyncEnumerable<string> StreamAsync(string providerName, string prompt, AIOptions options, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            var provider = GetProvider(providerName);
            if (provider == null)
            {
                yield return $"[Error] Provider '{providerName}' not found.";
                yield break;
            }

            if (!provider.IsAvailable)
            {
                yield return $"[Error] Provider '{providerName}' is not available.";
                yield break;
            }

            // Cannot yield inside catch, so capture errors in a sentinel variable
            string? initError = null;
            IAsyncEnumerator<string> enumerator = null!;
            try
            {
                enumerator = provider.StreamAsync(prompt, options, ct).GetAsyncEnumerator(ct);
            }
            catch (Exception ex)
            {
                initError = $"[Error] Initialization failed: {ex.Message}";
            }

            if (initError != null)
            {
                yield return initError;
                yield break;
            }

            while (true)
            {
                string? loopError = null;
                string chunk;
                try
                {
                    if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                        break;
                    chunk = enumerator.Current;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    loopError = $"[Error] Stream interrupted: {ex.Message}";
                    chunk = null!;
                }

                if (loopError != null)
                {
                    yield return loopError;
                    break;
                }
                yield return chunk;
            }
        }

        public IAIProvider? GetProvider(string name)
        {
            return _providers.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}

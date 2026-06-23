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

        public IAIProvider? GetProvider(string name)
        {
            return _providers.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}

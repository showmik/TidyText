using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TidyText.Core.AI
{
    public class AIProviderRouter : IAIProviderRouter
    {
        private readonly IEnumerable<IAIProvider> _providers;

        public AIProviderRouter(IEnumerable<IAIProvider> providers)
        {
            _providers = providers;
        }

        public async Task<AIResponse> RouteAsync(string providerName, string prompt, AIOptions options, CancellationToken ct = default)
        {
            var provider = _providers.FirstOrDefault(p => string.Equals(p.Name, providerName, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                return AIResponse.Error($"Provider '{providerName}' not found.");
            }

            if (!provider.IsAvailable)
            {
                return AIResponse.Error($"Provider '{providerName}' is not available. Please check API keys or connection.");
            }

            try
            {
                return await provider.CompleteAsync(prompt, options, ct);
            }
            catch (Exception ex)
            {
                return AIResponse.Error($"An error occurred with provider '{providerName}': {ex.Message}");
            }
        }
    }
}

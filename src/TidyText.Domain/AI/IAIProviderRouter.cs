using System.Threading;
using System.Threading.Tasks;

namespace TidyText.Domain.AI
{
    /// <summary>
    /// Abstracts AI provider routing so ViewModels and tests
    /// don't depend on the concrete router implementation.
    /// </summary>
    public interface IAIProviderRouter
    {
        Task<AIResponse> RouteAsync(string providerName, string prompt, AIOptions options, CancellationToken ct = default);
    }
}

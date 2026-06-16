using System.Threading;
using System.Threading.Tasks;

namespace TidyText.Core.AI
{
    public class AIOptions
    {
        public string Model { get; set; } = string.Empty;
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 2048;
        public string SystemPrompt { get; set; } = string.Empty;
    }

    public class AIResponse
    {
        public string Text { get; set; } = string.Empty;
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        
        public static AIResponse Success(string text) => new AIResponse { Text = text };
        public static AIResponse Error(string message) => new AIResponse { IsError = true, ErrorMessage = message };
    }

    public interface IAIProvider
    {
        string Name { get; }
        bool IsAvailable { get; }
        
        Task<AIResponse> CompleteAsync(string prompt, AIOptions options, CancellationToken ct = default);
    }
}

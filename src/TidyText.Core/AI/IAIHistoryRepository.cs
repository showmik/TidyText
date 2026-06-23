using System;
using System.Collections.Generic;

namespace TidyText.Core.AI
{
    /// <summary>
    /// DTO for persisting AI history items.
    /// Moved here from AIAssistantViewModel so Domain owns the contract.
    /// </summary>
    public class AIHistoryDto
    {
        public string Prompt { get; set; } = string.Empty;
        public string GeneratedText { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Abstracts AI history persistence so ViewModels don't
    /// perform raw file I/O (System.IO) themselves.
    /// </summary>
    public interface IAIHistoryRepository
    {
        List<AIHistoryDto> Load();
        void Save(IEnumerable<AIHistoryDto> items);
    }
}

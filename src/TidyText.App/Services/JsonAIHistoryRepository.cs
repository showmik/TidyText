using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TidyText.Domain.AI;

namespace TidyText.App.Services
{
    /// <summary>
    /// JSON file-based implementation of IAIHistoryRepository.
    /// Extracted from AIAssistantViewModel to remove raw File I/O
    /// from the presentation layer.
    /// </summary>
    public class JsonAIHistoryRepository : IAIHistoryRepository
    {
        private readonly string _filePath;

        public JsonAIHistoryRepository(string appDataFolderPath)
        {
            _filePath = Path.Combine(appDataFolderPath, "ai_history.json");
        }

        public List<AIHistoryDto> Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    var items = JsonSerializer.Deserialize<List<AIHistoryDto>>(json);
                    return items ?? new List<AIHistoryDto>();
                }
            }
            catch { }

            return new List<AIHistoryDto>();
        }

        public void Save(IEnumerable<AIHistoryDto> items)
        {
            try
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(items.ToList(), options);
                File.WriteAllText(_filePath, json);
            }
            catch { }
        }
    }
}

using TidyText.Domain.TextEngine.Processors;

namespace TidyText.Domain.TextEngine
{
    /// <summary>
    /// All user-facing cleaning toggles in one immutable DTO.
    /// </summary>
    public class CleaningOptions
    {
        public bool TrimStart { get; init; }
        public bool TrimEnd { get; init; }
        public bool RemoveMultipleSpaces { get; init; }
        public bool RemoveMultipleLines { get; init; }
        public bool RemoveAllLines { get; init; }
        public bool FixPunctuationSpacing { get; init; }
        public bool RemoveHtmlTags { get; init; }
        public bool ConvertSmartQuotes { get; init; }
        public bool StripMarkdown { get; init; }
        public CasingStyle CasingStyle { get; init; }
    }
}

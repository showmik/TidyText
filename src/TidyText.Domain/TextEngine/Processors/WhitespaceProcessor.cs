using System.Text;
using System.Text.RegularExpressions;

namespace TidyText.Domain.TextEngine.Processors
{
    public class WhitespaceProcessorOptions : ProcessorOptions
    {
        public bool TrimStart { get; set; } = false;
        public bool TrimEnd { get; set; } = false;
        public bool RemoveMultipleSpaces { get; set; } = false;
        public bool RemoveMultipleLines { get; set; } = false;
        public bool RemoveAllLines { get; set; } = false;
    }

    public class WhitespaceProcessor : ITextProcessor
    {
        public string Name => "Whitespace Processor";
        public string Description => "Handles trimming, line break removal, and whitespace normalization.";

        private readonly WhitespaceProcessorOptions _options;

        public WhitespaceProcessor(WhitespaceProcessorOptions? options = null)
        {
            _options = options ?? new WhitespaceProcessorOptions();
        }

        public string Process(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            string text = NormalizeNewlines(input);

            if (_options.TrimStart || _options.TrimEnd)
            {
                text = TrimEachLine(text, _options.TrimStart, _options.TrimEnd);
                if (_options.TrimStart) text = text.TrimStart();
                if (_options.TrimEnd) text = text.TrimEnd();
            }

            if (_options.RemoveMultipleSpaces)
                text = CollapseIntraLineWhitespace(text);

            if (_options.RemoveMultipleLines)
                text = ConvertMultipleLinesToSingle(text);

            if (_options.RemoveAllLines)
            {
                text = UnwrapAllLines(text);
                if (_options.RemoveMultipleSpaces) 
                    text = CollapseIntraLineWhitespace(text);
            }

            return text;
        }

        private static string NormalizeNewlines(string text)
        {
            if (text is null) return string.Empty;
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static string TrimEachLine(string text, bool trimStart, bool trimEnd)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (trimStart && trimEnd) line = line.Trim();
                else if (trimStart) line = line.TrimStart();
                else if (trimEnd) line = line.TrimEnd();
                lines[i] = line;
            }
            return string.Join("\n", lines);
        }

        private static string CollapseIntraLineWhitespace(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            text = NormalizeNewlines(text);
            var sb = new StringBuilder(text.Length);
            bool inWhitespace = false;
            foreach (var ch in text)
            {
                if (ch == '\n') { sb.Append('\n'); inWhitespace = false; continue; }
                if (char.IsWhiteSpace(ch))
                {
                    if (!inWhitespace) { sb.Append(' '); inWhitespace = true; }
                }
                else
                {
                    sb.Append(ch);
                    inWhitespace = false;
                }
            }
            return sb.ToString();
        }

        private static string ConvertMultipleLinesToSingle(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new StringBuilder(text.Length);
            int newlineCount = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\r')
                    continue;
                if (c == '\n')
                {
                    newlineCount++;
                    if (newlineCount < 3)
                        sb.Append('\n');
                }
                else
                {
                    sb.Append(c);
                    newlineCount = 0;
                }
            }
            return sb.ToString();
        }

        private static string UnwrapAllLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            text = NormalizeNewlines(text);
            var sb = new StringBuilder(text.Length);
            bool prevSpace = false;
            foreach (var ch in text)
            {
                if (ch == '\n')
                {
                    if (!prevSpace) { sb.Append(' '); prevSpace = true; }
                }
                else if (char.IsWhiteSpace(ch))
                {
                    if (!prevSpace) { sb.Append(' '); prevSpace = true; }
                }
                else
                {
                    sb.Append(ch);
                    prevSpace = false;
                }
            }
            return sb.ToString();
        }
    }
}

using System.Text.RegularExpressions;

namespace TidyText.Core.TextEngine.Processors
{
    public class MarkdownProcessorOptions : ProcessorOptions
    {
        public bool StripMarkdown { get; set; } = false;
    }

    public class MarkdownProcessor : ITextProcessor
    {
        public string Name => "Markdown Processor";
        public string Description => "Strips Markdown formatting characters to produce plain text.";

        private readonly MarkdownProcessorOptions _options;

        public MarkdownProcessor(MarkdownProcessorOptions? options = null)
        {
            _options = options ?? new MarkdownProcessorOptions();
        }

        public string Process(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            if (!_options.StripMarkdown)
                return input;

            // Simplified markdown stripping (bold, italic, strikethrough, headers, list items)
            string result = input;
            
            // Remove headers
            result = Regex.Replace(result, @"^#{1,6}\s+", "", RegexOptions.Multiline);
            
            // Remove bold/italic (**, __, *, _) - adding Singleline to handle multiline
            result = Regex.Replace(result, @"(\*\*|__)(.*?)\1", "$2", RegexOptions.Singleline);
            result = Regex.Replace(result, @"(\*|_)(.*?)\1", "$2", RegexOptions.Singleline);
            
            // Remove strikethrough
            result = Regex.Replace(result, @"~~(.*?)~~", "$1", RegexOptions.Singleline);
            
            // Remove images ![alt](url) -> alt (Must happen BEFORE links!)
            result = Regex.Replace(result, @"\!\[([^\]]+)\]\([^\)]+\)", "$1");

            // Remove links [text](url) -> text
            result = Regex.Replace(result, @"\[([^\]]+)\]\([^\)]+\)", "$1");
            
            // Remove blockquotes
            result = Regex.Replace(result, @"^\>\s+", "", RegexOptions.Multiline);

            return result;
        }
    }
}

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

        public string Process(string input, ProcessorOptions? options = null)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            var opts = options as MarkdownProcessorOptions;
            if (opts == null || !opts.StripMarkdown)
                return input;

            // Simplified markdown stripping (bold, italic, strikethrough, headers, list items)
            string result = input;
            
            // Remove headers
            result = Regex.Replace(result, @"^#{1,6}\s+", "", RegexOptions.Multiline);
            
            // Remove bold/italic (**, __, *, _)
            result = Regex.Replace(result, @"(\*\*|__)(.*?)\1", "$2");
            result = Regex.Replace(result, @"(\*|_)(.*?)\1", "$2");
            
            // Remove strikethrough
            result = Regex.Replace(result, @"~~(.*?)~~", "$1");
            
            // Remove links [text](url) -> text
            result = Regex.Replace(result, @"\[([^\]]+)\]\([^\)]+\)", "$1");
            
            // Remove images ![alt](url) -> alt
            result = Regex.Replace(result, @"\!\[([^\]]+)\]\([^\)]+\)", "$1");
            
            // Remove blockquotes
            result = Regex.Replace(result, @"^\>\s+", "", RegexOptions.Multiline);

            return result;
        }
    }
}

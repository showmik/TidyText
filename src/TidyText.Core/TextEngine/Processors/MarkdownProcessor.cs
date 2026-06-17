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

        private static readonly Regex HeadersRegex = new Regex(@"^#{1,6}\s+", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex BoldRegex = new Regex(@"(\*\*|__)(.*?)\1", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex ItalicRegex = new Regex(@"(\*|_)(.*?)\1", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex StrikethroughRegex = new Regex(@"~~(.*?)~~", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex ImagesRegex = new Regex(@"\!\[([^\]]+)\]\([^\)]+\)", RegexOptions.Compiled);
        private static readonly Regex LinksRegex = new Regex(@"\[([^\]]+)\]\([^\)]+\)", RegexOptions.Compiled);
        private static readonly Regex BlockquotesRegex = new Regex(@"^\>\s+", RegexOptions.Multiline | RegexOptions.Compiled);

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
            result = HeadersRegex.Replace(result, "");
            
            // Remove bold/italic (**, __, *, _) - adding Singleline to handle multiline
            result = BoldRegex.Replace(result, "$2");
            result = ItalicRegex.Replace(result, "$2");
            
            // Remove strikethrough
            result = StrikethroughRegex.Replace(result, "$1");
            
            // Remove images ![alt](url) -> alt (Must happen BEFORE links!)
            result = ImagesRegex.Replace(result, "$1");

            // Remove links [text](url) -> text
            result = LinksRegex.Replace(result, "$1");
            
            // Remove blockquotes
            result = BlockquotesRegex.Replace(result, "");

            return result;
        }
    }
}

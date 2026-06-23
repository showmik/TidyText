using System.Text.RegularExpressions;

namespace TidyText.Domain.TextEngine.Processors
{
    public class HtmlStripProcessorOptions : ProcessorOptions
    {
        public bool RemoveHtmlTags { get; set; } = false;
    }

    public class HtmlStripProcessor : ITextProcessor
    {
        public string Name => "HTML Strip Processor";
        public string Description => "Removes HTML tags from text.";

        private static readonly Regex HtmlRegex = new Regex("<.*?>", RegexOptions.Singleline | RegexOptions.Compiled);

        private readonly HtmlStripProcessorOptions _options;

        public HtmlStripProcessor(HtmlStripProcessorOptions? options = null)
        {
            _options = options ?? new HtmlStripProcessorOptions();
        }

        public string Process(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            if (!_options.RemoveHtmlTags)
                return input;

            return HtmlRegex.Replace(input, string.Empty);
        }
    }
}

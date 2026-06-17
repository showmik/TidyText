using System.Text.RegularExpressions;

namespace TidyText.Core.TextEngine.Processors
{
    public class HtmlStripProcessorOptions : ProcessorOptions
    {
        public bool RemoveHtmlTags { get; set; } = false;
    }

    public class HtmlStripProcessor : ITextProcessor
    {
        public string Name => "HTML Strip Processor";
        public string Description => "Removes HTML tags from text.";

        public string Process(string input, ProcessorOptions? options = null)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            var opts = options as HtmlStripProcessorOptions;
            if (opts != null && !opts.RemoveHtmlTags)
                return input;

            return Regex.Replace(input, "<.*?>", string.Empty, RegexOptions.Singleline);
        }
    }
}

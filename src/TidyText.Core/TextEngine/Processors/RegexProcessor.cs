using System.Text.RegularExpressions;

namespace TidyText.Core.TextEngine.Processors
{
    public class RegexProcessorOptions : ProcessorOptions
    {
        public string Pattern { get; set; } = string.Empty;
        public string Replacement { get; set; } = string.Empty;
        public RegexOptions RegexOptions { get; set; } = RegexOptions.None;
    }

    public class RegexProcessor : ITextProcessor
    {
        public string Name => "Regex Processor";
        public string Description => "Performs advanced regular expression substitutions.";

        public string Process(string input, ProcessorOptions? options = null)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            var opts = options as RegexProcessorOptions;
            if (opts == null || string.IsNullOrEmpty(opts.Pattern))
                return input;

            try
            {
                return Regex.Replace(input, opts.Pattern, opts.Replacement, opts.RegexOptions);
            }
            catch
            {
                // In case of invalid regex pattern, return original
                return input;
            }
        }
    }
}

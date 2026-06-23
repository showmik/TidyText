using System.Text.RegularExpressions;

namespace TidyText.Domain.TextEngine.Processors
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

        private readonly RegexProcessorOptions _options;

        public RegexProcessor(RegexProcessorOptions? options = null)
        {
            _options = options ?? new RegexProcessorOptions();
        }

        public string Process(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (string.IsNullOrEmpty(_options.Pattern)) return input;

            try
            {
                return Regex.Replace(input, _options.Pattern, _options.Replacement ?? string.Empty, _options.RegexOptions);
            }
            catch
            {
                // In case of invalid regex pattern, return original
                return input;
            }
        }
    }
}

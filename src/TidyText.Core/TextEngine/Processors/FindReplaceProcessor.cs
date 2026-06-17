using System;

namespace TidyText.Core.TextEngine.Processors
{
    public class FindReplaceProcessorOptions : ProcessorOptions
    {
        public string Find { get; set; } = string.Empty;
        public string Replace { get; set; } = string.Empty;
        public StringComparison ComparisonType { get; set; } = StringComparison.Ordinal;
    }

    public class FindReplaceProcessor : ITextProcessor
    {
        public string Name => "Find and Replace Processor";
        public string Description => "Performs simple text substitution.";

        private readonly FindReplaceProcessorOptions _options;

        public FindReplaceProcessor(FindReplaceProcessorOptions? options = null)
        {
            _options = options ?? new FindReplaceProcessorOptions();
        }

        public string Process(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (string.IsNullOrEmpty(_options.Find)) return input;

            return input.Replace(_options.Find, _options.Replace, _options.ComparisonType);
        }
    }
}

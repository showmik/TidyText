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

        public string Process(string input, ProcessorOptions? options = null)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            var opts = options as FindReplaceProcessorOptions;
            if (opts == null || string.IsNullOrEmpty(opts.Find))
                return input;

            return input.Replace(opts.Find, opts.Replace, opts.ComparisonType);
        }
    }
}

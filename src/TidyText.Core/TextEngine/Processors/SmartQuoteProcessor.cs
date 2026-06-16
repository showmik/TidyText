namespace TidyText.Core.TextEngine.Processors
{
    public class SmartQuoteProcessorOptions : ProcessorOptions
    {
        public bool ConvertSmartQuotes { get; set; } = false;
    }

    public class SmartQuoteProcessor : ITextProcessor
    {
        public string Name => "Smart Quote Processor";
        public string Description => "Converts curly quotes to straight quotes.";

        public string Process(string input, ProcessorOptions? options = null)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            var opts = options as SmartQuoteProcessorOptions;
            if (opts != null && !opts.ConvertSmartQuotes)
                return input;

            return input.Replace('“', '"').Replace('”', '"')
                        .Replace('‘', '\'').Replace('’', '\'');
        }
    }
}

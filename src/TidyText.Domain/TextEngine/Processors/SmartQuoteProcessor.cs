namespace TidyText.Domain.TextEngine.Processors
{
    public class SmartQuoteProcessorOptions : ProcessorOptions
    {
        public bool ConvertSmartQuotes { get; set; } = false;
    }

    public class SmartQuoteProcessor : ITextProcessor
    {
        public string Name => "Smart Quote Processor";
        public string Description => "Converts curly quotes to straight quotes.";

        private readonly SmartQuoteProcessorOptions _options;

        public SmartQuoteProcessor(SmartQuoteProcessorOptions? options = null)
        {
            _options = options ?? new SmartQuoteProcessorOptions();
        }

        public string Process(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            if (!_options.ConvertSmartQuotes)
                return input;

            return input.Replace('“', '"').Replace('”', '"')
                        .Replace('‘', '\'').Replace('’', '\'');
        }
    }
}

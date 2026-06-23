using System.Globalization;
using TidyText.Domain.TextEngine.Casing;

namespace TidyText.Domain.TextEngine.Processors
{
    public enum CasingStyle
    {
        DoNotChange,
        Uppercase,
        Lowercase,
        SentenceCase,
        TitleCase
    }

    public class CasingProcessor : ITextProcessor
    {
        public string Name => "Casing Processor";
        public string Description => "Converts text to uppercase, lowercase, sentence case, or title case.";

        private readonly ICasingStrategy _strategy;
        private readonly CultureInfo _culture;

        public CasingProcessor(ICasingStrategy strategy, CultureInfo? culture = null)
        {
            _strategy = strategy ?? new DoNotChangeStrategy();
            _culture = culture ?? CultureInfo.CurrentCulture;
        }

        public string Process(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            return _strategy.Convert(input, _culture);
        }
    }
}

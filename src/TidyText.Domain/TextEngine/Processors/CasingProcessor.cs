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

    public class CasingProcessorOptions : ProcessorOptions
    {
        public CasingStyle Style { get; set; } = CasingStyle.DoNotChange;
        public CultureInfo? Culture { get; set; }
    }

    public class CasingProcessor : ITextProcessor
    {
        public string Name => "Casing Processor";
        public string Description => "Converts text to uppercase, lowercase, sentence case, or title case.";

        private readonly CasingProcessorOptions _options;

        public CasingProcessor(CasingProcessorOptions? options = null)
        {
            _options = options ?? new CasingProcessorOptions();
        }

        public string Process(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            if (_options.Style == CasingStyle.DoNotChange)
                return input;

            var culture = _options.Culture ?? CultureInfo.CurrentCulture;

            switch (_options.Style)
            {
                case CasingStyle.Uppercase:
                    return input.ToUpper(culture);
                case CasingStyle.Lowercase:
                    return input.ToLower(culture);
                case CasingStyle.SentenceCase:
                    return SentenceCaseConverter.Default.Convert(input, culture);
                case CasingStyle.TitleCase:
                    return TitleCaseConverter.Default.Convert(input, culture);
                default:
                    return input;
            }
        }
    }
}

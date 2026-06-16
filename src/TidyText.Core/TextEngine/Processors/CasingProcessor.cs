using System.Globalization;
using TidyText.Core.TextEngine.Casing;

namespace TidyText.Core.TextEngine.Processors
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

        public string Process(string input, ProcessorOptions? options = null)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var opts = options as CasingProcessorOptions;
            if (opts == null || opts.Style == CasingStyle.DoNotChange)
                return input;

            var culture = opts.Culture ?? CultureInfo.CurrentCulture;

            switch (opts.Style)
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

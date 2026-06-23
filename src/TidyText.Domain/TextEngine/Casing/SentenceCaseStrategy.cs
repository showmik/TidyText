using System.Globalization;

namespace TidyText.Domain.TextEngine.Casing
{
    public class SentenceCaseStrategy : ICasingStrategy
    {
        public string Name => "Sentence Case";

        public string Convert(string input, CultureInfo culture)
        {
            return SentenceCaseConverter.Default.Convert(input, culture);
        }
    }
}

using System.Globalization;

namespace TidyText.Domain.TextEngine.Casing
{
    public class TitleCaseStrategy : ICasingStrategy
    {
        public string Name => "Title Case";

        public string Convert(string input, CultureInfo culture)
        {
            return TitleCaseConverter.Default.Convert(input, culture);
        }
    }
}

using System.Globalization;

namespace TidyText.Domain.TextEngine.Casing
{
    public class LowercaseStrategy : ICasingStrategy
    {
        public string Name => "Lowercase";

        public string Convert(string input, CultureInfo culture)
        {
            return input.ToLower(culture);
        }
    }
}

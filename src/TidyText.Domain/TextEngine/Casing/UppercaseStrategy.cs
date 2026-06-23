using System.Globalization;

namespace TidyText.Domain.TextEngine.Casing
{
    public class UppercaseStrategy : ICasingStrategy
    {
        public string Name => "Uppercase";

        public string Convert(string input, CultureInfo culture)
        {
            return input.ToUpper(culture);
        }
    }
}

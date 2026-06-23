using System.Globalization;

namespace TidyText.Domain.TextEngine.Casing
{
    public class DoNotChangeStrategy : ICasingStrategy
    {
        public string Name => "Do Not Change";

        public string Convert(string input, CultureInfo culture)
        {
            return input;
        }
    }
}

using System.Globalization;

namespace TidyText.Domain.TextEngine.Casing
{
    public interface ICasingStrategy
    {
        string Name { get; }
        string Convert(string input, CultureInfo culture);
    }
}

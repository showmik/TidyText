using System.Text;

namespace TidyText.Core.TextEngine.Processors
{
    public class UnicodeNormalizerOptions : ProcessorOptions
    {
        public NormalizationForm Form { get; set; } = NormalizationForm.FormC;
        public bool RemoveZeroWidthChars { get; set; } = true;
    }

    public class UnicodeNormalizer : ITextProcessor
    {
        public string Name => "Unicode Normalizer";
        public string Description => "Normalizes Unicode forms and strips zero-width characters.";

        private readonly UnicodeNormalizerOptions _options;

        public UnicodeNormalizer(UnicodeNormalizerOptions? options = null)
        {
            _options = options ?? new UnicodeNormalizerOptions();
        }

        public string Process(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            string result = input.Normalize(_options.Form);

            if (_options.RemoveZeroWidthChars)
            {
                result = result.Replace("\u200B", "") // Zero-width space
                               .Replace("\u200C", "") // Zero-width non-joiner
                               .Replace("\u200D", "") // Zero-width joiner
                               .Replace("\uFEFF", ""); // Zero-width no-break space
            }

            return result;
        }
    }
}

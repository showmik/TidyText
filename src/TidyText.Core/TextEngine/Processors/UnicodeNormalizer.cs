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

        public string Process(string input, ProcessorOptions? options = null)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            var opts = options as UnicodeNormalizerOptions ?? new UnicodeNormalizerOptions();

            string result = input.Normalize(opts.Form);

            if (opts.RemoveZeroWidthChars)
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

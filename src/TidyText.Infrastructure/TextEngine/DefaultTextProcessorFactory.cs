using TidyText.Domain.TextEngine;
using TidyText.Domain.TextEngine.Casing;
using TidyText.Domain.TextEngine.Processors;

namespace TidyText.Infrastructure.TextEngine
{
    public class DefaultTextProcessorFactory : ITextProcessorFactory
    {
        public CleaningPipeline BuildPipeline(CleaningOptions options)
        {
            return new CleaningPipeline()
                .AddProcessor(new MarkdownProcessor()) // Strips markdown if enabled
                .AddProcessor(new HtmlStripProcessor(new HtmlStripProcessorOptions
                    { RemoveHtmlTags = options.RemoveHtmlTags }))
                .AddProcessor(new SmartQuoteProcessor(new SmartQuoteProcessorOptions
                    { ConvertSmartQuotes = options.ConvertSmartQuotes }))
                .AddProcessor(new WhitespaceProcessor(new WhitespaceProcessorOptions
                {
                    TrimStart = options.TrimStart,
                    TrimEnd = options.TrimEnd,
                    RemoveMultipleSpaces = options.RemoveMultipleSpaces,
                    RemoveMultipleLines = options.RemoveMultipleLines,
                    RemoveAllLines = options.RemoveAllLines
                }))
                .AddProcessor(new PunctuationProcessor(new PunctuationProcessorOptions
                    { FixPunctuationSpacing = options.FixPunctuationSpacing,
                      TreatColonAsSentencePunct = true }))
                .AddProcessor(new CasingProcessor(GetCasingStrategy(options.CasingStyle)));
        }

        private ICasingStrategy GetCasingStrategy(CasingStyle style)
        {
            return style switch
            {
                CasingStyle.Uppercase => new UppercaseStrategy(),
                CasingStyle.Lowercase => new LowercaseStrategy(),
                CasingStyle.SentenceCase => new SentenceCaseStrategy(),
                CasingStyle.TitleCase => new TitleCaseStrategy(),
                _ => new DoNotChangeStrategy()
            };
        }
    }
}

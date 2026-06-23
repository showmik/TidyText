using TidyText.Domain.TextEngine;
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
                .AddProcessor(new CasingProcessor(new CasingProcessorOptions
                    { Style = options.CasingStyle }));
        }
    }
}

using System;
using TidyText.Domain.TextEngine.Processors;
using TidyText.Domain.TextEngine.Casing;

namespace TidyText.Domain.TextEngine
{
    public class TextPipelineBuilder
    {
        private readonly CleaningPipeline _pipeline;

        public TextPipelineBuilder()
        {
            _pipeline = new CleaningPipeline();
        }

        public TextPipelineBuilder If(bool condition, Action<TextPipelineBuilder> action)
        {
            if (condition)
            {
                action(this);
            }
            return this;
        }

        public TextPipelineBuilder AddProcessor(ITextProcessor processor)
        {
            // Automatically wrap all processors with the Error Handling & Telemetry decorator
            _pipeline.AddProcessor(new TextProcessorErrorHandlingDecorator(processor));
            return this;
        }

        // --- Fluent Convenience Methods ---

        public TextPipelineBuilder AddMarkdownStripper() => 
            AddProcessor(new MarkdownProcessor(new MarkdownProcessorOptions { StripMarkdown = true }));

        public TextPipelineBuilder AddHtmlStripper() => 
            AddProcessor(new HtmlStripProcessor(new HtmlStripProcessorOptions { RemoveHtmlTags = true }));

        public TextPipelineBuilder AddSmartQuotes() => 
            AddProcessor(new SmartQuoteProcessor(new SmartQuoteProcessorOptions { ConvertSmartQuotes = true }));

        public TextPipelineBuilder AddWhitespaceCleaning(bool trimStart, bool trimEnd, bool removeMultiSpaces, bool removeMultiLines, bool removeAllLines) =>
            AddProcessor(new WhitespaceProcessor(new WhitespaceProcessorOptions
            {
                TrimStart = trimStart,
                TrimEnd = trimEnd,
                RemoveMultipleSpaces = removeMultiSpaces,
                RemoveMultipleLines = removeMultiLines,
                RemoveAllLines = removeAllLines
            }));

        public TextPipelineBuilder AddPunctuationCleaning(bool fixSpacing) =>
            AddProcessor(new PunctuationProcessor(new PunctuationProcessorOptions
            {
                FixPunctuationSpacing = fixSpacing,
                TreatColonAsSentencePunct = true
            }));

        public TextPipelineBuilder AddCasing(CasingStyle style)
        {
            ICasingStrategy strategy = style switch
            {
                CasingStyle.Uppercase => new UppercaseStrategy(),
                CasingStyle.Lowercase => new LowercaseStrategy(),
                CasingStyle.SentenceCase => new SentenceCaseStrategy(),
                CasingStyle.TitleCase => new TitleCaseStrategy(),
                _ => new DoNotChangeStrategy()
            };
            
            return AddProcessor(new CasingProcessor(strategy));
        }

        public CleaningPipeline Build()
        {
            return _pipeline;
        }
    }
}

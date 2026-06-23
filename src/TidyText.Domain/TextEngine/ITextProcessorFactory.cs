namespace TidyText.Domain.TextEngine
{
    /// <summary>
    /// Builds the processor pipeline from the current user options.
    /// New processors are added by creating a new ITextProcessor + updating
    /// the factory — the ViewModel never changes.
    /// </summary>
    public interface ITextProcessorFactory
    {
        CleaningPipeline BuildPipeline(CleaningOptions options);
    }
}

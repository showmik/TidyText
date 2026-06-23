using System.Collections.Generic;

namespace TidyText.Domain.TextEngine
{
    public class ProcessorOptions
    {
        // Add shared options if any
    }

    public interface ITextProcessor
    {
        string Name { get; }
        string Description { get; }
        
        /// <summary>
        /// Processes the input text and returns the modified text.
        /// </summary>
        string Process(string input);
    }
}

using System.Collections.Generic;

namespace TidyText.Core.TextEngine
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
        string Process(string input, ProcessorOptions? options = null);
    }
}

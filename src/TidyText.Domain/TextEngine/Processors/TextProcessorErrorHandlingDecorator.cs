using System;
using System.Diagnostics;
using TidyText.Domain.TextEngine;

namespace TidyText.Domain.TextEngine.Processors
{
    public class TextProcessorErrorHandlingDecorator : ITextProcessor
    {
        private readonly ITextProcessor _inner;

        public TextProcessorErrorHandlingDecorator(ITextProcessor inner)
        {
            _inner = inner;
        }

        public string Name => _inner.Name;
        public string Description => _inner.Description;

        public string Process(string input)
        {
            try
            {
                // Here we could also add telemetry/logging using a Stopwatch
                var result = _inner.Process(input);
                return result;
            }
            catch (Exception ex)
            {
                // In a real app we'd log this exception using an injected ILogger.
                // For now, we gracefully fallback and return the unmodified input
                // so the pipeline doesn't crash entirely.
                Debug.WriteLine($"[Error in {_inner.Name}]: {ex.Message}");
                return input;
            }
        }
    }
}

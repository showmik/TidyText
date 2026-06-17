using System.Collections.Generic;
using System.Linq;

namespace TidyText.Core.TextEngine
{
    public class CleaningPipeline
    {
        private readonly List<ITextProcessor> _processors = new();

        public IReadOnlyList<ITextProcessor> Processors => _processors;

        public CleaningPipeline AddProcessor(ITextProcessor processor)
        {
            _processors.Add(processor);
            return this;
        }

        public CleaningPipeline RemoveProcessor(string processorName)
        {
            _processors.RemoveAll(p => p.Name == processorName);
            return this;
        }

        public CleaningPipeline ClearProcessors()
        {
            _processors.Clear();
            return this;
        }

        public string Process(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            string result = input;
            foreach (var processor in _processors)
            {
                result = processor.Process(result);
            }

            return result;
        }
    }
}

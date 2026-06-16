using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace TidyText.Core.Statistics
{
    public class TextStatistics
    {
        public int WordCount { get; }
        public int CharacterCount { get; }
        public int SentenceCount { get; }
        public int ParagraphCount { get; }
        public int LineCount { get; }
        
        // Count syllables for readability formulas
        public int SyllableCount { get; }

        private TextStatistics(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                WordCount = 0;
                CharacterCount = 0;
                SentenceCount = 0;
                ParagraphCount = 0;
                LineCount = 0;
                SyllableCount = 0;
                return;
            }

            CharacterCount = text.Length;
            LineCount = text.Split('\n').Length;
            ParagraphCount = Regex.Split(text, @"\n+").Count(p => !string.IsNullOrWhiteSpace(p));
            
            var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            WordCount = words.Length;
            
            string[] sentences = Regex.Split(text, @"(?<=[.!?])\s+");
            SentenceCount = sentences.Count(s => !string.IsNullOrWhiteSpace(s));

            SyllableCount = words.Sum(w => CountSyllables(w));
        }

        public static TextStatistics Calculate(string text)
        {
            return new TextStatistics(text);
        }

        private static int CountSyllables(string word)
        {
            word = word.ToLowerInvariant();
            if (word.Length <= 3) return 1;

            word = Regex.Replace(word, @"(?:[^laeiouy]es|ed|[^laeiouy]e)$", "");
            word = Regex.Replace(word, @"^y", "");
            var matches = Regex.Matches(word, @"[aeiouy]{1,2}");
            return Math.Max(1, matches.Count);
        }
    }
}

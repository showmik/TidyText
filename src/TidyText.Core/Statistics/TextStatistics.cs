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
        public int LetterAndDigitCount { get; }
        
        // Count syllables for readability formulas
        public int SyllableCount { get; }
        
        // Count words with more than 6 letters for LIX readability
        public int LongWordCount { get; }

        private static readonly Regex ParagraphRegex = new Regex(@"(?:\r?\n[\t\x20]*){2,}", RegexOptions.Compiled);
        private static readonly Regex SentenceRegex = new Regex(@"(?<=(?<!\b(?:mr|mrs|ms|dr|prof|sr|jr|vs|etc|e\.g|i\.e))[.!?])\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SyllableSuffixRegex = new Regex(@"(?:[^laeiouy]es|ed|[^laeiouy]e)$", RegexOptions.Compiled);
        private static readonly Regex SyllablePrefixRegex = new Regex(@"^y", RegexOptions.Compiled);
        private static readonly Regex SyllableVowelsRegex = new Regex(@"[aeiouy]+", RegexOptions.Compiled);

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
                LongWordCount = 0;
                return;
            }

            CharacterCount = text.Length;
            LetterAndDigitCount = text.Count(char.IsLetterOrDigit);
            LineCount = text.Split('\n').Length;
            
            // Paragraphs should be separated by two or more newlines (with optional carriage returns or spaces between them)
            ParagraphCount = ParagraphRegex.Split(text).Count(p => !string.IsNullOrWhiteSpace(p));
            
            var words = text.Split(new[] { ' ', '\t', '\n', '\r', '—', '–' }, StringSplitOptions.RemoveEmptyEntries);
            WordCount = words.Length;
            
            string[] sentences = SentenceRegex.Split(text);
            SentenceCount = sentences.Count(s => !string.IsNullOrWhiteSpace(s));

            SyllableCount = words.Sum(w => CountSyllables(w));
            LongWordCount = words.Count(w => w.Count(char.IsLetter) > 6);
        }

        public static TextStatistics Calculate(string text)
        {
            return new TextStatistics(text);
        }

        private static int CountSyllables(string word)
        {
            // Strip punctuation before checking length or applying regexes
            word = new string(word.Where(char.IsLetter).ToArray()).ToLowerInvariant();
            
            if (word.Length <= 3) return 1;

            word = SyllableSuffixRegex.Replace(word, "");
            word = SyllablePrefixRegex.Replace(word, "");
            var matches = SyllableVowelsRegex.Matches(word);
            return Math.Max(1, matches.Count);
        }
    }
}

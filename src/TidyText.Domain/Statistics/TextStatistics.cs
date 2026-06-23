using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace TidyText.Domain.Statistics
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
        private static readonly Regex SentenceRegex = new Regex(@"(?<=(?<!\b(?:[Mm]r|[Mm]rs|[Mm]s|[Dd]r|[Pp]rof|[Ss]r|[Jj]r|vs|etc|e\.g|i\.e))[.!?][""']?)\s+(?=[\p{Lu}\p{N}""'])", RegexOptions.Compiled);
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

            // Original raw stats
            CharacterCount = text.Length;
            LineCount = text.Split('\n').Length;

            // Strip markdown code blocks before processing readability stats
            var codeBlockRegex = new Regex(@"```.*?```", RegexOptions.Singleline | RegexOptions.Compiled);
            string processedText = codeBlockRegex.Replace(text, " ");

            LetterAndDigitCount = processedText.Count(char.IsLetterOrDigit);
            
            // Paragraphs should be separated by two or more newlines (with optional carriage returns or spaces between them)
            ParagraphCount = ParagraphRegex.Split(processedText).Count(p => !string.IsNullOrWhiteSpace(p));
            
            var words = processedText.Split(new[] { ' ', '\t', '\n', '\r', '—', '–' }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(w => w.Any(char.IsLetterOrDigit))
                            .ToArray();
            WordCount = words.Length;
            
            // Sentence counting: split by paragraphs and bullet points first to prevent massive run-on lists
            var blockRegex = new Regex(@"(?:\n\s*\n)|(?:\n\s*(?=[-*\u2022]|\d+\.\s))", RegexOptions.Compiled);
            var blocks = blockRegex.Split(processedText).Where(b => !string.IsNullOrWhiteSpace(b));

            int sentenceCount = 0;
            foreach (var block in blocks)
            {
                var subSentences = SentenceRegex.Split(block).Where(s => !string.IsNullOrWhiteSpace(s));
                sentenceCount += subSentences.Count();
            }
            SentenceCount = sentenceCount;

            SyllableCount = words.Sum(w => CountSyllables(w));
            LongWordCount = words.Count(w => w.Count(char.IsLetter) > 6 && !w.StartsWith("http") && !w.StartsWith("www.") && !w.Contains("@"));
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

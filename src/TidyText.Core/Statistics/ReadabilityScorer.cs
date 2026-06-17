using System;

namespace TidyText.Core.Statistics
{
    public class ReadabilityScores
    {
        public double LixIndex { get; set; }
        public double FleschKincaidGradeLevel { get; set; }
        public double ColemanLiauIndex { get; set; }
        public double AutomatedReadabilityIndex { get; set; }
        
        public string ReadingEaseDescription
        {
            get
            {
                if (LixIndex < 30) return "Very Easy";
                if (LixIndex < 40) return "Easy";
                if (LixIndex < 50) return "Standard";
                if (LixIndex < 60) return "Difficult";
                return "Very Difficult";
            }
        }
    }

    public static class ReadabilityScorer
    {
        public static ReadabilityScores Calculate(TextStatistics stats)
        {
            if (stats.WordCount == 0 || stats.SentenceCount == 0)
                return new ReadabilityScores();

            double wordsPerSentence = (double)stats.WordCount / stats.SentenceCount;
            double syllablesPerWord = (double)stats.SyllableCount / stats.WordCount;
            double lettersPerWord = (double)stats.LetterAndDigitCount / stats.WordCount;

            return new ReadabilityScores
            {
                LixIndex = wordsPerSentence + (((double)stats.LongWordCount * 100) / stats.WordCount),
                FleschKincaidGradeLevel = (0.39 * wordsPerSentence) + (11.8 * syllablesPerWord) - 15.59,
                ColemanLiauIndex = (0.0588 * (lettersPerWord * 100)) - (0.296 * (100.0 / wordsPerSentence)) - 15.8,
                AutomatedReadabilityIndex = (4.71 * lettersPerWord) + (0.5 * wordsPerSentence) - 21.43
            };
        }
    }
}

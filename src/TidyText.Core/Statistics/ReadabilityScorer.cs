using System;

namespace TidyText.Core.Statistics
{
    public class ReadabilityScores
    {
        public double FleschReadingEase { get; set; }
        public double FleschKincaidGradeLevel { get; set; }
        public double ColemanLiauIndex { get; set; }
        public double AutomatedReadabilityIndex { get; set; }
        
        public string ReadingEaseDescription
        {
            get
            {
                if (FleschReadingEase >= 90) return "Very Easy";
                if (FleschReadingEase >= 80) return "Easy";
                if (FleschReadingEase >= 70) return "Fairly Easy";
                if (FleschReadingEase >= 60) return "Standard";
                if (FleschReadingEase >= 50) return "Fairly Difficult";
                if (FleschReadingEase >= 30) return "Difficult";
                return "Very Confusing";
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
                FleschReadingEase = 206.835 - (1.015 * wordsPerSentence) - (84.6 * syllablesPerWord),
                FleschKincaidGradeLevel = (0.39 * wordsPerSentence) + (11.8 * syllablesPerWord) - 15.59,
                ColemanLiauIndex = (0.0588 * (lettersPerWord * 100)) - (0.296 * (100.0 / wordsPerSentence)) - 15.8,
                AutomatedReadabilityIndex = (4.71 * lettersPerWord) + (0.5 * wordsPerSentence) - 21.43
            };
        }
    }
}

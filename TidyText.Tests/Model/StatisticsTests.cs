using NUnit.Framework;
using TidyText.Core.Statistics;

namespace TidyText.Tests.Model
{
    [TestFixture]
    public class StatisticsTests
    {
        [Test]
        public void TextStatistics_CalculatesCountsCorrectly()
        {
            var text = "Hello world! This is a test.\n\nIt has multiple sentences.";
            var stats = TextStatistics.Calculate(text);

            Assert.That(stats.SentenceCount, Is.EqualTo(3));
            Assert.That(stats.WordCount, Is.EqualTo(10));
            Assert.That(stats.ParagraphCount, Is.EqualTo(2));
            Assert.That(stats.LetterAndDigitCount, Is.EqualTo(43));
        }

        [Test]
        public void ReadabilityScorer_UsesLettersForColemanLiau()
        {
            var text = "The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog.";
            var stats = TextStatistics.Calculate(text);
            var scores = ReadabilityScorer.Calculate(stats);

            // 18 words, 2 sentences. 
            // Words: "The quick brown fox jumps over the lazy dog." (9 words) x 2 = 18 words
            // Letters: 35 x 2 = 70 letters
            // L = (70 / 18) * 100 = 388.88
            // S = (2 / 18) * 100 = 11.11
            // CLI = 0.0588 * 388.88 - 0.296 * 11.11 - 15.8 = 22.86 - 3.28 - 15.8 = 3.78

            Assert.That(scores.ColemanLiauIndex, Is.EqualTo(3.78).Within(0.1));
            
            // ARI = 4.71 * (70 / 18) + 0.5 * (18 / 2) - 21.43
            // ARI = 4.71 * 3.888 + 4.5 - 21.43 = 18.31 + 4.5 - 21.43 = 1.38
            Assert.That(scores.AutomatedReadabilityIndex, Is.EqualTo(1.38).Within(0.1));
        }

        [Test]
        public void ReadabilityScorer_CalculatesLixCorrectly()
        {
            var text = "Paste this code into your application to complete authentication.";
            var stats = TextStatistics.Calculate(text);
            var scores = ReadabilityScorer.Calculate(stats);

            // 9 words, 1 sentence.
            // Long words (> 6 letters): "application", "complete", "authentication" (3 words)
            // LIX = (9 / 1) + (3 * 100 / 9) = 9 + 33.333 = 42.333
            
            Assert.That(stats.LongWordCount, Is.EqualTo(3));
            Assert.That(scores.LixIndex, Is.EqualTo(42.33).Within(0.1));
            Assert.That(scores.ReadingEaseDescription, Is.EqualTo("Standard"));
        }

        [Test]
        public void TextStatistics_ParagraphCount_UsesDoubleNewline()
        {
            var text = "Line 1\nLine 2\n\nParagraph 2\n\nParagraph 3";
            var stats = TextStatistics.Calculate(text);
            
            Assert.That(stats.ParagraphCount, Is.EqualTo(3));
            Assert.That(stats.LineCount, Is.EqualTo(6));
        }
    }
}

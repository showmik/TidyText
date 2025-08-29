using System.Globalization;
using NUnit.Framework;
using TidyText.Model.Casing;

namespace TidyText.Tests.Model.Casing
{
    [TestFixture]
    public class SentenceCaseConverterTests
    {
        private static SentenceCaseConverter NewConverter(SentenceCaseOptions? opt = null)
            => new SentenceCaseConverter(DefaultSentenceCaseLexicon.Instance,
                                         opt ?? new SentenceCaseOptions { Culture = CultureInfo.InvariantCulture });

        // --- Canonical brand / proper names (map + tokens) ---
        [TestCase("watch YouTube today. ok.", "Watch YouTube today. Ok.")]
        [TestCase("we love iPhone 15. nice.", "We love iPhone 15. Nice.")]
        [TestCase("we test iOS and macOS now. ok.", "We test iOS and macOS now. Ok.")]
        [TestCase("we use OpenAI models. cool.", "We use OpenAI models. Cool.")]
        public void CanonicalBrands_ArePreserved(string input, string expected)
        {
            var c = NewConverter();
            Assert.That(c.Convert(input), Is.EqualTo(expected));
        }

        // --- Start of sentence capitalization ---
        [TestCase("hello there. ok.", "Hello there. Ok.")]
        [TestCase("first line.\nsecond line.", "First line.\nSecond line.")]
        public void SentenceStart_IsCapitalized_AfterTerminators_AndNewlines(string input, string expected)
        {
            var c = NewConverter();
            Assert.That(c.Convert(input), Is.EqualTo(expected));
        }

        // --- Abbreviations (do NOT start a new sentence) ---
        [Test]
        public void DottedAbbreviations_DoNotTriggerNewSentence()
        {
            var c = NewConverter();
            var input = "We meet at 5 p.m. today. ok.";
            var expected = "We meet at 5 p.m. today. Ok.";
            Assert.That(c.Convert(input), Is.EqualTo(expected));
        }

        // --- Decimal guard: "3.14" does not end a sentence ---
        [Test]
        public void Decimal_DoesNotEndSentence()
        {
            var c = NewConverter();
            var input = "Version 3.14 is out. ok.";
            var expected = "Version 3.14 is out. Ok.";
            Assert.That(c.Convert(input), Is.EqualTo(expected));
        }

        // --- Quotes / wrappers after a terminator ("\" ' ) ] }) ---
        [Test]
        public void Wrappers_AfterTerminator_StillCapitalizeNextWord()
        {
            var c = NewConverter();
            var input = "He paused. \"ok\" (cool).";
            var expected = "He paused. \"Ok\" (Cool).";
            Assert.That(c.Convert(input), Is.EqualTo(expected));
        }

        // --- Single-letter pronoun I ---
        [Test]
        public void Pronoun_I_IsAlwaysUppercase()
        {
            var c = NewConverter();
            var input = "i did it. and i won.";
            var expected = "I did it. And I won.";
            Assert.That(c.Convert(input), Is.EqualTo(expected));
        }

        // --- Camel / mixed case should be preserved mid-sentence ---
        [Test]
        public void CamelOrMixedCase_IsPreserved()
        {
            var c = NewConverter();
            var input = "We like eBay and OpenAI tools.";
            var expected = "We like eBay and OpenAI tools.";
            Assert.That(c.Convert(input), Is.EqualTo(expected));
        }

        // --- Brand + suffix (e.g., "iPhone Pro") and digit + suffix ("15 Pro") ---
        [Test]
        public void BrandOrDigit_AllowsModelSuffixToCapitalize()
        {
            var c = NewConverter();
            Assert.That(c.Convert("iphone pro rocks."), Is.EqualTo("iPhone Pro rocks."));
            Assert.That(c.Convert("we love 15 pro."), Is.EqualTo("We love 15 Pro."));
        }

        // --- Acronyms mid-sentence (whitelist) ---
        [Test]
        public void WhitelistedAcronyms_MidSentence_ArePreserved_ByDefault()
        {
            var c = NewConverter(); // PreserveAcronymsMidSentence = true (default)
            var input = "We like GPU drivers and GPT models. ok.";
            var expected = "We like GPU drivers and GPT models. Ok.";
            Assert.That(c.Convert(input), Is.EqualTo(expected));
        }

        // --- Unknown short ALL-CAPS mid-sentence: default behavior is to lowercase them ---
        [Test]
        public void UnknownShortAllCaps_MidSentence_Default_IsLowercased()
        {
            var c = NewConverter(); // TreatUnknownShortAllCapsAsAcronym = false (default)
            var input = "We met XYZ today. ok.";
            var expected = "We met xyz today. Ok.";
            Assert.That(c.Convert(input), Is.EqualTo(expected));
        }

        // --- Unknown short ALL-CAPS mid-sentence can be preserved if option is enabled ---
        [Test]
        public void UnknownShortAllCaps_MidSentence_Preserved_WhenOptionEnabled()
        {
            var c = NewConverter(new SentenceCaseOptions
            {
                Culture = CultureInfo.InvariantCulture,
                PreserveAcronymsMidSentence = true,
                TreatUnknownShortAllCapsAsAcronym = true
            });
            var input = "We met XYZ today. ok.";
            var expected = "We met XYZ today. Ok.";
            Assert.That(c.Convert(input), Is.EqualTo(expected));
        }

        // --- Disabling acronym preservation mid-sentence forces lowercase for unknown ALL-CAPS ---
        [Test]
        public void DisablingMidSentenceAcronyms_LowercasesUnknownAllCaps()
        {
            var c = NewConverter(new SentenceCaseOptions
            {
                Culture = CultureInfo.InvariantCulture,
                PreserveAcronymsMidSentence = false,
                TreatUnknownShortAllCapsAsAcronym = true // ignored when preserve=false
            });
            var input = "We met XYZ today. ok.";
            var expected = "We met xyz today. Ok.";
            Assert.That(c.Convert(input), Is.EqualTo(expected));
        }

        // --- Honorific / dotted-abbrev before a capitalized name: keep incoming TitleCase mid-sentence ---
        [Test]
        public void HonorificBeforeName_PreservesIncomingTitleCase()
        {
            var c = NewConverter();
            var input = "Mr. Smith is here. ok.";
            var expected = "Mr. Smith is here. Ok.";
            Assert.That(c.Convert(input), Is.EqualTo(expected));
        }

        // --- Apostrophes (both ASCII and curly) should capitalize next letter when needed ---
        [Test]
        public void Apostrophes_AreHandledForCapitalization()
        {
            var c = NewConverter();
            Assert.That(c.Convert("o'neill arrived. ok."), Is.EqualTo("O'Neill arrived. Ok."));
            Assert.That(c.Convert("d’angelo arrived. ok."), Is.EqualTo("D’Angelo arrived. Ok."));
        }

        // --- Newline at start: Normalize CRLF and still treat next token as sentence start ---
        [Test]
        public void NewlineNormalization_TreatsNextTokenAsSentenceStart()
        {
            var c = NewConverter();
            var input = "hello\r\nworld. ok.";
            var expected = "Hello\nWorld. Ok.";
            Assert.That(c.Convert(input), Is.EqualTo(expected));
        }
    }
}
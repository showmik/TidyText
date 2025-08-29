using NUnit.Framework;
using TidyText.Model.Casing;

namespace TidyText.Tests.Model.Casing
{
    [TestFixture]
    internal class TitleCaseConverterTests
    {
        [TestCase("an in-depth look at gpu-based ai: the road to gpt-5",
          "An In-Depth Look at GPU-Based AI: The Road to GPT-5")]
        [TestCase("THE STATE OF THE ART IN MACHINE LEARNING",
          "The State of the Art in Machine Learning")]
        [TestCase("from a to z: an ap guide",
          "From A to Z: An AP Guide")]
        [TestCase("iphone vs. pixel: which is better?",
          "iPhone vs. Pixel: Which Is Better?")]
        [TestCase("meet macOS and eBay at the expo",
          "Meet macOS and eBay at the Expo")]

        // --- Edge cases below ---

        [TestCase("", "")]
        [TestCase(" ", " ")]
        [TestCase("   ", "   ")]
        [TestCase("1234", "1234")]
        [TestCase("42 is the answer", "42 Is the Answer")]
        [TestCase("hello! is this working?", "Hello! Is This Working?")]
        [TestCase("a.b.c. easy as 1.2.3.", "A.B.C. Easy as 1.2.3.")]
        [TestCase("ALL CAPS SENTENCE", "All Caps Sentence")]
        [TestCase("mixed CaSe inPUT", "Mixed Case Input")]
        [TestCase("title with emoji 😊 and symbols #hashtag", "Title With Emoji 😊 and Symbols #Hashtag")]
        [TestCase("über alles: naïve café résumé", "Über Alles: Naïve Café Résumé")]
        [TestCase("newline\nin the title", "Newline\nIn the Title")]
        [TestCase("tab\tseparated", "Tab\tSeparated")]
        [TestCase("punctuation!@#$%^&*() remains", "Punctuation!@#$%^&*() Remains")]
        [TestCase("the quick-brown_fox jumps/over the_lazy dog.", "The Quick-Brown_Fox Jumps/Over the_Lazy Dog.")]
        [TestCase("a single", "A Single")]
        [TestCase("a", "A")]
        [TestCase("i", "I")]
        [TestCase("o'neill and d'angelo", "O'Neill and D'Angelo")]
        [TestCase("macdonald's farm", "MacDonald's Farm")]
        [TestCase("e=mc^2: einstein's equation", "E=MC^2: Einstein's Equation")]
        [TestCase("[brackets] and (parentheses) included", "[Brackets] and (Parentheses) Included")]
        [TestCase("rock ’n’ roll: the best of the best", "Rock ’n’ Roll: The Best of the Best")]
        [TestCase("co-founder and co-chair", "Co-Founder and Co-Chair")]
        [TestCase("state-of-the-art design", "State-of-the-Art Design")]
        [TestCase("input/output and read/write", "Input/Output and Read/Write")]
        [TestCase("c++ vs. c# vs. go", "C++ vs. C# vs. Go")]
        [TestCase("“quoted” words inside", "“Quoted” Words Inside")]
        [TestCase("— dash start cases", "— Dash Start Cases")]
        [TestCase("  multiple   spaces  title  ", "  Multiple   Spaces  Title  ")]
        [TestCase("o'reilly's book", "O'Reilly's Book")]
        [TestCase("we'll rock you", "We'll Rock You")]
        [TestCase("don't stop now", "Don't Stop Now")]
        [TestCase("u.s. federal policy", "U.S. Federal Policy")]
        [TestCase("Q&A: what's new", "Q&A: What's New")]
        [TestCase("per-user and per-app limits", "Per-User and Per-App Limits")]
        [TestCase("naïve façades coöperate", "Naïve Façades Coöperate")]
        [TestCase("crlf\r\nnext line caps", "Crlf\r\nNext Line Caps")]
        [TestCase("m dash — new section", "M Dash — New Section")]
        [TestCase("the 'quoted' word", "The 'Quoted' Word")]
        [TestCase("the \"quoted\" word", "The \"Quoted\" Word")]
        [TestCase("email addresses like user@example.com", "Email Addresses Like user@example.com")]
        [TestCase("number ranges 1–10 and 2—3", "Number Ranges 1–10 and 2—3")]
        [TestCase("variables x^2 and y^3", "Variables X^2 and Y^3")]
        [TestCase("we’re excited", "We’re Excited")]
        public void TitleCase_AP_Rules(string input, string expected)
        {
            var tc = TitleCaseConverter.Default;
            Assert.That(tc.Convert(input), Is.EqualTo(expected));
        }
    }
}

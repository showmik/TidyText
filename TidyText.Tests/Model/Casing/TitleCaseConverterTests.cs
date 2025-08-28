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
        public void TitleCase_AP_Rules(string input, string expected)
        {
            var tc = TitleCaseConverter.Default;
            Assert.That(tc.Convert(input), Is.EqualTo(expected));
        }
    }
}

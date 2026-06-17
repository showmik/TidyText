using NUnit.Framework;
using TidyText.Core.TextEngine.Processors;

namespace TidyText.Tests.Model
{
    [TestFixture]
    public class HtmlStripProcessorTests
    {
        private readonly HtmlStripProcessor _processor = new HtmlStripProcessor();

        [Test]
        public void StripHtml_RemovesSimpleTags()
        {
            var opts = new HtmlStripProcessorOptions { RemoveHtmlTags = true };
            var result = _processor.Process("Hello <b>World</b>!", opts);
            Assert.That(result, Is.EqualTo("Hello World!"));
        }

        [Test]
        public void StripHtml_RemovesTagsWithAttributes()
        {
            var opts = new HtmlStripProcessorOptions { RemoveHtmlTags = true };
            var result = _processor.Process("<a href=\"http://example.com\">Link</a>", opts);
            Assert.That(result, Is.EqualTo("Link"));
        }

        [Test]
        public void StripHtml_RemovesMultilineTags()
        {
            var opts = new HtmlStripProcessorOptions { RemoveHtmlTags = true };
            var result = _processor.Process("Hello <div\nclass=\"test\">World</div>!", opts);
            Assert.That(result, Is.EqualTo("Hello World!"));
        }

        [Test]
        public void StripHtml_DoesNotStripIfOptionIsFalse()
        {
            var opts = new HtmlStripProcessorOptions { RemoveHtmlTags = false };
            var result = _processor.Process("Hello <b>World</b>!", opts);
            Assert.That(result, Is.EqualTo("Hello <b>World</b>!"));
        }
    }
}

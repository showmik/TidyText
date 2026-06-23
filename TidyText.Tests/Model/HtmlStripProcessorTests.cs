using NUnit.Framework;
using TidyText.Domain.TextEngine.Processors;

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
            var processor = new HtmlStripProcessor(opts);
            var result = processor.Process("Hello <b>World</b>!");
            Assert.That(result, Is.EqualTo("Hello World!"));
        }

        [Test]
        public void StripHtml_RemovesTagsWithAttributes()
        {
            var opts = new HtmlStripProcessorOptions { RemoveHtmlTags = true };
            var processor = new HtmlStripProcessor(opts);
            var result = processor.Process("<a href=\"http://example.com\">Link</a>");
            Assert.That(result, Is.EqualTo("Link"));
        }

        [Test]
        public void StripHtml_RemovesMultilineTags()
        {
            var opts = new HtmlStripProcessorOptions { RemoveHtmlTags = true };
            var processor = new HtmlStripProcessor(opts);
            var result = processor.Process("Hello <div\nclass=\"test\">World</div>!");
            Assert.That(result, Is.EqualTo("Hello World!"));
        }

        [Test]
        public void StripHtml_DoesNotStripIfOptionIsFalse()
        {
            var opts = new HtmlStripProcessorOptions { RemoveHtmlTags = false };
            var processor = new HtmlStripProcessor(opts);
            var result = processor.Process("Hello <b>World</b>!");
            Assert.That(result, Is.EqualTo("Hello <b>World</b>!"));
        }
    }
}

using NUnit.Framework;
using TidyText.Core.TextEngine.Processors;

namespace TidyText.Tests.Model
{
    [TestFixture]
    public class MarkdownProcessorTests
    {
        private readonly MarkdownProcessor _processor = new MarkdownProcessor();
        private readonly MarkdownProcessorOptions _opts = new MarkdownProcessorOptions { StripMarkdown = true };

        [Test]
        public void StripMarkdown_RemovesHeaders()
        {
            Assert.That(_processor.Process("# Header 1", _opts), Is.EqualTo("Header 1"));
            Assert.That(_processor.Process("###### Header 6", _opts), Is.EqualTo("Header 6"));
        }

        [Test]
        public void StripMarkdown_RemovesBoldAndItalic()
        {
            Assert.That(_processor.Process("This is **bold** and __also bold__.", _opts), Is.EqualTo("This is bold and also bold."));
            Assert.That(_processor.Process("This is *italic* and _also italic_.", _opts), Is.EqualTo("This is italic and also italic."));
        }

        [Test]
        public void StripMarkdown_RemovesLinksAndImages()
        {
            Assert.That(_processor.Process("Here is a [link](http://example.com).", _opts), Is.EqualTo("Here is a link."));
            Assert.That(_processor.Process("Here is an ![image alt](http://example.com/img.jpg).", _opts), Is.EqualTo("Here is an image alt."));
        }

        [Test]
        public void StripMarkdown_RemovesMultilineBold()
        {
            var input = "This is **multiline\nbold** text.";
            var expected = "This is multiline\nbold text.";
            Assert.That(_processor.Process(input, _opts), Is.EqualTo(expected));
        }

        [Test]
        public void StripMarkdown_LeavesNormalTextAlone()
        {
            Assert.That(_processor.Process("Just some normal text.", _opts), Is.EqualTo("Just some normal text."));
        }
    }
}

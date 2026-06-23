using NUnit.Framework;
using TidyText.Domain.TextEngine.Processors;

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
            var processor = new MarkdownProcessor(new MarkdownProcessorOptions { StripMarkdown = true });
            Assert.That(processor.Process("# Header 1"), Is.EqualTo("Header 1"));
            Assert.That(processor.Process("###### Header 6"), Is.EqualTo("Header 6"));
        }

        [Test]
        public void StripMarkdown_RemovesBoldAndItalic()
        {
            var processor = new MarkdownProcessor(new MarkdownProcessorOptions { StripMarkdown = true });
            Assert.That(processor.Process("This is **bold** and __also bold__."), Is.EqualTo("This is bold and also bold."));
            Assert.That(processor.Process("This is *italic* and _also italic_."), Is.EqualTo("This is italic and also italic."));
        }

        [Test]
        public void StripMarkdown_RemovesLinksAndImages()
        {
            var processor = new MarkdownProcessor(new MarkdownProcessorOptions { StripMarkdown = true });
            Assert.That(processor.Process("Here is a [link](http://example.com)."), Is.EqualTo("Here is a link."));
            Assert.That(processor.Process("Here is an ![image alt](http://example.com/img.jpg)."), Is.EqualTo("Here is an image alt."));
        }

        [Test]
        public void StripMarkdown_RemovesMultilineBold()
        {
            var processor = new MarkdownProcessor(new MarkdownProcessorOptions { StripMarkdown = true });
            var input = "This is **multiline\nbold** text.";
            var expected = "This is multiline\nbold text.";
            Assert.That(processor.Process(input), Is.EqualTo(expected));
        }

        [Test]
        public void StripMarkdown_LeavesNormalTextAlone()
        {
            var processor = new MarkdownProcessor(new MarkdownProcessorOptions { StripMarkdown = true });
            Assert.That(processor.Process("Just some normal text."), Is.EqualTo("Just some normal text."));
        }
    }
}

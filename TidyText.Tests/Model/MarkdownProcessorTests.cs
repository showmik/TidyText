using NUnit.Framework;
using TidyText.Domain.TextEngine.Processors;

namespace TidyText.Tests.Model
{
    [TestFixture]
    public class MarkdownProcessorTests
    {
        private readonly MarkdownProcessor _enabledProcessor =
            new MarkdownProcessor(new MarkdownProcessorOptions { StripMarkdown = true });

        private readonly MarkdownProcessor _disabledProcessor =
            new MarkdownProcessor(new MarkdownProcessorOptions { StripMarkdown = false });

        private readonly MarkdownProcessor _defaultProcessor =
            new MarkdownProcessor();

        private string Strip(string input) => _enabledProcessor.Process(input);

        #region Option Toggle Behavior

        [Test]
        public void DisabledFlag_ReturnsInputUnchanged()
        {
            var markdown = "# Header with **bold** and [link](http://x.com)";
            Assert.That(_disabledProcessor.Process(markdown), Is.EqualTo(markdown));
        }

        [Test]
        public void DefaultConstructor_ReturnsInputUnchanged()
        {
            var markdown = "## Hello **world**";
            Assert.That(_defaultProcessor.Process(markdown), Is.EqualTo(markdown));
        }

        [Test]
        public void NullOptions_ReturnsInputUnchanged()
        {
            var processor = new MarkdownProcessor(null);
            Assert.That(processor.Process("**bold**"), Is.EqualTo("**bold**"));
        }

        #endregion

        #region Null and Empty Input

        [Test]
        public void NullInput_ReturnsNull()
        {
            Assert.That(Strip(null!), Is.Null);
        }

        [Test]
        public void EmptyString_ReturnsEmpty()
        {
            Assert.That(Strip(""), Is.EqualTo(""));
        }

        [Test]
        public void WhitespaceOnly_ReturnsWhitespace()
        {
            Assert.That(Strip("   "), Is.EqualTo("   "));
        }

        [Test]
        public void PlainText_Untouched()
        {
            const string text = "Just some normal text with no markdown at all.";
            Assert.That(Strip(text), Is.EqualTo(text));
        }

        #endregion

        #region Headers

        [Test]
        public void Headers_AllSixLevels()
        {
            Assert.That(Strip("# H1"), Is.EqualTo("H1"));
            Assert.That(Strip("## H2"), Is.EqualTo("H2"));
            Assert.That(Strip("### H3"), Is.EqualTo("H3"));
            Assert.That(Strip("#### H4"), Is.EqualTo("H4"));
            Assert.That(Strip("##### H5"), Is.EqualTo("H5"));
            Assert.That(Strip("###### H6"), Is.EqualTo("H6"));
        }

        [Test]
        public void Headers_SevenHashes_NotStripped()
        {
            // 7+ hashes is not a valid markdown header
            Assert.That(Strip("####### Not a header"), Is.EqualTo("####### Not a header"));
        }

        [Test]
        public void Headers_NoSpaceAfterHash_NotStripped()
        {
            // Markdown requires space after #
            Assert.That(Strip("#NoSpace"), Is.EqualTo("#NoSpace"));
        }

        [Test]
        public void Headers_MultipleOnSeparateLines()
        {
            var input = "# Title\n## Subtitle\n### Section";
            var expected = "Title\nSubtitle\nSection";
            Assert.That(Strip(input), Is.EqualTo(expected));
        }

        [Test]
        public void Headers_WithTrailingContent()
        {
            Assert.That(Strip("# Header with trailing text"), Is.EqualTo("Header with trailing text"));
        }

        [Test]
        public void Headers_MidLineHashesNotStripped()
        {
            // Hashes in the middle of a line should not be treated as headers
            Assert.That(Strip("This is not # a header"), Is.EqualTo("This is not # a header"));
        }

        [Test]
        public void Headers_WithMultipleSpaces()
        {
            // Multiple spaces after # — regex uses \s+ so this is stripped
            Assert.That(Strip("#  Double spaced header"), Is.EqualTo("Double spaced header"));
        }

        [Test]
        public void Headers_WithTabAfterHash()
        {
            Assert.That(Strip("#\tTabbed header"), Is.EqualTo("Tabbed header"));
        }

        [Test]
        public void Headers_EmptyAfterHash()
        {
            // "# " followed by nothing — just whitespace removed
            Assert.That(Strip("# "), Is.EqualTo(""));
        }

        #endregion

        #region Bold

        [Test]
        public void Bold_DoubleAsterisks()
        {
            Assert.That(Strip("This is **bold** text"), Is.EqualTo("This is bold text"));
        }

        [Test]
        public void Bold_DoubleUnderscores()
        {
            Assert.That(Strip("This is __bold__ text"), Is.EqualTo("This is bold text"));
        }

        [Test]
        public void Bold_MultipleBoldSegments()
        {
            Assert.That(Strip("**one** and **two** and **three**"),
                Is.EqualTo("one and two and three"));
        }

        [Test]
        public void Bold_MultilineContent()
        {
            var input = "**spanning\nmultiple\nlines**";
            var expected = "spanning\nmultiple\nlines";
            Assert.That(Strip(input), Is.EqualTo(expected));
        }

        [Test]
        public void Bold_EmptyContent()
        {
            Assert.That(Strip("before**** after"), Is.EqualTo("before after"));
        }

        [Test]
        public void Bold_SingleAsterisksNotBold()
        {
            // Single asterisk is italic, not bold — result should still strip italic
            Assert.That(Strip("not *bold* here"), Is.EqualTo("not bold here"));
        }

        [Test]
        public void Bold_MixedDelimiters_StrippedByItalicFallthrough()
        {
            // Known limitation: **text__ doesn't match bold (backreference mismatch),
            // but the italic regex (* or _) picks up * and _ individually,
            // consuming the formatting chars and leaving the inner text.
            Assert.That(Strip("**mismatched__ delimiters"), Is.EqualTo("mismatched delimiters"));
        }

        #endregion

        #region Italic

        [Test]
        public void Italic_SingleAsterisks()
        {
            Assert.That(Strip("This is *italic* text"), Is.EqualTo("This is italic text"));
        }

        [Test]
        public void Italic_SingleUnderscores()
        {
            Assert.That(Strip("This is _italic_ text"), Is.EqualTo("This is italic text"));
        }

        [Test]
        public void Italic_MultipleSegments()
        {
            Assert.That(Strip("*one* and *two*"), Is.EqualTo("one and two"));
        }

        [Test]
        public void Italic_MixedDelimiters_NotStripped()
        {
            // *text_ is mismatched
            Assert.That(Strip("*mismatched_ italic"), Is.EqualTo("*mismatched_ italic"));
        }

        #endregion

        #region Bold + Italic Combined

        [Test]
        public void BoldItalic_TripleAsterisks()
        {
            // ***text*** — bold regex matches the outer **, italic matches inner *
            var result = Strip("***bold italic***");
            // After bold strip: the ** wrapping is removed, leaving *bold italic*
            // Then italic strip removes the remaining *...*
            Assert.That(result, Is.EqualTo("bold italic"));
        }

        [Test]
        public void BoldInsideItalic()
        {
            Assert.That(Strip("*italic with **bold** inside*"),
                Is.EqualTo("italic with bold inside"));
        }

        [Test]
        public void ItalicInsideBold()
        {
            Assert.That(Strip("**bold with *italic* inside**"),
                Is.EqualTo("bold with italic inside"));
        }

        #endregion

        #region Strikethrough

        [Test]
        public void Strikethrough_Basic()
        {
            Assert.That(Strip("This is ~~deleted~~ text"), Is.EqualTo("This is deleted text"));
        }

        [Test]
        public void Strikethrough_MultipleSegments()
        {
            Assert.That(Strip("~~one~~ and ~~two~~"), Is.EqualTo("one and two"));
        }

        [Test]
        public void Strikethrough_MultilineContent()
        {
            Assert.That(Strip("~~cross\nline~~"), Is.EqualTo("cross\nline"));
        }

        [Test]
        public void Strikethrough_EmptyContent()
        {
            Assert.That(Strip("before~~~~ after"), Is.EqualTo("before after"));
        }

        [Test]
        public void Strikethrough_SingleTilde_NotStripped()
        {
            Assert.That(Strip("~not strikethrough~"), Is.EqualTo("~not strikethrough~"));
        }

        #endregion

        #region Links

        [Test]
        public void Links_Basic()
        {
            Assert.That(Strip("[click here](https://example.com)"), Is.EqualTo("click here"));
        }

        [Test]
        public void Links_WithSpecialCharsInUrl()
        {
            Assert.That(Strip("[search](https://google.com/search?q=hello&lang=en)"),
                Is.EqualTo("search"));
        }

        [Test]
        public void Links_MultipleOnSameLine()
        {
            Assert.That(Strip("[a](http://a.com) and [b](http://b.com)"),
                Is.EqualTo("a and b"));
        }

        [Test]
        public void Links_WithFormattedLinkText()
        {
            // Bold inside link text
            Assert.That(Strip("[**bold link**](http://example.com)"),
                Is.EqualTo("bold link"));
        }

        [Test]
        public void Links_NestedBracketsInText_KnownLimitation()
        {
            // Known limitation: [text with [brackets]](url) — the regex [^\]]+ stops
            // at the first ], so the outer link pattern never fully matches.
            // The input is returned unchanged.
            var result = Strip("[text with [brackets]](http://example.com)");
            Assert.That(result, Is.EqualTo("[text with [brackets]](http://example.com)"));
        }

        [Test]
        public void Links_EmptyUrl()
        {
            Assert.That(Strip("[text]()"), Is.EqualTo("[text]()"));
            // Regex requires [^\)]+ (one or more), so empty parens won't match
        }

        [Test]
        public void Links_WithTitleAttribute()
        {
            Assert.That(Strip("[text](http://x.com \"title\")"), Is.EqualTo("text"));
        }

        #endregion

        #region Images

        [Test]
        public void Images_Basic()
        {
            Assert.That(Strip("![alt text](http://img.com/photo.png)"), Is.EqualTo("alt text"));
        }

        [Test]
        public void Images_PreservesSurroundingText()
        {
            Assert.That(Strip("Before ![img](http://x.com/i.jpg) after"),
                Is.EqualTo("Before img after"));
        }

        [Test]
        public void Images_EmptyAlt_NotStripped()
        {
            // Regex requires [^\]]+ (one or more chars for alt), empty alt won't match
            Assert.That(Strip("![](http://img.com/photo.png)"),
                Is.EqualTo("![](http://img.com/photo.png)"));
        }

        [Test]
        public void Images_BeforeLinks_OrderMatters()
        {
            // Images must be processed before links so ![alt](url) isn't
            // partially matched as a link [alt](url)
            var input = "![image](http://img.com/a.png) and [link](http://x.com)";
            var expected = "image and link";
            Assert.That(Strip(input), Is.EqualTo(expected));
        }

        #endregion

        #region Blockquotes

        [Test]
        public void Blockquotes_SingleLine()
        {
            Assert.That(Strip("> This is quoted"), Is.EqualTo("This is quoted"));
        }

        [Test]
        public void Blockquotes_MultipleLines()
        {
            var input = "> Line one\n> Line two\n> Line three";
            var expected = "Line one\nLine two\nLine three";
            Assert.That(Strip(input), Is.EqualTo(expected));
        }

        [Test]
        public void Blockquotes_NestedQuotes()
        {
            // >> nested — only one > is stripped per level by regex
            var result = Strip(">> Nested quote");
            // First > + space is stripped, leaving "> Nested quote"... 
            // but wait, the regex is ^>\s+ which matches "> " from ">> N..."
            // Actually: ">> Nested quote" → regex ^>\s+ matches "> " (the first > and the space after >>)
            // No, let's think: ^>\s+ matches ">" followed by one or more spaces.
            // ">>" — the first char is >, then the next char is > which is not \s, so no match.
            // Unless there's a space: ">> " → > then > is not whitespace. No match.
            // So double-nested blockquotes are NOT stripped.
            Assert.That(result, Is.EqualTo(">> Nested quote"));
        }

        [Test]
        public void Blockquotes_NoSpaceAfterAngle_NotStripped()
        {
            // Regex requires \s+ after >
            Assert.That(Strip(">NoSpace"), Is.EqualTo(">NoSpace"));
        }

        [Test]
        public void Blockquotes_MidLine_NotStripped()
        {
            // > must be at start of line
            Assert.That(Strip("text > not a quote"), Is.EqualTo("text > not a quote"));
        }

        [Test]
        public void Blockquotes_WithFormattingInside()
        {
            Assert.That(Strip("> **bold quote**"), Is.EqualTo("bold quote"));
        }

        #endregion

        #region Combined / Real-World Markdown

        [Test]
        public void Combined_HeaderWithBoldAndLink()
        {
            var input = "## **Important**: See [docs](http://docs.com)";
            var expected = "Important: See docs";
            Assert.That(Strip(input), Is.EqualTo(expected));
        }

        [Test]
        public void Combined_FullDocument()
        {
            var input = string.Join("\n", new[]
            {
                "# Project Title",
                "",
                "This is a **description** of the project.",
                "",
                "## Features",
                "",
                "- *Fast* processing",
                "- ~~Legacy~~ Modern API",
                "- [Documentation](http://docs.com)",
                "",
                "> Note: This is important."
            });

            var expected = string.Join("\n", new[]
            {
                "Project Title",
                "",
                "This is a description of the project.",
                "",
                "Features",
                "",
                "- Fast processing",
                "- Legacy Modern API",
                "- Documentation",
                "",
                "Note: This is important."
            });

            Assert.That(Strip(input), Is.EqualTo(expected));
        }

        [Test]
        public void Combined_AllFormattingInOneSentence()
        {
            var input = "Use **bold**, *italic*, ~~strike~~, and [links](http://x.com).";
            var expected = "Use bold, italic, strike, and links.";
            Assert.That(Strip(input), Is.EqualTo(expected));
        }

        [Test]
        public void Combined_BlockquoteWithStrikethroughAndLink()
        {
            var input = "> Check ~~old~~ [new site](http://new.com)";
            var expected = "Check old new site";
            Assert.That(Strip(input), Is.EqualTo(expected));
        }

        #endregion

        #region Edge Cases — Things the processor does NOT strip

        [Test]
        public void InlineCode_NotStripped()
        {
            // Inline code backticks are not handled by this processor
            var input = "Use `printf()` for output";
            Assert.That(Strip(input), Is.EqualTo(input));
        }

        [Test]
        public void CodeBlock_NotStripped()
        {
            var input = "```\ncode block\n```";
            Assert.That(Strip(input), Is.EqualTo(input));
        }

        [Test]
        public void HorizontalRule_DashesNotStripped()
        {
            Assert.That(Strip("---"), Is.EqualTo("---"));
        }

        [Test]
        public void HorizontalRule_Asterisks_KnownLimitation()
        {
            // Known limitation: *** is consumed by the bold regex matching
            // the first ** as a bold delimiter pair with empty content,
            // leaving just the trailing *.
            Assert.That(Strip("***"), Is.EqualTo("*"));
        }

        [Test]
        public void UnorderedListMarkers_NotStripped()
        {
            // List markers (- * +) are not stripped
            var input = "- Item one\n- Item two";
            Assert.That(Strip(input), Is.EqualTo(input));
        }

        [Test]
        public void OrderedListMarkers_NotStripped()
        {
            var input = "1. First\n2. Second";
            Assert.That(Strip(input), Is.EqualTo(input));
        }

        [Test]
        public void Tables_NotStripped()
        {
            var input = "| Col1 | Col2 |\n|------|------|\n| A    | B    |";
            Assert.That(Strip(input), Is.EqualTo(input));
        }

        [Test]
        public void FootnoteLinks_NotStripped()
        {
            // Reference-style links [text][id] are not handled
            var input = "[link text][ref-id]";
            Assert.That(Strip(input), Is.EqualTo(input));
        }

        #endregion

        #region Edge Cases — Adversarial / Tricky Input

        [Test]
        public void AdjacentBoldSegments_NoSpaceBetween()
        {
            Assert.That(Strip("**one****two**"), Is.EqualTo("onetwo"));
        }

        [Test]
        public void UnclosedBold_StrippedByItalicFallthrough()
        {
            // Known limitation: ** is not matched by bold regex (no closing **),
            // but italic regex matches the two * chars individually as a pair
            // wrapping empty content, leaving the text without asterisks.
            Assert.That(Strip("**unclosed bold"), Is.EqualTo("unclosed bold"));
        }

        [Test]
        public void UnclosedItalic_NotStripped()
        {
            Assert.That(Strip("*unclosed italic"), Is.EqualTo("*unclosed italic"));
        }

        [Test]
        public void UnclosedStrikethrough_NotStripped()
        {
            Assert.That(Strip("~~unclosed strike"), Is.EqualTo("~~unclosed strike"));
        }

        [Test]
        public void UnclosedLink_NotStripped()
        {
            Assert.That(Strip("[unclosed link(http://x.com)"), Is.EqualTo("[unclosed link(http://x.com)"));
        }

        [Test]
        public void OnlyMarkdownChars_Asterisks_KnownLimitation()
        {
            // Known limitation: "* * *" — italic regex matches * (space) * as
            // a valid italic span (content = " "), consuming two asterisks
            // and leaving "  *" (two spaces + trailing asterisk).
            Assert.That(Strip("* * *"), Is.EqualTo("  *"));
        }

        [Test]
        public void EscapedCharacters_KnownLimitation()
        {
            // Known limitation: the processor does NOT handle backslash escapes.
            // \*text\* — the italic regex sees * and * as delimiters, stripping them.
            var input = @"This is \*not italic\*";
            Assert.That(Strip(input), Is.EqualTo("This is \\not italic\\"));
        }

        [Test]
        public void VeryLongInput_DoesNotHang()
        {
            // Regex catastrophic backtracking test
            var input = new string('*', 100) + "text" + new string('*', 100);
            // Should complete without timeout — we just verify it returns
            Assert.DoesNotThrow(() => Strip(input));
        }

        [Test]
        public void UnicodeContent_InBold()
        {
            Assert.That(Strip("**日本語テキスト**"), Is.EqualTo("日本語テキスト"));
        }

        [Test]
        public void UnicodeContent_InLink()
        {
            Assert.That(Strip("[日本語](http://example.com)"), Is.EqualTo("日本語"));
        }

        [Test]
        public void Emoji_InBold()
        {
            Assert.That(Strip("**🚀 Launch**"), Is.EqualTo("🚀 Launch"));
        }

        [Test]
        public void WindowsLineEndings_Headers()
        {
            var input = "# Title\r\n## Subtitle\r\nPlain text";
            var expected = "Title\r\nSubtitle\r\nPlain text";
            Assert.That(Strip(input), Is.EqualTo(expected));
        }

        [Test]
        public void WindowsLineEndings_Blockquotes()
        {
            var input = "> Quote one\r\n> Quote two";
            var expected = "Quote one\r\nQuote two";
            Assert.That(Strip(input), Is.EqualTo(expected));
        }

        [Test]
        public void MultipleFormattingOnSameBoundary()
        {
            // Bold immediately followed by italic
            Assert.That(Strip("**bold***italic*"), Is.EqualTo("bolditalic"));
        }

        [Test]
        public void SingleCharBold()
        {
            Assert.That(Strip("**x**"), Is.EqualTo("x"));
        }

        [Test]
        public void SingleCharItalic()
        {
            Assert.That(Strip("*x*"), Is.EqualTo("x"));
        }

        [Test]
        public void SingleCharStrikethrough()
        {
            Assert.That(Strip("~~x~~"), Is.EqualTo("x"));
        }

        [Test]
        public void LinkWithParensInUrl()
        {
            // Wikipedia-style URLs with parens: [text](http://en.wikipedia.org/wiki/Thing_(concept))
            // The regex [^\)]+ stops at the first ), so this will break
            var result = Strip("[Thing](http://en.wikipedia.org/wiki/Thing_(concept))");
            // Known limitation: the regex can't handle nested parens in URLs
            Assert.That(result, Does.Not.Contain("[Thing]"));
        }

        [Test]
        public void ConsecutiveHeaders()
        {
            var input = "# One\n# Two\n# Three";
            var expected = "One\nTwo\nThree";
            Assert.That(Strip(input), Is.EqualTo(expected));
        }

        [Test]
        public void HeaderFollowedByBlockquote()
        {
            var input = "# Title\n> Summary of the title";
            var expected = "Title\nSummary of the title";
            Assert.That(Strip(input), Is.EqualTo(expected));
        }

        #endregion

        #region Pipeline Integration

        [Test]
        public void Pipeline_MarkdownStripperActivatesWithOption()
        {
            var builder = new TidyText.Domain.TextEngine.TextPipelineBuilder();
            var pipeline = builder.AddMarkdownStripper().Build();

            Assert.That(pipeline.Process("# Hello **world**"), Is.EqualTo("Hello world"));
        }

        [Test]
        public void Pipeline_ConditionalSkip()
        {
            var builder = new TidyText.Domain.TextEngine.TextPipelineBuilder();
            var pipeline = builder
                .If(false, b => b.AddMarkdownStripper())
                .Build();

            // No processors added, input passes through
            Assert.That(pipeline.Process("# Hello **world**"), Is.EqualTo("# Hello **world**"));
        }

        [Test]
        public void Pipeline_ConditionalAdd()
        {
            var builder = new TidyText.Domain.TextEngine.TextPipelineBuilder();
            var pipeline = builder
                .If(true, b => b.AddMarkdownStripper())
                .Build();

            Assert.That(pipeline.Process("## **Title**"), Is.EqualTo("Title"));
        }

        #endregion
    }
}

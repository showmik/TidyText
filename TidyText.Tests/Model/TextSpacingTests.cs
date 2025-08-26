using NUnit.Framework;
using TidyText.Model;

namespace TidyText.Tests.Model
{
    [TestFixture]
    public class TextSpacingTests
    {
        private static string Fix(string s, bool treatColon = false) => TextSpacing.FixPunctuationSpacing(s, treatColon);

        // --- Core punctuation spacing --------------------------------------------------------

        [TestCase("Hello,world!", "Hello, world!")]
        [TestCase("Hi .There", "Hi. There")]
        [TestCase("Edge!Case?OK", "Edge! Case? OK")]
        [TestCase("Quotes.\"Yes\"", "Quotes.\"Yes\"")]
        [TestCase("Before ;after", "Before; after")]
        public void Basic_Punctuation_Rules(string input, string expected)
        {
            Assert.That(Fix(input), Is.EqualTo(expected));
        }

        // --- URL protection + sentence boundary (digit '.' letter) ---------------------------

        [Test]
        public void Url_Then_Period_Then_Letter_Gets_Space()
        {
            var s = "Visit https://ex.com/a?x=1,2&y=3.Please";
            var got = Fix(s);
            Assert.That(got, Is.EqualTo("Visit https://ex.com/a?x=1,2&y=3. Please"));
        }

        [Test]
        public void Url_With_Trailing_Period_And_Paren_Keeps_Paren_Trims_Period()
        {
            var s = "See (https://example.com).Thanks";
            var got = Fix(s);
            Assert.That(got, Is.EqualTo("See (https://example.com). Thanks"));
        }

        [Test]
        public void Www_Urls_Are_Protected_Too()
        {
            var s = "Start www.example.org,then go.";
            var got = Fix(s);
            Assert.That(got, Is.EqualTo("Start www.example.org, then go."));
        }

        // --- Email / domain / version / decimals --------------------------------------------

        [Test]
        public void Email_And_Time_Are_Not_Broken()
        {
            var s = "Email me: a.b-c+d@foo.bar,before 10:30am.";
            var got = Fix(s);
            Assert.That(got, Is.EqualTo("Email me: a.b-c+d@foo.bar, before 10:30am."));
        }

        [Test]
        public void Versions_And_Decimals_Stay_Intact()
        {
            var s = "v1.2.3,and 1,234.56";
            var got = Fix(s);
            Assert.That(got, Is.EqualTo("v1.2.3, and 1,234.56"));
        }

        [Test]
        public void Domains_Are_Not_Split()
        {
            var s = "Visit example.co.uk,now";
            var got = Fix(s);
            Assert.That(got, Is.EqualTo("Visit example.co.uk, now"));
        }

        // --- Times / ratios / colon behavior -------------------------------------------------

        [Test]
        public void Ratios_And_Times_Are_Protected_By_Default()
        {
            Assert.That(Fix("Ratio 4:3 is fine"), Is.EqualTo("Ratio 4:3 is fine"));
            Assert.That(Fix("At 10:30 we meet,ok"), Is.EqualTo("At 10:30 we meet, ok"));
        }

        [Test]
        public void TreatColonAsSentencePunct_Does_Not_Break_Times()
        {
            var s = "Meet at 09:15am.Ok";
            var got = Fix(s, treatColon: true);
            Assert.That(got, Is.EqualTo("Meet at 09:15am. Ok"),
                "Colon rule must not interfere with valid times even when enabled.");
        }

        // --- Ellipsis handling ---------------------------------------------------------------

        [TestCase("Pi≈3.1415...OK?", "Pi≈3.1415... OK?")]
        [TestCase("Wait…OK!", "Wait… OK!")]
        [TestCase("Four....Dots", "Four.... Dots")]
        public void Ellipsis_Gets_Space_After(string input, string expected)
        {
            Assert.That(Fix(input), Is.EqualTo(expected));
        }

        // --- Code spans (inline and fenced) -------------------------------------------------

        [Test]
        public void Inline_Code_Is_Preserved_But_Outside_Punct_Is_Normalized()
        {
            var s = "Use `code()` ,please";
            var got = Fix(s);
            Assert.That(got, Is.EqualTo("Use `code()`, please"),
                "Everything inside backticks unchanged; space before ',' removed; one space after ','.");
        }

        [Test]
        public void Triple_Backtick_Blocks_Are_Preserved()
        {
            var s = "Start.\n```\na=1,b=2\nc.d()\n```\nThen,go.";
            var got = Fix(s);
            Assert.That(got, Is.EqualTo("Start.\n```\na=1,b=2\nc.d()\n```\nThen, go."));
        }

        // --- Paths (Windows & Unix) ---------------------------------------------------------

        [Test]
        public void File_Paths_Are_Not_Disturbed()
        {
            Assert.That(Fix(@"Open C:\Temp\file.txt,ok"), Is.EqualTo(@"Open C:\Temp\file.txt, ok"));
            Assert.That(Fix("/usr/bin/env,ok"), Is.EqualTo("/usr/bin/env, ok"));
        }

        // --- Dotted abbreviations (e.g., i.e., U.S.A.) --------------------------------------

        [TestCase("Use e.g.,this", "Use e.g., this")]
        [TestCase("See i.e.,that", "See i.e., that")]
        [TestCase("U.S.A.,rocks", "U.S.A., rocks")]
        public void Dotted_Abbreviations_Are_Protected(string input, string expected)
        {
            Assert.That(Fix(input), Is.EqualTo(expected));
        }

        // --- Newlines unaffected & spaces collapsed only between tokens ---------------------

        [Test]
        public void Newlines_Are_Preserved_Spaces_Are_Not_Doubled()
        {
            var s = "Line1.  \nLine2!  OK";
            var got = Fix(s);
            Assert.That(got, Is.EqualTo("Line1.\nLine2! OK"));
            Assert.That(!got.Contains("  "));
        }

        // --- Idempotence ---------------------------------------------------------------------

        [Test]
        public void Running_Twice_Is_Idempotent()
        {
            var once = Fix("Visit https://ex.com/a?x=1,2&y=3.Please  Now…OK?");
            var twice = Fix(once);
            Assert.That(twice, Is.EqualTo(once));
        }

        // ===================== Aphostrophe and Contraction =====================
        [Test]
        public void Contractions_Stay_Tight() => Assert.That(Fix("It’s fine"), Is.EqualTo("It’s fine"));

        [Test]
        public void No_Space_Before_Closing_Straight_Quote() => Assert.That(Fix("\"Single mega paragraph,\""), Is.EqualTo("\"Single mega paragraph,\""));

        [Test]
        public void Quote_Comma_Then_Text_And_Contraction()
        {
            Assert.That(Fix("\"very good test text,\" I’ll include"),
            Is.EqualTo("\"very good test text,\" I’ll include"));
        }

        // ===================== COLON POLICY (narrative vs technical) =====================

        [Test]
        public void Colon_Narrative_Spaces_When_Enabled()
        {
            // When treatColon is ON, enforce “one space after” and “no space before”
            var got = Fix("Note:Important. Key : Value. Title:\"Quote\"", treatColon: true);
            // Decide policy for the quote case:
            // If you WANT a space before the opening quote (common in English): expect Title: "Quote"
            // If you prefer no space (mirror period behavior): change the expected accordingly.
            Assert.That(got, Is.EqualTo("Note: Important. Key: Value. Title: \"Quote\""));
        }

        [Test]
        public void Colon_Narrative_No_Changes_When_Disabled()
        {
            // With treatColon OFF, narrative colons remain untouched
            var got = Fix("Note:Important. Key : Value. Title:\"Quote\"", treatColon: false);
            Assert.That(got, Is.EqualTo("Note:Important. Key : Value. Title:\"Quote\""));
        }

        [Test]
        public void Colon_Before_Newline_Should_Not_Insert_Trailing_Space()
        {
            var got = Fix("Title:\nNext line", treatColon: true);
            Assert.That(got, Is.EqualTo("Title:\nNext line"));
        }

        [Test]
        public void Colon_After_Closers_Behaves_Like_Narrative_When_Enabled()
        {
            var got = Fix("See (ref):next", treatColon: true);
            Assert.That(got, Is.EqualTo("See (ref): next"));
        }

        // ===================== COLON: TECHNICAL FORMS MUST SURVIVE =====================

        [Test]
        public void Times_And_Ratios_Survive_Even_When_Colon_Enabled()
        {
            Assert.That(Fix("At 10:30 we meet.Ok", true), Is.EqualTo("At 10:30 we meet. Ok"));
            Assert.That(Fix("HMS 10:30:05.Ok", true), Is.EqualTo("HMS 10:30:05. Ok"));
            Assert.That(Fix("Ratio 16:9 looks fine.Ok", true), Is.EqualTo("Ratio 16:9 looks fine. Ok"));
        }

        [Test]
        public void Iso8601_Timestamps_Survive()
        {
            var got = Fix("Stamp 2025-08-26T10:05:01Z.Ok", treatColon: true);
            Assert.That(got, Is.EqualTo("Stamp 2025-08-26T10:05:01Z. Ok"));
        }

        // This one might FAIL with treatColon:true depending on your current guard.
        // If it does, consider adding an IPv6 protector or extending the colon guard to hex patterns.
        [Test]
        public void IPv6_Addresses_Should_Not_Get_Spaced()
        {
            var got = Fix("Ping 2001:db8::1.Ok", treatColon: true);
            Assert.That(got, Is.EqualTo("Ping 2001:db8::1. Ok"));
        }

        // ===================== URL TRAIL VARIANTS =====================

        [Test]
        public void Url_Comma_Then_Quote_Should_Space_After_Comma()
        {
            var got = Fix("Visit www.example.org,\"then\" now.");
            // If this fails, add an early-stop in ScanUrlLikeEnd for comma + opening quote.
            Assert.That(got, Is.EqualTo("Visit www.example.org, \"then\" now."));
        }

        [Test]
        public void Url_Semicolon_Then_Letter_Should_Be_Treated_As_Prose()
        {
            var got = Fix("Go to www.example.org;then press enter.");
            // If this fails, add an early-stop for semicolon + letter.
            Assert.That(got, Is.EqualTo("Go to www.example.org; then press enter."));
        }

        [Test]
        public void Url_Semicolon_Then_Quote_Should_Space()
        {
            var got = Fix("Go to www.example.org;\"then\" now.");
            Assert.That(got, Is.EqualTo("Go to www.example.org; \"then\" now."));
        }

        // ===================== QUOTES & CLOSERS AROUND SENTENCE PUNCT =====================

        [Test]
        public void Period_Then_Closing_Quote_Then_Letter_Gets_Space()
        {
            var got = Fix("He ended.”Yes”OK");
            Assert.That(got, Is.EqualTo("He ended.” Yes” OK"));
        }

        [Test]
        public void Closer_Paren_Then_Period_Then_Letter_Gets_Space()
        {
            var got = Fix("Done.).Next");
            Assert.That(got, Is.EqualTo("Done.). Next"));
        }

        // ===================== DASHES & EM–EN PUNCT (should be left alone) =====================

        [Test]
        public void EmDash_And_EnDash_Are_Not_Touched()
        {
            Assert.That(Fix("Wait—this,is fine"), Is.EqualTo("Wait—this, is fine"));
            Assert.That(Fix("Range 1–10,is fine"), Is.EqualTo("Range 1–10, is fine"));
        }

        // ===================== PATH EDGE CASES =====================

        [Test]
        public void Windows_Path_With_Parens_And_Comma()
        {
            var got = Fix(@"Check C:\Program Files (x86)\App,ok");
            Assert.That(got, Is.EqualTo(@"Check C:\Program Files (x86)\App, ok"));
        }

        [Test]
        public void Quoted_Path_With_Commas_Remains_Intact()
        {
            var got = Fix("Open `C:\\Temp\\file,name.txt`,please");
            Assert.That(got, Is.EqualTo("Open `C:\\Temp\\file,name.txt`, please"));
        }

        // ===================== URL + PORT =====================

        [Test]
        public void Url_With_Port_Comma_Then_Letter_Spaces()
        {
            var got = Fix("Hit http://example.com:8080,then go.");
            Assert.That(got, Is.EqualTo("Hit http://example.com:8080, then go."));
        }

        // Bracketed IPv6 host with port (will likely need a tiny URL-host extension)
        [Test]
        public void Bracketed_IPv6_Url_With_Port_Spaces_After_Period()
        {
            var got = Fix("Browse http://[2001:db8::1]:443.OK");
            Assert.That(got, Is.EqualTo("Browse http://[2001:db8::1]:443. OK"));
        }

        // ===================== MAILTO / FTP =====================

        [Test]
        public void Mailto_Then_Comma_Treated_As_Prose()
        {
            var got = Fix("Mail me at mailto:john.doe@example.com,thanks");
            Assert.That(got, Is.EqualTo("Mail me at mailto:john.doe@example.com, thanks"));
        }

        [Test]
        public void Ftp_Url_Semicolon_Then_Letter_Spaces()
        {
            var got = Fix("Fetch ftp://host/path;then process");
            Assert.That(got, Is.EqualTo("Fetch ftp://host/path; then process"));
        }

        // ===================== BARE DOMAIN WITH UPPERCASE TLD =====================

        [Test]
        public void Bare_Uppercase_Tld_Domain_Does_Not_Block_Spacing()
        {
            var got = Fix("Visit EXAMPLE.COM,please");
            Assert.That(got, Is.EqualTo("Visit EXAMPLE.COM, please"));
        }

        // ===================== AM/PM VARIANTS =====================

        [Test]
        public void AM_PM_Dotted_Variant_With_Comma()
        {
            var got = Fix("Meet 10:30 a.m.,ok", treatColon: true);
            Assert.That(got, Is.EqualTo("Meet 10:30 a.m., ok"));
        }

        [Test]
        public void AM_PM_Compact_Variant_With_Comma()
        {
            var got = Fix("Meet 10:30AM,ok", treatColon: true);
            Assert.That(got, Is.EqualTo("Meet 10:30AM, ok"));
        }

        // ===================== ISO-8601 WITH OFFSET =====================

        [Test]
        public void Iso8601_With_Offset_Survives()
        {
            var got = Fix("Stamp 2025-08-26T10:05:01+06:00.Ok", treatColon: true);
            Assert.That(got, Is.EqualTo("Stamp 2025-08-26T10:05:01+06:00. Ok"));
        }

        // ===================== IPv6 + IPv4 MIXED =====================

        [Test]
        public void IPv6_Embedded_IPv4_Survives()
        {
            var got = Fix("Test ::ffff:192.0.2.128.Ok", treatColon: true);
            Assert.That(got, Is.EqualTo("Test ::ffff:192.0.2.128. Ok"));
        }

        // ===================== EMOJI & GRAPHEME BOUNDARIES =====================

        [Test]
        public void Emoji_After_Period_Gets_Space()
        {
            var got = Fix("Done.✅OK");
            Assert.That(got, Is.EqualTo("Done. ✅OK"));
        }

        // ===================== FULL-WIDTH / INTERNATIONAL PUNCT =====================

        [Test]
        public void Cjk_Fullwidth_Period_Behavior()
        {
            var got = Fix("你好。好的");
            Assert.That(got, Is.EqualTo("你好。 好的"));
            Assert.That(Fix("你好！好的"), Is.EqualTo("你好！ 好的"));
            Assert.That(Fix("你好？好的"), Is.EqualTo("你好？ 好的"));
        }

        // ===================== NBSP & TABS =====================

        [Test]
        public void Nbsp_Is_Preserved()
        {
            var nbsp = '\u00A0';
            var got = Fix("Hello," + nbsp + "world!");
            Assert.That(got, Is.EqualTo("Hello," + nbsp + "world!"));
        }

        // ===================== BACKTICK EDGE: UNCLOSED INLINE =====================

        [Test]
        public void Unclosed_Inline_Code_At_EOT_Is_Preserved()
        {
            var got = Fix("Use `code();OK");
            Assert.That(got, Is.EqualTo("Use `code();OK"));
        }

        // ===================== DECIMAL FOLLOWED BY SENTENCE =====================

        [Test]
        public void Decimal_Followed_By_Sentence_Does_Space()
        {
            var got = Fix("value=3.14.Go");
            Assert.That(got, Is.EqualTo("value=3.14. Go"));
        }

        // ===================== NESTED CLOSERS =====================

        [Test]
        public void Nested_Closers_Before_Letter_Gets_Space()
        {
            var got = Fix("Done.”)OK");
            Assert.That(got, Is.EqualTo("Done.”) OK"));
        }

        // ===================== URL + PORT (WWW VARIANT WITH SEMICOLON) =====================

        [Test]
        public void Www_With_Portlike_Trailer_Semicolon_Then_Letter()
        {
            var got = Fix("Go to www.example.org;then press enter.");
            Assert.That(got, Is.EqualTo("Go to www.example.org; then press enter."));
        }

    }
}

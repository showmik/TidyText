using NUnit.Framework;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows; // WPF Application
using TidyText.ViewModel;

namespace TidyText.Tests.ViewModel
{
    [TestFixture]
    [Apartment(ApartmentState.STA)] // WPF & Clipboard safety
    public class MainViewModelTests
    {
        [SetUp]
        public void EnsureWpfApp()
        {
            if (Application.Current == null)
                new Application();
        }

        private static MainViewModel NewVm() => new MainViewModel
        {
            ShouldTrim = false,
            ShouldTrimStart = false,
            ShouldTrimEnd = false,
            ShouldRemoveMultipleSpaces = false,
            ShouldRemoveMultipleLines = false,
            ShouldRemoveAllLines = false,
            ShouldFixPunctuationSpace = false,
            IsUppercase = false,
            IsLowercase = false,
            IsSentenceCase = false,
            IsCapitalizeEachWord = false,
            WrapLines = false
        };

        [Test]
        public void Counters_EmptyString()
        {
            var vm = NewVm();
            vm.MainText = "";
            Assert.That(vm.WordCount, Is.EqualTo(0));
            Assert.That(vm.SentenceCount, Is.EqualTo(0));
            Assert.That(vm.ParagraphCount, Is.EqualTo(0));
            Assert.That(vm.LineCount, Is.EqualTo(0));
            Assert.That(vm.CharacterCount, Is.EqualTo(0));
        }

        [Test]
        public void Counters_WhitespaceOnly()
        {
            var vm = NewVm();
            vm.MainText = "   \n\t  ";
            Assert.That(vm.WordCount, Is.EqualTo(0));
            Assert.That(vm.SentenceCount, Is.EqualTo(0));
            Assert.That(vm.ParagraphCount, Is.EqualTo(0));
            Assert.That(vm.LineCount, Is.EqualTo(2)); // 1 line break = 2 lines
            Assert.That(vm.CharacterCount, Is.EqualTo(7));
        }

        [Test]
        public void Counters_SingleLine_NoLineBreaks()
        {
            var vm = NewVm();
            vm.MainText = "This is a test";
            Assert.That(vm.WordCount, Is.EqualTo(4));
            Assert.That(vm.SentenceCount, Is.EqualTo(1));
            Assert.That(vm.ParagraphCount, Is.EqualTo(1));
            Assert.That(vm.LineCount, Is.EqualTo(1));
            Assert.That(vm.CharacterCount, Is.EqualTo(14));
        }

        [Test]
        public void Counters_SingleWord_NoPunctuation()
        {
            var vm = NewVm();
            vm.MainText = "Word";
            Assert.That(vm.WordCount, Is.EqualTo(1));
            Assert.That(vm.SentenceCount, Is.EqualTo(1));
            Assert.That(vm.ParagraphCount, Is.EqualTo(1));
            Assert.That(vm.LineCount, Is.EqualTo(1));
            Assert.That(vm.CharacterCount, Is.EqualTo(4));
        }

        [Test]
        public void Counters_LargeInput()
        {
            var vm = NewVm();
            var text = string.Join("\n", Enumerable.Repeat("word1 word2. word3!", 1000));
            vm.MainText = text;
            Assert.That(vm.WordCount, Is.EqualTo(3000));
            Assert.That(vm.SentenceCount, Is.GreaterThan(0));
            Assert.That(vm.ParagraphCount, Is.EqualTo(1000));
            Assert.That(vm.LineCount, Is.EqualTo(1000));
            Assert.That(vm.CharacterCount, Is.EqualTo(text.Length));
        }

        [Test]
        public void Constructor_Defaults_Are_Safe()
        {
            var vm = NewVm();
            Assert.That(vm.IsDoNotChange, Is.True);
            Assert.That(vm.MainText, Is.Null.Or.EqualTo(string.Empty));
            Assert.That(vm.WordCount, Is.EqualTo(0));
            Assert.That(vm.CharacterCount, Is.EqualTo(0));
            Assert.That(vm.SentenceCount, Is.EqualTo(0));
            Assert.That(vm.ParagraphCount, Is.EqualTo(0));
            Assert.That(vm.LineCount, Is.EqualTo(0));
        }

        [Test]
        public void Counters_Update_On_MainText_Set()
        {
            var vm = NewVm();
            vm.MainText = "Hello world!\n\nNew para.";
            Assert.That(vm.WordCount, Is.EqualTo(4));
            Assert.That(vm.SentenceCount, Is.EqualTo(2));
            Assert.That(vm.ParagraphCount, Is.EqualTo(2));
            Assert.That(vm.LineCount, Is.EqualTo(3));
            Assert.That(vm.CharacterCount, Is.EqualTo("Hello world!\n\nNew para.".Length));
        }

        [Test]
        public void Clean_TrimStart_PerLine()
        {
            var vm = NewVm();
            vm.MainText = "  a\n   b  \n c";
            vm.ShouldTrimStart = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("a\nb  \nc"));
        }

        [Test]
        public void Clean_TrimEnd_PerLine()
        {
            var vm = NewVm();
            vm.MainText = "a  \n b \n c   ";
            vm.ShouldTrimEnd = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("a\n b\n c"));
        }

        [Test]
        public void Clean_Collapse_IntraLine_Whitespace()
        {
            var vm = NewVm();
            vm.MainText = "a   b\t\tc\nx    y";
            vm.ShouldRemoveMultipleSpaces = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("a b c\nx y"));
        }

        [Test]
        public void Clean_Multiple_Blank_Lines_To_Single_Blank_Line()
        {
            var vm = NewVm();
            vm.MainText = "a\n\n\n\nb";
            vm.ShouldRemoveMultipleLines = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("a\n\nb"));
        }

        [Test]
        public void Clean_Unwrap_All_Lines_To_Spaces()
        {
            var vm = NewVm();
            vm.MainText = "a \n  b\t\n\nc";
            vm.ShouldRemoveAllLines = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("a b c"));
        }

        [Test]
        public void Clean_Unwrap_All_Then_Collapse_Spaces()
        {
            var vm = NewVm();
            vm.MainText = "a \n   b\t\tc\n\n   d";
            vm.ShouldRemoveAllLines = true;
            vm.ShouldRemoveMultipleSpaces = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("a b c d"));
        }

        [Test]
        public void Clean_PunctuationFix_Key_Edges()
        {
            var vm = NewVm();
            vm.ShouldFixPunctuationSpace = true;

            vm.MainText = "Visit https://ex.com/a?x=1,2&y=3.Please";
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("Visit https://ex.com/a?x=1,2&y=3. Please"));

            vm.MainText = "Email me: a.b-c+d@foo.bar,before 10:30am.";
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("Email me: a.b-c+d@foo.bar, before 10:30am."));

            vm.MainText = "Pi≈3.1415...OK?";
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("Pi≈3.1415... OK?"));

            vm.MainText = "v1.2.3,and 1,234.56";
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("v1.2.3, and 1,234.56"));

            vm.MainText = "Ratio 4:3 is fine";
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("Ratio 4:3 is fine"));

            vm.MainText = "Use `code()` ,please";
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("Use `code()`, please"));
        }

        [Test]
        public void Clean_Uppercase_Applied_Last()
        {
            var vm = NewVm();
            vm.MainText = " a ,b  ";
            vm.ShouldTrim = true;
            vm.ShouldFixPunctuationSpace = true;
            vm.IsUppercase = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("A, B"));
        }

        [Test]
        public void Clean_Lowercase_Applied_Last()
        {
            var vm = NewVm();
            vm.MainText = "  Hello WORLD!  ";
            vm.ShouldTrim = true;
            vm.IsLowercase = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("hello world!"));
        }

        [TestCase("Met Dr. Smith at 5 p.m. today. great chat.", "Met Dr. Smith at 5 p.m. today. Great chat.")]
        [TestCase("Meet me at 10 A.M. tomorrow. thanks.", "Meet me at 10 A.M. tomorrow. Thanks.")]
        [TestCase("We live in the U.S. today. all good.", "We live in the U.S. today. All good.")]
        [TestCase("We are in the U.S.A. now. ok.", "We are in the U.S.A. now. Ok.")]
        [TestCase("It works, e.g. like this. cool.", "It works, e.g. like this. Cool.")]
        [TestCase("Value is 3.14 meters. next.", "Value is 3.14 meters. Next.")]
        [TestCase("Wait... is this real? yes.", "Wait... Is this real? Yes.")]
        [TestCase("\"hello\", he said. 'okay' she replied. fine.", "\"Hello\", he said. 'Okay' she replied. Fine.")]
        [TestCase("(greetings) are fun. indeed.", "(Greetings) are fun. Indeed.")]
        [TestCase("first line\nsecond line. third line", "First line\nSecond line. Third line")]
        [TestCase("para one. end.\n\nsecond para starts lower. fine.", "Para one. End.\n\nSecond para starts lower. Fine.")]
        [TestCase("i think it's fine. it's okay.", "I think it's fine. It's okay.")]
        [TestCase("state-of-the-art devices are here. welcome.", "State-of-the-art devices are here. Welcome.")]
        // Sentence case should not alter spacing when only IsSentenceCase is true:
        [TestCase(" hello ,world !  ok ", " Hello ,world !  Ok ")]
        public void Clean_SentenceCase_Robust_Scenarios(string input, string expected)
        {
            var vm = NewVm();
            vm.MainText = input;
            vm.IsSentenceCase = true; // only casing, no spacing/trim changes
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo(expected));
        }

        [TestCase("we love iPhone 15. nice.", "We love iPhone 15. Nice.")]
        [TestCase("watch YouTube today. ok.", "Watch YouTube today. Ok.")]
        [TestCase("we use OpenAI models. cool.", "We use OpenAI models. Cool.")]
        [TestCase("order at McDonald’s now. thanks.", "Order at McDonald’s now. Thanks.")]
        [TestCase("we test iOS and macOS now. ok.", "We test iOS and macOS now. Ok.")]
        [TestCase("NASA announced new SLS rocket. big news.", "NASA announced new SLS rocket. Big news.")]
        [TestCase("Meet me at 10 a.m. tomorrow. thanks.", "Meet me at 10 a.m. tomorrow. Thanks.")]
        [TestCase("Value is 3.14 meters. next.", "Value is 3.14 meters. Next.")]
        [TestCase("\"hello\", he said. 'okay' she replied. fine.", "\"Hello\", he said. 'Okay' she replied. Fine.")]
        [TestCase("(greetings) are fun. indeed.", "(Greetings) are fun. Indeed.")]
        public void Clean_SentenceCase_Preserves_CamelAndAbbrev(string input, string expected)
        {
            var vm = NewVm();
            vm.MainText = input;
            vm.IsSentenceCase = true;   // no spacing/trim toggles
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo(expected));
        }

        [Test]
        public void Clean_SentenceCase_Normalizes_Alternating_Case()
        {
            var vm = NewVm();
            vm.MainText = "after that, tEsT again. ok.";
            vm.IsSentenceCase = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("After that, test again. Ok."));
        }

        [Test]
        public void Clean_SentenceCase_Applied_Last_CamelSafe()
        {
            var vm = NewVm();
            vm.MainText = "tHis IS. a tEsT.\nnew line";
            vm.IsSentenceCase = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("This is. A test.\nNew line"));
        }

        [Test]
        public void Clean_TitleCase_Uses_EnUs_Applied_Last()
        {
            var prev = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                var vm = NewVm();
                vm.MainText = "hello from the world of text";
                vm.IsCapitalizeEachWord = true;
                vm.Clean();
                Assert.That(vm.MainText, Is.EqualTo("Hello From the World of Text"));
            }
            finally { Thread.CurrentThread.CurrentCulture = prev; }
        }

        [Test]
        public void Undo_Restores_Previous_Text()
        {
            var vm = NewVm();
            vm.MainText = "a   b";
            vm.ShouldRemoveMultipleSpaces = true;
            vm.Clean();   // "a b"
            Assert.That(vm.MainText, Is.EqualTo("a b"));
            vm.Undo();    // back
            Assert.That(vm.MainText, Is.EqualTo("a   b"));
        }

        [Test]
        public void Undo_After_Multiple_Cleans()
        {
            var vm = NewVm();
            vm.MainText = "  a   b  \n c";
            vm.ShouldTrim = true;
            vm.Clean(); // "a   b\nc"
            vm.ShouldRemoveMultipleSpaces = true;
            vm.Clean(); // "a b\nc"
            vm.Undo();  // -> "a   b\nc"
            Assert.That(vm.MainText, Is.EqualTo("a   b\nc"));
            vm.Undo();  // -> original
            Assert.That(vm.MainText, Is.EqualTo("  a   b  \n c"));
        }
    }
}

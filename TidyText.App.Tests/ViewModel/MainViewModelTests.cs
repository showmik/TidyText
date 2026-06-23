using NUnit.Framework;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows; // WPF Application
using TidyText.App.ViewModels;
using TidyText.Domain.Services;
using TidyText.Domain.TextEngine;
using TidyText.Domain.TextEngine.Processors;

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

        private class StubClipboardService : IClipboardService
        {
            public string? LastText { get; private set; }
            public void SetText(string text) => LastText = text;
        }

        private static MainViewModel NewVm()
        {
            var vm = new MainViewModel(
                new StubClipboardService(),
                new UndoRedoService(),
                CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default,
                null! // Mock AIAssistantVM
            );

            vm.Options.ShouldTrim = false;
            vm.Options.ShouldTrimStart = false;
            vm.Options.ShouldTrimEnd = false;
            vm.Options.ShouldRemoveMultipleSpaces = false;
            vm.Options.ShouldRemoveMultipleLines = false;
            vm.Options.ShouldRemoveAllLines = false;
            vm.Options.ShouldFixPunctuationSpace = false;
            vm.Options.CasingStyle = CasingStyle.DoNotChange;
            vm.WrapLines = false;

            return vm;
        }

        [Test]
        public void Counters_EmptyString()
        {
            var vm = NewVm();
            vm.MainText = "";
            Assert.That(vm.Statistics.WordCount, Is.EqualTo(0));
            Assert.That(vm.Statistics.SentenceCount, Is.EqualTo(0));
            Assert.That(vm.Statistics.ParagraphCount, Is.EqualTo(0));
            Assert.That(vm.Statistics.LineCount, Is.EqualTo(0));
            Assert.That(vm.Statistics.CharacterCount, Is.EqualTo(0));
        }

        [Test]
        public void Counters_WhitespaceOnly()
        {
            var vm = NewVm();
            vm.MainText = "   \n\t  ";
            Assert.That(vm.Statistics.WordCount, Is.EqualTo(0));
            Assert.That(vm.Statistics.SentenceCount, Is.EqualTo(0));
            Assert.That(vm.Statistics.ParagraphCount, Is.EqualTo(0));
            Assert.That(vm.Statistics.LineCount, Is.EqualTo(2));
            Assert.That(vm.Statistics.CharacterCount, Is.EqualTo(7));
        }

        [Test]
        public void Counters_SingleLine_NoLineBreaks()
        {
            var vm = NewVm();
            vm.MainText = "This is a test";
            Assert.That(vm.Statistics.WordCount, Is.EqualTo(4));
            Assert.That(vm.Statistics.SentenceCount, Is.EqualTo(1));
            Assert.That(vm.Statistics.ParagraphCount, Is.EqualTo(1));
            Assert.That(vm.Statistics.LineCount, Is.EqualTo(1));
            Assert.That(vm.Statistics.CharacterCount, Is.EqualTo(14));
        }

        [Test]
        public void Counters_SingleWord_NoPunctuation()
        {
            var vm = NewVm();
            vm.MainText = "Word";
            Assert.That(vm.Statistics.WordCount, Is.EqualTo(1));
            Assert.That(vm.Statistics.SentenceCount, Is.EqualTo(1));
            Assert.That(vm.Statistics.ParagraphCount, Is.EqualTo(1));
            Assert.That(vm.Statistics.LineCount, Is.EqualTo(1));
            Assert.That(vm.Statistics.CharacterCount, Is.EqualTo(4));
        }

        [Test]
        public void Counters_LargeInput()
        {
            var vm = NewVm();
            var text = string.Join("\n\n", Enumerable.Repeat("word1 word2. word3!", 1000));
            vm.MainText = text;
            Assert.That(vm.Statistics.WordCount, Is.EqualTo(3000));
            Assert.That(vm.Statistics.SentenceCount, Is.GreaterThan(0));
            Assert.That(vm.Statistics.ParagraphCount, Is.EqualTo(1000));
            Assert.That(vm.Statistics.LineCount, Is.EqualTo(1999));
            Assert.That(vm.Statistics.CharacterCount, Is.EqualTo(text.Length));
        }

        [Test]
        public void Constructor_Defaults_Are_Safe()
        {
            var vm = new MainViewModel(
                new StubClipboardService(),
                new UndoRedoService(),
                CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default,
                null!);
            Assert.That(vm.Options.CasingStyle, Is.EqualTo(CasingStyle.DoNotChange));
            Assert.That(vm.MainText, Is.Null.Or.EqualTo(string.Empty));
            Assert.That(vm.Statistics.WordCount, Is.EqualTo(0));
            Assert.That(vm.Statistics.CharacterCount, Is.EqualTo(0));
            Assert.That(vm.Statistics.SentenceCount, Is.EqualTo(0));
            Assert.That(vm.Statistics.ParagraphCount, Is.EqualTo(0));
            Assert.That(vm.Statistics.LineCount, Is.EqualTo(0));
        }

        [Test]
        public void Counters_Update_On_MainText_Set()
        {
            var vm = NewVm();
            vm.MainText = "Hello world!\n\nNew para.";
            Assert.That(vm.Statistics.WordCount, Is.EqualTo(4));
            Assert.That(vm.Statistics.SentenceCount, Is.EqualTo(2));
            Assert.That(vm.Statistics.ParagraphCount, Is.EqualTo(2));
            Assert.That(vm.Statistics.LineCount, Is.EqualTo(3));
            Assert.That(vm.Statistics.CharacterCount, Is.EqualTo("Hello world!\n\nNew para.".Length));
        }

        [Test]
        public void Clean_TrimStart_PerLine()
        {
            var vm = NewVm();
            vm.MainText = "  a\n   b  \n c";
            vm.Options.ShouldTrimStart = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("a\nb  \nc"));
        }

        [Test]
        public void Clean_TrimStart_Removes_Leading_LineBreaks()
        {
            var vm = NewVm();
            vm.MainText = "\n\n  a\n  b  ";
            vm.Options.ShouldTrimStart = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("a\nb  "));
        }

        [Test]
        public void Clean_TrimEnd_PerLine()
        {
            var vm = NewVm();
            vm.MainText = "a  \n b \n c   ";
            vm.Options.ShouldTrimEnd = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("a\n b\n c"));
        }

        [Test]
        public void Clean_TrimEnd_Removes_Trailing_LineBreaks()
        {
            var vm = NewVm();
            vm.MainText = "  a\n  b  \n\n";
            vm.Options.ShouldTrimEnd = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("  a\n  b"));
        }

        [Test]
        public void Clean_Collapse_IntraLine_Whitespace()
        {
            var vm = NewVm();
            vm.MainText = "a   b\t\tc\nx    y";
            vm.Options.ShouldRemoveMultipleSpaces = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("a b c\nx y"));
        }

        [Test]
        public void Clean_Multiple_Blank_Lines_To_Single_Blank_Line()
        {
            var vm = NewVm();
            vm.MainText = "a\n\n\n\nb";
            vm.Options.ShouldRemoveMultipleLines = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("a\n\nb"));
        }

        [Test]
        public void Clean_Unwrap_All_Lines_To_Spaces()
        {
            var vm = NewVm();
            vm.MainText = "a \n  b\t\n\nc";
            vm.Options.ShouldRemoveAllLines = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("a b c"));
        }

        [Test]
        public void Clean_Unwrap_All_Then_Collapse_Spaces()
        {
            var vm = NewVm();
            vm.MainText = "a \n   b\t\tc\n\n   d";
            vm.Options.ShouldRemoveAllLines = true;
            vm.Options.ShouldRemoveMultipleSpaces = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("a b c d"));
        }

        [Test]
        public void Clean_PunctuationFix_Key_Edges()
        {
            var vm = NewVm();
            vm.Options.ShouldFixPunctuationSpace = true;

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
            vm.Options.ShouldTrim = true;
            vm.Options.ShouldFixPunctuationSpace = true;
            vm.Options.CasingStyle = CasingStyle.Uppercase;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("A, B"));
        }

        [Test]
        public void Clean_Lowercase_Applied_Last()
        {
            var vm = NewVm();
            vm.MainText = "  Hello WORLD!  ";
            vm.Options.ShouldTrim = true;
            vm.Options.CasingStyle = CasingStyle.Lowercase;
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
        [TestCase(" hello ,world !  ok ", " Hello ,world !  Ok ")]
        [TestCase("WHEN THAT WRAPPER CLOSES (E.G.,) BRINGS DEPTH BACK TO 0)", "When that wrapper closes (e.g.,) brings depth back to 0)")]
        [TestCase("THIS WILL NEED A DRAG-AND-DROP PATCH", "This will need a drag-and-drop patch")]
        [TestCase("This Will Need A Drag-and-drop Patch", "This will need a drag-and-drop patch")]
        [TestCase("i’m looking at how honorifics like \"mr.\" are processed.", "I’m looking at how honorifics like \"Mr.\" are processed.")]
        public void Clean_SentenceCase_Robust_Scenarios(string input, string expected)
        {
            var vm = NewVm();
            vm.MainText = input;
            vm.Options.CasingStyle = CasingStyle.SentenceCase;
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
            vm.Options.CasingStyle = CasingStyle.SentenceCase;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo(expected));
        }

        [Test]
        public void Clean_SentenceCase_Normalizes_Alternating_Case()
        {
            var vm = NewVm();
            vm.MainText = "after that, tEsT again. ok.";
            vm.Options.CasingStyle = CasingStyle.SentenceCase;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("After that, test again. Ok."));
        }

        [Test]
        public void Clean_SentenceCase_Applied_Last_CamelSafe()
        {
            var vm = NewVm();
            vm.MainText = "tHis IS. a tEsT.\nnew line";
            vm.Options.CasingStyle = CasingStyle.SentenceCase;
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
                vm.Options.CasingStyle = CasingStyle.TitleCase;
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
            vm.Options.ShouldRemoveMultipleSpaces = true;
            vm.Clean();
            Assert.That(vm.MainText, Is.EqualTo("a b"));
            vm.Undo();
            Assert.That(vm.MainText, Is.EqualTo("a   b"));
        }

        [Test]
        public void Undo_After_Multiple_Cleans()
        {
            var vm = NewVm();
            vm.MainText = "  a   b  \n c";
            vm.Options.ShouldTrim = true;
            vm.Clean();
            vm.Options.ShouldRemoveMultipleSpaces = true;
            vm.Clean();
            vm.Undo();
            Assert.That(vm.MainText, Is.EqualTo("a   b\nc"));
            vm.Undo();
            Assert.That(vm.MainText, Is.EqualTo("  a   b  \n c"));
        }
    }
}

using NUnit.Framework;
using System.Globalization;
using System.Threading;
using System.Windows; // WPF Application
using TidyText.ViewModel;

namespace TidyText.Tests
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

        [Test]
        public void Clean_SentenceCase_Applied_Last()
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
                Assert.That(vm.MainText, Is.EqualTo("Hello From The World Of Text"));
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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace TidyText.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        // White Spaces
        [ObservableProperty] private bool _shouldTrim;
        [ObservableProperty] private bool _shouldTrimEnd;
        [ObservableProperty] private bool _shouldTrimStart;
        [ObservableProperty] private bool _shouldRemoveMultipleSpaces;
        [ObservableProperty] private bool _shouldRemoveMultipleLines;
        [ObservableProperty] private bool _shouldRemoveAllLines;
        [ObservableProperty] private bool _shouldFixPunctuaionSpace;
        [ObservableProperty] private bool _wrapLines;

        // Letter Case
        [ObservableProperty] private bool _IsUppercase;
        [ObservableProperty] private bool _IsLowercase;
        [ObservableProperty] private bool _IsSentenceCase;
        [ObservableProperty] private bool _IsCapEachWord;
        [ObservableProperty] private bool _IsDoNotChange;

        // Input Text
        private string _mainText;
        private string _previousText;
        private Stack<string> _inputStringStack = new();

        // Counters
        private int _wordCount;
        private int _characterCount;
        private int _sentenceCount;
        private int _paragraphCount;
        private int _lineBreakCount;

        // Getters and Setters
        public int WordCount { get => _wordCount; set => SetProperty(ref _wordCount, value); }
        public int CharacterCount { get => _characterCount; set => SetProperty(ref _characterCount, value); }
        public int SentenceCount { get => _sentenceCount; set => SetProperty(ref _sentenceCount, value); }
        public int ParagraphCount { get => _paragraphCount; set => SetProperty(ref _paragraphCount, value); }
        public int LineBreakCount { get => _lineBreakCount; set => SetProperty(ref _lineBreakCount, value); }

        public string MainText
        {
            get => _mainText;
            set
            {
                SetProperty(ref _mainText, value);
                WordCount = CountWords(_mainText);
                CharacterCount = CountCharacters(_mainText);
                SentenceCount = CountSentences(_mainText);
                ParagraphCount = CountParagraphs(_mainText);
                LineBreakCount = CountLineBreaks(_mainText);
            }
        }

        public MainViewModel()
        {
            Application.Current.Exit += OnApplicationClosing;
            GetSettings();
            IsDoNotChange = true;
            _mainText = string.Empty;
            _previousText = string.Empty;
        }

        [RelayCommand]
        public void Undo()
        {
            if (_inputStringStack.Count > 0)
            {
                MainText = _inputStringStack.Pop();
            }
        }

        [RelayCommand]
        public void Copy()
        {
            Clipboard.SetText(MainText);
        }

        [RelayCommand]
        public void Clean()
        {
            _previousText = MainText;

            // Tries to remove white spaces
            if (ShouldTrim) { MainText = MainText.Trim(); }
            if (ShouldTrimStart) { MainText = MainText.TrimStart(); }
            if (ShouldTrimEnd) { MainText = MainText.TrimEnd(); }
            if (ShouldRemoveMultipleSpaces) { MainText = CovertMultipleSpaceToSingle(MainText); }
            if (ShouldRemoveMultipleLines) { MainText = CovertMultipleLinesToSingle(MainText); }
            if (ShouldRemoveAllLines) { MainText = RemoveAllLineBreaks(MainText); }
            if (ShouldFixPunctuaionSpace) { MainText = FixSpacesAfterPuntuation(MainText); }

            // Tries changing letter case
            if (IsUppercase) { MainText = MainText.ToUpper(); }
            else if (IsLowercase) { MainText = MainText.ToLower(); }
            else if (IsSentenceCase) { MainText = ConvertToSentenceCase(MainText); }
            else if (IsCapEachWord) { MainText = ConvertToTitleCase(MainText); }

            if (MainText != _previousText) { _inputStringStack.Push(_previousText); }
        }

        // Text statistics related methods
        private int CountWords(string text) => text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

        private int CountCharacters(string text) => text.Length;

        private int CountSentences(string text)
        {
            string[] sentences = Regex.Split(text, @"(?<=[.!?])\s+");
            int count = 0;

            foreach (string sentence in sentences)
            {
                if (!string.IsNullOrWhiteSpace(sentence)) { count++; }
            }
            return count;
        }

        public int CountParagraphs(string text) => Regex.Split(text, @"\n+").Count(paragraph => !string.IsNullOrWhiteSpace(paragraph));

        public int CountLineBreaks(string text) => text.Split('\n').Length;

        // White space related methods
        public string CovertMultipleSpaceToSingle(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new StringBuilder(text.Length);
            bool previousIsSpace = false;
            foreach (char c in text)
            {
                if (c == ' ')
                {
                    if (!previousIsSpace)
                    {
                        sb.Append(' ');
                        previousIsSpace = true;
                    }
                }
                else
                {
                    sb.Append(c);
                    previousIsSpace = false;
                }
            }
            return sb.ToString();
        }

        public string CovertMultipleLinesToSingle(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new StringBuilder(text.Length);
            int newlineCount = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // Skip carriage returns – treat CR/LF uniformly
                if (c == '\r')
                    continue;

                if (c == '\n')
                {
                    newlineCount++;
                    // Keep at most two consecutive newlines to separate paragraphs
                    if (newlineCount < 3)
                        sb.Append('\n');
                }
                else
                {
                    sb.Append(c);
                    newlineCount = 0;
                }
            }
            return sb.ToString();
        }

        public string RemoveAllLineBreaks(string text) => Regex.Replace(text, @"\s*(\r\n?|\n)\s*", "");

        public string FixSpacesAfterPuntuation(string text)
        {
            // remove spaces before punctuation (.,!?:;) but leave hyphens/dashes and brackets alone
            string result = Regex.Replace(text, @"\s+([,.!?;:])", "$1");
            // ensure a space after punctuation unless it's the end of line or another punctuation
            result = Regex.Replace(result, @"([,.!?;:])(?=\S)", "$1 ");
            // collapse spaced ellipses back to "..."
            result = Regex.Replace(result, @"\\.\\s?\\.\\s?\\.", "...");
            // avoid inserting spaces inside numbers
            result = Regex.Replace(result, @"(?<=\\d)[,.](?=\\d)", "$0");

            result = Regex.Replace(result, @"\\s*([\\)\\]\\}])", "$1");
            result = Regex.Replace(result, @"([\\(\\[\\{])\\s*", "$1");
            return result;
        }

        // Letter case related methods
        private string ConvertToSentenceCase(string text)
        {
            string[] sentences = Regex.Split(text, @"(?<=[.!?])(?<!\\b[A-Za-z]\\.)\\s+");

            for (int i = 0; i < sentences.Length; i++)
            {
                string sentence = sentences[i];

                if (!string.IsNullOrEmpty(sentence))
                {
                    sentence = char.ToUpper(sentence[0]) + sentence.Substring(1).ToLower(CultureInfo.CurrentCulture);
                }

                sentences[i] = sentence;
            }
            return string.Join(" ", sentences);
        }

        public string ConvertToTitleCase(string text) => new CultureInfo("en-US", false).TextInfo.ToTitleCase(text);

        // Other methods
        public void GetSettings()
        {
            WrapLines = Properties.Settings.Default.IsWrapLine;
            ShouldTrim = Properties.Settings.Default.ShouldTrim;
            ShouldTrimStart = Properties.Settings.Default.ShouldTrimLeadSpaces;
            ShouldTrimEnd = Properties.Settings.Default.ShouldTrimTrailSpaces;
            ShouldRemoveMultipleSpaces = Properties.Settings.Default.ShouldTrimMultipleSpaces;
            ShouldRemoveMultipleLines = Properties.Settings.Default.ShouldTrimMultipleLines;
            ShouldRemoveAllLines = Properties.Settings.Default.ShouldRemoveAllLines;
            ShouldFixPunctuaionSpace = Properties.Settings.Default.ShouldFixPunctuaionSpace;
        }

        public void SaveSettings()
        {
            Properties.Settings.Default.IsWrapLine = WrapLines;
            Properties.Settings.Default.ShouldTrim = ShouldTrim;
            Properties.Settings.Default.ShouldTrimLeadSpaces = ShouldTrimStart;
            Properties.Settings.Default.ShouldTrimTrailSpaces = ShouldTrimEnd;
            Properties.Settings.Default.ShouldTrimMultipleSpaces = ShouldRemoveMultipleSpaces;
            Properties.Settings.Default.ShouldTrimMultipleLines = ShouldRemoveMultipleLines;
            Properties.Settings.Default.ShouldRemoveAllLines = ShouldRemoveAllLines;
            Properties.Settings.Default.ShouldFixPunctuaionSpace = ShouldFixPunctuaionSpace;
            Properties.Settings.Default.Save();
        }

        private void OnApplicationClosing(object sender, ExitEventArgs e) => SaveSettings();
    }
}
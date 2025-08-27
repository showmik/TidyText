using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using TidyText.Model;

namespace TidyText.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        // White Spaces
        [ObservableProperty] private bool _shouldTrim;
        [ObservableProperty] private bool _shouldTrimStart;
        [ObservableProperty] private bool _shouldTrimEnd;
        [ObservableProperty] private bool _shouldRemoveMultipleSpaces;
        [ObservableProperty] private bool _shouldRemoveMultipleLines;
        [ObservableProperty] private bool _shouldRemoveAllLines;
        [ObservableProperty] private bool _shouldFixPunctuationSpace;
        [ObservableProperty] private bool _wrapLines;

        // Letter Case
        [ObservableProperty] private bool _isUppercase;
        [ObservableProperty] private bool _isLowercase;
        [ObservableProperty] private bool _isSentenceCase;
        [ObservableProperty] private bool _isCapitalizeEachWord;
        [ObservableProperty] private bool _isDoNotChange;

        // Input Text
        private string _mainText;
        private string _previousText;
        private Stack<string> _inputTextHistory = new();

        // Counters
        private int _wordCount;
        private int _characterCount;
        private int _sentenceCount;
        private int _paragraphCount;
        private int _lineCount;

        // Getters and Setters
        public int WordCount { get => _wordCount; set => SetProperty(ref _wordCount, value); }
        public int CharacterCount { get => _characterCount; set => SetProperty(ref _characterCount, value); }
        public int SentenceCount { get => _sentenceCount; set => SetProperty(ref _sentenceCount, value); }
        public int ParagraphCount { get => _paragraphCount; set => SetProperty(ref _paragraphCount, value); }
        public int LineCount { get => _lineCount; set => SetProperty(ref _lineCount, value); }

        public string MainText
        {
            get => _mainText;
            set
            {
                SetProperty(ref _mainText, value);
                WordCount = GetWordCount(_mainText);
                CharacterCount = GetCharacterCount(_mainText);
                SentenceCount = GetSentenceCount(_mainText);
                ParagraphCount = GetParagraphCount(_mainText);
                LineCount = GetLineBreakCount(_mainText);
            }
        }

        public MainViewModel()
        {
            Application.Current.Exit += OnApplicationClosing;
            LoadSettings();
            IsDoNotChange = true;
            _mainText = string.Empty;
            _previousText = string.Empty;
        }

        [RelayCommand]
        public void Undo()
        {
            if (_inputTextHistory.Count > 0)
            {
                MainText = _inputTextHistory.Pop();
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
            _previousText = MainText ?? string.Empty;

            bool trimStart = ShouldTrim || ShouldTrimStart;
            bool trimEnd = ShouldTrim || ShouldTrimEnd;

            string text = NormalizeNewlines(_previousText);

            if (trimStart || trimEnd)
                text = TrimEachLine(text, trimStart, trimEnd);

            if (ShouldRemoveMultipleSpaces)
                text = CollapseIntraLineWhitespace(text);

            if (ShouldRemoveMultipleLines)
                text = ConvertMultipleLinesToSingle(text);

            if (ShouldRemoveAllLines)
            {
                text = UnwrapAllLines(text);
                if (ShouldRemoveMultipleSpaces) text = CollapseIntraLineWhitespace(text);
            }

            if (ShouldFixPunctuationSpace)
                text = TextSpacing.FixPunctuationSpacing(text, spaceAfterColon: false);

            // CASE at the very end
            if (IsUppercase) text = text.ToUpper(CultureInfo.CurrentCulture);
            else if (IsLowercase) text = text.ToLower(CultureInfo.CurrentCulture);
            else if (IsSentenceCase) text = ConvertToSentenceCase(text);
            else if (IsCapitalizeEachWord) text = new CultureInfo("en-US", false).TextInfo.ToTitleCase(text);

            if (text != _previousText)
            {
                _inputTextHistory.Push(_previousText);
                MainText = text;
            }
        }

        // Text statistics related methods
        private int GetWordCount(string text) => string.IsNullOrWhiteSpace(text) ? 0 : text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

        private int GetCharacterCount(string text) => text?.Length ?? 0;

        private int GetSentenceCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            string[] sentences = Regex.Split(text, @"(?<=[.!?])\s+");
            int count = 0;
            foreach (string sentence in sentences)
            {
                if (!string.IsNullOrWhiteSpace(sentence)) { count++; }
            }
            return count;
        }

        private int GetParagraphCount(string text) => string.IsNullOrWhiteSpace(text) ? 0 : Regex.Split(text, @"\n+").Count(paragraph => !string.IsNullOrWhiteSpace(paragraph));

        private int GetLineBreakCount(string text) => string.IsNullOrEmpty(text) ? 0 : text.Split('\n').Length;

        // White space related methods
        public string ConvertMultipleLinesToSingle(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new StringBuilder(text.Length);
            int newlineCount = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\r')
                    continue;
                if (c == '\n')
                {
                    newlineCount++;
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

        private static string NormalizeNewlines(string text)
        {
            if (text is null) return string.Empty;
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static string TrimEachLine(string text, bool trimStart, bool trimEnd)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var lines = NormalizeNewlines(text).Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (trimStart && trimEnd) line = line.Trim();
                else if (trimStart) line = line.TrimStart();
                else if (trimEnd) line = line.TrimEnd();
                lines[i] = line;
            }
            return string.Join("\n", lines);
        }

        private static string UnwrapAllLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            text = NormalizeNewlines(text);
            var sb = new StringBuilder(text.Length);
            bool prevSpace = false;
            foreach (var ch in text)
            {
                if (ch == '\n')
                {
                    if (!prevSpace) { sb.Append(' '); prevSpace = true; }
                }
                else if (char.IsWhiteSpace(ch))
                {
                    if (!prevSpace) { sb.Append(' '); prevSpace = true; }
                }
                else
                {
                    sb.Append(ch);
                    prevSpace = false;
                }
            }
            return sb.ToString();
        }

        private static string CollapseIntraLineWhitespace(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            text = NormalizeNewlines(text);
            var sb = new StringBuilder(text.Length);
            bool inWhitespace = false;
            foreach (var ch in text)
            {
                if (ch == '\n') { sb.Append('\n'); inWhitespace = false; continue; }
                if (char.IsWhiteSpace(ch))
                {
                    if (!inWhitespace) { sb.Append(' '); inWhitespace = true; }
                }
                else
                {
                    sb.Append(ch);
                    inWhitespace = false;
                }
            }
            return sb.ToString();
        }

        private static string ConvertToSentenceCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            text = NormalizeNewlines(text);

            var sb = new StringBuilder(text.Length);
            bool atSentenceStart = true;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (atSentenceStart && char.IsLetter(c))
                {
                    sb.Append(char.ToUpper(c, CultureInfo.CurrentCulture));
                    atSentenceStart = false;
                }
                else
                {
                    sb.Append(char.ToLower(c, CultureInfo.CurrentCulture));
                }

                if (c == '.' || c == '!' || c == '?')
                {
                    int j = i + 1;
                    while (j < text.Length && (text[j] == ' ')) j++;
                    if (j < text.Length && text[j] != '\n') atSentenceStart = true;
                }
                else if (c == '\n')
                {
                    atSentenceStart = true;
                }
            }
            return sb.ToString();
        }

        // Settings methods
        public void LoadSettings()
        {
            WrapLines = Properties.Settings.Default.IsWrapLine;
            ShouldTrim = Properties.Settings.Default.ShouldTrim;
            ShouldTrimStart = Properties.Settings.Default.ShouldTrimLeadSpaces;
            ShouldTrimEnd = Properties.Settings.Default.ShouldTrimTrailSpaces;
            ShouldRemoveMultipleSpaces = Properties.Settings.Default.ShouldTrimMultipleSpaces;
            ShouldRemoveMultipleLines = Properties.Settings.Default.ShouldTrimMultipleLines;
            ShouldRemoveAllLines = Properties.Settings.Default.ShouldRemoveAllLines;
            ShouldFixPunctuationSpace = Properties.Settings.Default.ShouldFixPunctuaionSpace;
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
            Properties.Settings.Default.ShouldFixPunctuaionSpace = ShouldFixPunctuationSpace;
            Properties.Settings.Default.Save();
        }

        private void OnApplicationClosing(object sender, ExitEventArgs e) => SaveSettings();
    }
}
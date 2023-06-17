using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;

namespace TextCleaner.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty] private bool _shouldTrim;
        [ObservableProperty] private bool _shouldTrimLeadSpaces;
        [ObservableProperty] private bool _shouldTrimTrailSpaces;
        [ObservableProperty] private bool _shouldTrimMultipleSpaces;
        [ObservableProperty] private bool _shouldTrimMultipleLines;
        [ObservableProperty] private bool _shouldRemoveAllLines;
        [ObservableProperty] private bool _shouldFixPunctuaionSpace;
        [ObservableProperty] private bool _wrapLines;

        [ObservableProperty] private bool _IsUppercase;
        [ObservableProperty] private bool _IsLowercase;
        [ObservableProperty] private bool _IsSentenceCase;
        [ObservableProperty] private bool _IsCapEachWord;
        [ObservableProperty] private bool _IsDoNotChange;

        private string _mainText;

        private int _wordCount;
        private int _characterCount;
        private int _sentenceCount;
        private int _paragraphCount;
        private int _lineBreakCount;

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
        }

        [RelayCommand]
        public void CopyButton()
        {
            Clipboard.SetText(MainText);
        }

        [RelayCommand]
        public void CleanButton()
        {
            if (ShouldTrim)
            {
                MainText = MainText.Trim();
            }

            if (ShouldTrimLeadSpaces)
            {
                MainText = MainText.TrimStart();
            }

            if (ShouldTrimTrailSpaces)
            {
                MainText = MainText.TrimEnd();
            }

            if (ShouldTrimMultipleSpaces)
            {
                MainText = CovertMultipleSpaceToSingle(MainText);
            }

            if (ShouldTrimMultipleLines)
            {
                MainText = CovertMultipleLinesToSingle(MainText);
            }

            if (ShouldRemoveAllLines)
            {
                MainText = RemoveAllLineBreaks(MainText);
            }

            if (ShouldFixPunctuaionSpace)
            {
                MainText = FixSpacesAfterPuntuation(MainText);
            }

            if (IsUppercase)
            {
                MainText = MainText.ToUpper();
            }
            else if (IsLowercase)
            {
                MainText = MainText.ToLower();
            }
            else if (IsSentenceCase)
            {
                MainText = ConvertToSentenceCase(MainText);
            }
            else if (IsCapEachWord)
            {
                MainText = ConvertToTitleCase(MainText);
            }
        }

        private int CountWords(string text) => text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

        private int CountCharacters(string text) => text.Length;

        private int CountSentences(string text) => Regex.Split(text, @"(?<=[.!?])\s+").Length;

        public int CountParagraphs(string text) => Regex.Split(text, @"\n\s*\n").Length;

        public int CountLineBreaks(string text) => Regex.Matches(text, @"\n").Count;

        public string CovertMultipleSpaceToSingle(string text) => Regex.Replace(text, @"\s+", " ");

        public string CovertMultipleLinesToSingle(string text) => Regex.Replace(text, @"(\n\s*){2,}", "\n\n");

        public string RemoveAllLineBreaks(string text) => Regex.Replace(text, @"\r\n?|\n", "");

        public string FixSpacesAfterPuntuation(string text) => Regex.Replace(text, @"(?<=[^\s—–])\s*(\p{P})(?<!-)\s*", "$1 ");

        private string ConvertToSentenceCase(string text)
        {
            string[] sentences = Regex.Split(text, @"(?<=[\.!\?])\s+");

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

        public void GetSettings()
        {
            WrapLines = Properties.Settings.Default.IsWrapLine;
            ShouldTrim = Properties.Settings.Default.ShouldTrim;
            ShouldTrimLeadSpaces = Properties.Settings.Default.ShouldTrimLeadSpaces;
            ShouldTrimTrailSpaces = Properties.Settings.Default.ShouldTrimTrailSpaces;
            ShouldTrimMultipleSpaces = Properties.Settings.Default.ShouldTrimMultipleSpaces;
            ShouldTrimMultipleLines = Properties.Settings.Default.ShouldTrimMultipleLines;
            ShouldRemoveAllLines = Properties.Settings.Default.ShouldRemoveAllLines;
            ShouldFixPunctuaionSpace = Properties.Settings.Default.ShouldFixPunctuaionSpace;
        }

        public void SaveSettings()
        {
            Properties.Settings.Default.IsWrapLine = WrapLines;
            Properties.Settings.Default.ShouldTrim = ShouldTrim;
            Properties.Settings.Default.ShouldTrimLeadSpaces = ShouldTrimLeadSpaces;
            Properties.Settings.Default.ShouldTrimTrailSpaces = ShouldTrimTrailSpaces;
            Properties.Settings.Default.ShouldTrimMultipleSpaces = ShouldTrimMultipleSpaces;
            Properties.Settings.Default.ShouldTrimMultipleLines = ShouldTrimMultipleLines;
            Properties.Settings.Default.ShouldRemoveAllLines = ShouldRemoveAllLines;
            Properties.Settings.Default.ShouldFixPunctuaionSpace = ShouldFixPunctuaionSpace;
            Properties.Settings.Default.Save();
        }

        private void OnApplicationClosing(object sender, ExitEventArgs e)
        {
            SaveSettings();
        }
    }
}
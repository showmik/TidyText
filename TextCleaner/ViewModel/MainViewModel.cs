using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace TextCleaner.ViewModel
{
    internal partial class MainViewModel : ObservableObject
    {
        [ObservableProperty] private bool _trim;
        [ObservableProperty] private bool _removeLeadSpace;
        [ObservableProperty] private bool _removeTrailSpace;
        [ObservableProperty] private bool _multipleSpaceToSingle;
        [ObservableProperty] private bool _multipleLinesToSingle;
        [ObservableProperty] private bool _removeLineBreaks;
        [ObservableProperty] private bool _wrapLines;

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
        public TextBox TextBoxReference { get; set; }


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
            WrapLines = true;
        }

        private int CountWords(string text)
        {
            string[] words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            return words.Length;
        }

        private int CountCharacters(string text)
        {
            string cleanedText = Regex.Replace(text, @"\s", "");
            return cleanedText.Length;
        }

        private int CountSentences(string text)
        {
            string[] sentences = Regex.Split(text, @"(?<=[.!?])\s+");
            return sentences.Length;
        }

        public int CountParagraphs(string text)
        {
            string[] paragraphs = Regex.Split(text, @"\n\s*\n");
            return paragraphs.Length;
        }

        public int CountLineBreaks(string text)
        {
            int lineBreaks = Regex.Matches(text, @"\n").Count;
            return lineBreaks;
        }
    }
}
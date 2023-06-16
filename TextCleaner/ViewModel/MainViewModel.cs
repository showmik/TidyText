using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text.RegularExpressions;
using System.Windows;

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

        public int WordCount { get => _wordCount; set => SetProperty(ref _wordCount, value); }
        public int CharacterCount { get => _characterCount; set => SetProperty(ref _characterCount, value); }
       

        public string MainText
        {
            get => _mainText;
            set
            {
                SetProperty(ref _mainText, value);
                WordCount = CountWords(_mainText);
                CharacterCount = CountCharacters(_mainText);
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
    }
}
﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
            if(Trim)
            {
                MainText = MainText.Trim();
            }

            if(RemoveLeadSpace)
            {
                MainText = MainText.TrimStart();
            }

            if (RemoveTrailSpace)
            {
                MainText = MainText.TrimEnd();
            }

            if (MultipleSpaceToSingle)
            {
                MainText = CovertMultipleSpaceToSingle(MainText);
            }

            if (MultipleLinesToSingle)
            {
                MainText = CovertMultipleLinesToSingle(MainText);
            }

        }

        private int CountWords(string text) => text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

        private int CountCharacters(string text) => Regex.Replace(text, @"\s", "").Length;

        private int CountSentences(string text) => Regex.Split(text, @"(?<=[.!?])\s+").Length;

        public int CountParagraphs(string text) => Regex.Split(text, @"\n\s*\n").Length;

        public int CountLineBreaks(string text) => Regex.Matches(text, @"\n").Count;

        public string CovertMultipleSpaceToSingle(string text) => Regex.Replace(text, @"\s+", " ");

        public string CovertMultipleLinesToSingle(string text) => Regex.Replace(text, @"(\n\s*){2,}", "\n\n");
    }
}
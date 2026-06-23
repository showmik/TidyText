using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TidyText.Domain.Services;
using TidyText.Domain.TextEngine;
using TidyText.Domain.TextEngine.Processors;
using TidyText.Domain.Statistics;
using CommunityToolkit.Mvvm.Messaging;
using TidyText.App.Messages;

namespace TidyText.App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public string CurrentText => MainText;
        public void ReplaceText(string newText) => MainText = newText;

        private readonly IClipboardService _clipboardService;
        private readonly IUndoRedoService _undoRedoService;

        
        // --- Input & Output ---
        [ObservableProperty]
        private string _mainText = string.Empty;

        // --- View State ---
        [ObservableProperty]
        private bool _isAIPanelOpen = false;
        
        [ObservableProperty]
        private bool _isHistoryPanelOpen = false;

        [RelayCommand]
        private void ToggleHistoryPanel()
        {
            IsHistoryPanelOpen = !IsHistoryPanelOpen;
        }

        [ObservableProperty]
        private bool _wrapLines = true;
        
        // --- Statistics ---
        [ObservableProperty] private int _wordCount;
        [ObservableProperty] private int _characterCount;
        [ObservableProperty] private int _sentenceCount;
        [ObservableProperty] private int _paragraphCount;
        [ObservableProperty] private int _lineCount;
        [ObservableProperty] private int _readingTimeSeconds;
        [ObservableProperty] private string _readabilityScore = "N/A";

        // --- Processing Options ---
        [ObservableProperty] private bool _shouldTrim = false;
        [ObservableProperty] private bool _shouldTrimStart = true;
        [ObservableProperty] private bool _shouldTrimEnd = true;
        [ObservableProperty] private bool _shouldRemoveMultipleSpaces = true;
        [ObservableProperty] private bool _shouldRemoveMultipleLines = true;
        [ObservableProperty] private bool _shouldRemoveAllLines = false;
        [ObservableProperty] private bool _shouldFixPunctuationSpace = true;
        [ObservableProperty] private bool _shouldRemoveHtmlTags = false;
        [ObservableProperty] private bool _shouldConvertSmartQuotes = false;
        
        [ObservableProperty] private CasingStyle _selectedCasingStyle = CasingStyle.DoNotChange;

        partial void OnSelectedCasingStyleChanged(CasingStyle value)
        {
            OnPropertyChanged(nameof(IsUppercase));
            OnPropertyChanged(nameof(IsLowercase));
            OnPropertyChanged(nameof(IsSentenceCase));
            OnPropertyChanged(nameof(IsTitleCase));
            OnPropertyChanged(nameof(IsDoNotChange));
        }

        public bool IsUppercase
        {
            get => SelectedCasingStyle == CasingStyle.Uppercase;
            set { if (value) SelectedCasingStyle = CasingStyle.Uppercase; }
        }

        public bool IsLowercase
        {
            get => SelectedCasingStyle == CasingStyle.Lowercase;
            set { if (value) SelectedCasingStyle = CasingStyle.Lowercase; }
        }

        public bool IsSentenceCase
        {
            get => SelectedCasingStyle == CasingStyle.SentenceCase;
            set { if (value) SelectedCasingStyle = CasingStyle.SentenceCase; }
        }

        public bool IsTitleCase
        {
            get => SelectedCasingStyle == CasingStyle.TitleCase;
            set { if (value) SelectedCasingStyle = CasingStyle.TitleCase; }
        }

        public bool IsDoNotChange
        {
            get => SelectedCasingStyle == CasingStyle.DoNotChange;
            set { if (value) SelectedCasingStyle = CasingStyle.DoNotChange; }
        }

        public MainViewModel(IClipboardService clipboardService, IUndoRedoService undoRedoService, IMessenger messenger)
        {
            _clipboardService = clipboardService;
            _undoRedoService = undoRedoService;

            messenger.Register<MainViewModel, TextReplacementRequestedMessage>(this, (r, m) => r.ReplaceText(m.NewText));
            messenger.Register<MainViewModel, CurrentTextRequestMessage>(this, (r, m) => m.Reply(r.CurrentText));
        }

        partial void OnMainTextChanged(string value)
        {
            UpdateStatistics(value);
        }

        private void UpdateStatistics(string text)
        {
            var stats = TextStatistics.Calculate(text);
            WordCount = stats.WordCount;
            CharacterCount = stats.CharacterCount;
            SentenceCount = stats.SentenceCount;
            ParagraphCount = stats.ParagraphCount;
            LineCount = stats.LineCount;

            // Average reading speed: 200 words per minute -> 3.3 words per second
            ReadingTimeSeconds = (int)(WordCount / 3.3);

            var scores = ReadabilityScorer.Calculate(stats);
            if (WordCount > 0)
            {
                ReadabilityScore = $"{scores.LixIndex:F1} ({scores.ReadingEaseDescription})";
            }
            else
            {
                ReadabilityScore = "N/A";
            }
        }

        [RelayCommand]
        public void CleanText()
        {
            if (string.IsNullOrEmpty(MainText)) return;

            _undoRedoService.Push(MainText);

            var pipeline = new TextPipelineBuilder()
                .AddMarkdownStripper() // Always add markdown stripping as per original design, or add flag
                .If(ShouldRemoveHtmlTags, b => b.AddHtmlStripper())
                .If(ShouldConvertSmartQuotes, b => b.AddSmartQuotes())
                .AddWhitespaceCleaning(ShouldTrim || ShouldTrimStart, ShouldTrim || ShouldTrimEnd, ShouldRemoveMultipleSpaces, ShouldRemoveMultipleLines, ShouldRemoveAllLines)
                .AddPunctuationCleaning(ShouldFixPunctuationSpace)
                .AddCasing(SelectedCasingStyle)
                .Build();

            MainText = pipeline.Process(MainText);
        }

        [RelayCommand]
        public void Clean()
        {
            CleanText();
        }

        [RelayCommand]
        public void Undo()
        {
            var previous = _undoRedoService.Undo(MainText);
            if (previous != null)
                MainText = previous;
        }

        [RelayCommand]
        public void Redo()
        {
            var next = _undoRedoService.Redo(MainText);
            if (next != null)
                MainText = next;
        }

        [RelayCommand]
        public void ToggleAIPanel()
        {
            IsAIPanelOpen = !IsAIPanelOpen;
        }
        
        [RelayCommand]
        public void Copy()
        {
            if (!string.IsNullOrEmpty(MainText))
            {
                _clipboardService.SetText(MainText);
                // Trigger toast notification event here
            }
        }
    }
}

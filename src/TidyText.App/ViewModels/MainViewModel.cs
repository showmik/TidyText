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
    public partial class DocumentStatistics : ObservableObject
    {
        [ObservableProperty] private int _wordCount;
        [ObservableProperty] private int _characterCount;
        [ObservableProperty] private int _sentenceCount;
        [ObservableProperty] private int _paragraphCount;
        [ObservableProperty] private int _lineCount;
        [ObservableProperty] private int _readingTimeSeconds;
        [ObservableProperty] private string _readabilityScore = "N/A";
    }

    public partial class TextCleaningOptions : ObservableObject
    {
        [ObservableProperty] private bool _shouldTrim = false;
        [ObservableProperty] private bool _shouldTrimStart = true;
        [ObservableProperty] private bool _shouldTrimEnd = true;
        [ObservableProperty] private bool _shouldRemoveMultipleSpaces = true;
        [ObservableProperty] private bool _shouldRemoveMultipleLines = true;
        [ObservableProperty] private bool _shouldRemoveAllLines = false;
        [ObservableProperty] private bool _shouldFixPunctuationSpace = true;
        [ObservableProperty] private bool _shouldRemoveHtmlTags = false;
        [ObservableProperty] private bool _shouldConvertSmartQuotes = false;
        [ObservableProperty] private CasingStyle _casingStyle = CasingStyle.DoNotChange;
    }

    public partial class MainViewModel : ObservableObject
    {
        public AIAssistantViewModel AIAssistantVM { get; }
        public string CurrentText => MainText;
        public void ReplaceText(string newText) => MainText = newText;

        private readonly IClipboardService _clipboardService;
        private readonly IUndoRedoService _undoRedoService;

        // --- Core State ---
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentText))]
        private string _mainText = string.Empty;

        // --- View State ---
        [ObservableProperty]
        private bool _isAIPanelOpen = false;
        
        [ObservableProperty]
        private bool _isHistoryPanelOpen = false;

        [ObservableProperty]
        private bool _wrapLines = true;

        // --- Modular Sub-States ---
        [ObservableProperty]
        private DocumentStatistics _statistics = new();

        [ObservableProperty]
        private TextCleaningOptions _options = new();

        public MainViewModel(IClipboardService clipboardService, IUndoRedoService undoRedoService, IMessenger messenger, AIAssistantViewModel aiAssistantVM)
        {
            _clipboardService = clipboardService;
            _undoRedoService = undoRedoService;
            AIAssistantVM = aiAssistantVM;

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
            var scores = ReadabilityScorer.Calculate(stats);

            // Instant UI update via single property assignment
            Statistics = new DocumentStatistics
            {
                WordCount = stats.WordCount,
                CharacterCount = stats.CharacterCount,
                SentenceCount = stats.SentenceCount,
                ParagraphCount = stats.ParagraphCount,
                LineCount = stats.LineCount,
                ReadingTimeSeconds = (int)(stats.WordCount / 3.3),
                ReadabilityScore = stats.WordCount > 0 ? $"{scores.LixIndex:F1} ({scores.ReadingEaseDescription})" : "N/A"
            };
        }

        [RelayCommand]
        private void ToggleHistoryPanel() => IsHistoryPanelOpen = !IsHistoryPanelOpen;

        [RelayCommand]
        public void ToggleAIPanel() => IsAIPanelOpen = !IsAIPanelOpen;

        [RelayCommand]
        public void CleanText()
        {
            if (string.IsNullOrEmpty(MainText)) return;

            _undoRedoService.Push(MainText);

            var pipeline = new TextPipelineBuilder()
                .AddMarkdownStripper() 
                .If(Options.ShouldRemoveHtmlTags, b => b.AddHtmlStripper())
                .If(Options.ShouldConvertSmartQuotes, b => b.AddSmartQuotes())
                .AddWhitespaceCleaning(Options.ShouldTrim || Options.ShouldTrimStart, Options.ShouldTrim || Options.ShouldTrimEnd, Options.ShouldRemoveMultipleSpaces, Options.ShouldRemoveMultipleLines, Options.ShouldRemoveAllLines)
                .AddPunctuationCleaning(Options.ShouldFixPunctuationSpace)
                .AddCasing(Options.CasingStyle)
                .Build();

            MainText = pipeline.Process(MainText);
        }

        [RelayCommand]
        public void Clean() => CleanText();

        [RelayCommand]
        public void Undo()
        {
            var previous = _undoRedoService.Undo(MainText);
            if (previous != null) MainText = previous;
        }

        [RelayCommand]
        public void Redo()
        {
            var next = _undoRedoService.Redo(MainText);
            if (next != null) MainText = next;
        }

        [RelayCommand]
        public void Copy()
        {
            if (!string.IsNullOrEmpty(MainText))
            {
                _clipboardService.SetText(MainText);
            }
        }
    }
}

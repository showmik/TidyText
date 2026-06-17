using System;
using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TidyText.Core.TextEngine;
using TidyText.Core.TextEngine.Processors;
using TidyText.Core.Statistics;

namespace TidyText.App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly CleaningPipeline _pipeline;
        
        public AIAssistantViewModel? AIAssistantVM { get; set; }

        
        // --- Input & Output ---
        [ObservableProperty]
        private string _mainText = string.Empty;

        // --- View State ---
        [ObservableProperty]
        private bool _isAIPanelOpen = false;

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
        private readonly System.Collections.Generic.Stack<string> _history = new();

        [ObservableProperty] private bool _shouldTrim = true;
        [ObservableProperty] private bool _shouldTrimStart = false;
        [ObservableProperty] private bool _shouldTrimEnd = false;
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

        public MainViewModel()
        {
            _pipeline = new CleaningPipeline()
                .AddProcessor(new MarkdownProcessor()) // Strips markdown if enabled
                .AddProcessor(new HtmlStripProcessor())
                .AddProcessor(new SmartQuoteProcessor())
                .AddProcessor(new WhitespaceProcessor())
                .AddProcessor(new PunctuationProcessor())
                .AddProcessor(new CasingProcessor());
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
                ReadabilityScore = $"{scores.FleschReadingEase:F1} ({scores.ReadingEaseDescription})";
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

            _history.Push(MainText);

            var options = new WhitespaceProcessorOptions
            {
                TrimStart = ShouldTrim || ShouldTrimStart,
                TrimEnd = ShouldTrim || ShouldTrimEnd,
                RemoveMultipleSpaces = ShouldRemoveMultipleSpaces,
                RemoveMultipleLines = ShouldRemoveMultipleLines,
                RemoveAllLines = ShouldRemoveAllLines
            };

            // Re-configure pipeline with current options using the clean unified architecture
            _pipeline.ClearProcessors()
                .AddProcessor(new HtmlStripProcessor(new HtmlStripProcessorOptions { RemoveHtmlTags = ShouldRemoveHtmlTags }))
                .AddProcessor(new SmartQuoteProcessor(new SmartQuoteProcessorOptions { ConvertSmartQuotes = ShouldConvertSmartQuotes }))
                .AddProcessor(new WhitespaceProcessor(options))
                .AddProcessor(new PunctuationProcessor(new PunctuationProcessorOptions { FixPunctuationSpacing = ShouldFixPunctuationSpace, TreatColonAsSentencePunct = true }))
                .AddProcessor(new CasingProcessor(new CasingProcessorOptions { Style = SelectedCasingStyle }));
            
            MainText = _pipeline.Process(MainText);
        }

        [RelayCommand]
        public void Clean()
        {
            CleanText();
        }

        [RelayCommand]
        public void Undo()
        {
            if (_history.Count > 0)
            {
                MainText = _history.Pop();
            }
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
                Clipboard.SetText(MainText);
                // Trigger toast notification event here
            }
        }
    }
}

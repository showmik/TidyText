using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Windows;
using TidyText.Model;

namespace TidyText.ViewModel;

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
    private Stack<string> _inputTextsStack = new();

    // Counters
    private int _wordCount;
    private int _characterCount;
    private int _sentenceCount;
    private int _paragraphCount;
    private int _lineBreakCount;

    // Others

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
            WordCount = Counter.CountWords(_mainText);
            CharacterCount = Counter.CountCharacters(_mainText);
            SentenceCount = Counter.CountSentences(_mainText);
            ParagraphCount = Counter.CountParagraphs(_mainText);
            LineBreakCount = Counter.CountLineBreaks(_mainText);
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
        if (_inputTextsStack.Count > 0)
        {
            MainText = _inputTextsStack.Pop();
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
        if (ShouldRemoveMultipleSpaces) { MainText = Cleaner.RemoveMultipleSpaces(MainText); }
        if (ShouldRemoveMultipleLines) { MainText = Cleaner.RemoveMultipleLines(MainText); }
        if (ShouldRemoveAllLines) { MainText = Cleaner.RemoveAllLineBreaks(MainText); }
        if (ShouldFixPunctuaionSpace) { MainText = Cleaner.FixSpacesAfterPuntuation(MainText); }

        // Tries changing letter case
        if (IsUppercase) { MainText = MainText.ToUpper(); }
        else if (IsLowercase) { MainText = MainText.ToLower(); }
        else if (IsSentenceCase) { MainText = Cleaner.ConvertToSentenceCase(MainText); }
        else if (IsCapEachWord) { MainText = Cleaner.ConvertToTitleCase(MainText); }

        if (MainText != _previousText) { _inputTextsStack.Push(_previousText); }
    }

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
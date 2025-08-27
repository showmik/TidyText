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
            var culture = CultureInfo.CurrentCulture;

            bool atSentenceStart = true;
            int i = 0, n = text.Length;

            while (i < n)
            {
                char c = text[i];

                if (char.IsLetterOrDigit(c))
                {
                    int start = i;
                    // token = letters/digits with internal apostrophes (’ or ')
                    while (i < n && (char.IsLetterOrDigit(text[i]) || text[i] == '\'' || text[i] == '’')) i++;
                    string token = text.Substring(start, i - start);

                    string outTok;
                    bool hasDigit = token.Any(char.IsDigit);

                    // single-letter pronoun
                    if (token.Length == 1 && (token[0] == 'i' || token[0] == 'I'))
                    {
                        outTok = "I";
                    }
                    else if (atSentenceStart)
                    {
                        // Start of sentence: keep long acronyms & digit-mixed tokens; otherwise normalize to "This"
                        if (IsAllCapsAcronym(token) || hasDigit)
                            outTok = token;
                        else
                            outTok = char.ToUpper(token[0], culture) +
                                     (token.Length > 1 ? token.Substring(1).ToLower(culture) : string.Empty);
                    }
                    else
                    {
                        // Mid-sentence: preserve proper nouns/brands/acronyms/digits & CamelCase, else lowercase
                        if (IsAllCapsAcronym(token) || LooksLikeSimpleTitleCase(token) || IsCamelOrMixedCase(token) || hasDigit)
                            outTok = token;
                        else
                            outTok = token.ToLower(culture);
                    }

                    sb.Append(outTok);
                    atSentenceStart = false;
                    continue; // i already advanced to token end
                }

                // Non-word char path
                sb.Append(c);

                if (IsSentenceTerminator(c))
                {
                    // Decimal guard: "3.14" dot isn't a sentence end
                    if (!(i > 0 && i + 1 < n && char.IsDigit(text[i - 1]) && char.IsDigit(text[i + 1])))
                    {
                        // Expand over dotted chunk (e.g., "p.m.", "U.S.A.") and test as a whole
                        int left = i - 1;
                        while (left >= 0 && (char.IsLetter(text[left]) || text[left] == '.')) left--;
                        int right = i + 1;
                        while (right < n && (char.IsLetter(text[right]) || text[right] == '.')) right++;

                        string dotted = text.Substring(left + 1, right - (left + 1));
                        string normalized = dotted.Trim('.').ToLowerInvariant();

                        bool abbreviation = NonTerminalAbbr.Contains(normalized);
                        if (!abbreviation)
                        {
                            // Next real word after spaces/wrappers starts a sentence
                            int j = i + 1;
                            while (j < n && (char.IsWhiteSpace(text[j]) || IsWrapper(text[j]))) j++;
                            if (j < n) atSentenceStart = true;
                        }
                    }
                }
                else if (c == '\n')
                {
                    atSentenceStart = true;
                }

                i++;
            }

            return sb.ToString();
        }


        private static bool IsSentenceTerminator(char c) => c == '.' || c == '!' || c == '?';
        private static bool IsWrapper(char c) =>
            c == '"' || c == '“' || c == '”' || c == '\'' || c == '’' ||
            c == ')' || c == ']' || c == '}';

        private static readonly HashSet<string> NonTerminalAbbr = new(StringComparer.OrdinalIgnoreCase)
        {
            "mr","mrs","ms","dr","prof","sr","jr","st",
            "vs","v","etc","e.g","eg","i.e","ie",
            "a.m","am","p.m","pm",
            "u.s","u.s.a","u.k","u.n"
        };



        private static bool ShouldPreserveToken(string token)
        {
            // Preserve if it’s ALL CAPS (≥2 caps), has an interior capital (CamelCase), or has digits
            int upper = 0;
            for (int i = 0; i < token.Length; i++)
            {
                char ch = token[i];
                if (char.IsUpper(ch)) upper++;
                if (i > 0 && char.IsUpper(ch)) return true; // mixed case
                if (char.IsDigit(ch)) return true;
            }
            return upper >= 2;
        }

        // Capitalize a single token, respecting apostrophes like O'Neill
        private static string CapTokenWithApostrophes(string token, CultureInfo culture)
        {
            if (token.Length == 0) return token;

            var chars = token.ToCharArray();
            bool capNext = true; // first letter
            for (int i = 0; i < chars.Length; i++)
            {
                char ch = chars[i];
                if (char.IsLetter(ch))
                {
                    chars[i] = capNext ? char.ToUpper(ch, culture) : char.ToLower(ch, culture);
                    capNext = false;
                }
                else
                {
                    // After apostrophe, capitalize next letter: O'Neill / rock 'n' roll -> 'N' becomes capital
                    capNext = (ch == '\'' || ch == '’');
                }
            }
            return new string(chars);
        }

        // preserve ALL-CAPS acronyms of length ≥3 (NASA, SLS)
        private static bool IsAllCapsAcronym(string token)
        {
            int letters = 0;
            foreach (var ch in token)
            {
                if (char.IsLetter(ch))
                {
                    letters++;
                    if (!char.IsUpper(ch)) return false;
                }
            }
            return letters >= 3;
        }

        // simple TitleCase like "Dr", "Smith" (first letter upper, rest lower)
        private static bool LooksLikeSimpleTitleCase(string token)
        {
            bool seen = false;
            for (int i = 0; i < token.Length; i++)
            {
                char ch = token[i];
                if (!char.IsLetter(ch)) continue;
                if (!seen)
                {
                    if (!char.IsUpper(ch)) return false;
                    seen = true;
                }
                else
                {
                    if (char.IsLetter(ch) && !char.IsLower(ch)) return false;
                }
            }
            return seen;
        }

        // Preserve brand-like Camel/MixedCase (iPhone, YouTube, OpenAI, McDonald’s, macOS, iOS).
        // Reject only shouty/alternating patterns like "tEsT".
        private static bool IsCamelOrMixedCase(string token)
        {
            int letters = 0, uppers = 0, lowers = 0, transitions = 0;
            char? prevLetter = null;

            for (int i = 0; i < token.Length; i++)
            {
                char ch = token[i];
                if (!char.IsLetter(ch)) continue;

                letters++;
                bool isUpper = char.IsUpper(ch);
                if (isUpper) uppers++; else lowers++;

                if (prevLetter.HasValue)
                {
                    bool prevUpper = char.IsUpper(prevLetter.Value);
                    if (prevUpper != isUpper) transitions++;
                }
                prevLetter = ch;
            }

            if (letters == 0) return false;
            if (uppers == 0 || lowers == 0) return false; // must be mixed case

            // Reject only if it's almost fully alternating case: e.g., t->E->s->T has transitions = letters-1
            if (transitions >= letters - 1) return false;

            // Accept if there is an interior uppercase with a lowercase neighbor (eBay, iPhone, YouTube, OpenAI, McDonald’s, macOS)
            for (int i = 1; i < token.Length - 1; i++)
            {
                if (char.IsUpper(token[i]) && (char.IsLower(token[i - 1]) || char.IsLower(token[i + 1])))
                    return true;
            }

            // Edge cases like "iOS": lower prefix followed by an upper run
            bool sawLowerPrefix = false, sawUpperRunAfter = false;
            for (int i = 0; i < token.Length; i++)
            {
                char ch = token[i];
                if (!char.IsLetter(ch)) continue;
                if (char.IsLower(ch))
                {
                    if (!sawUpperRunAfter) sawLowerPrefix = true;
                }
                else if (char.IsUpper(ch) && sawLowerPrefix)
                {
                    sawUpperRunAfter = true;
                }
            }
            return sawLowerPrefix && sawUpperRunAfter;
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
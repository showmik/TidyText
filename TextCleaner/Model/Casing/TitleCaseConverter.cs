// TidyText/Model/Casing/TitleCaseConverter.cs
using System;
using System.Globalization;
using System.Text;

namespace TidyText.Model.Casing
{
    /// <summary>
    /// AP-style Title Case converter.
    /// - Lowercases “small words” unless first/last or forced after a colon.
    /// - Preserves acronyms (via ISentenceCaseLexicon) and camel/mixed case tokens.
    /// - Handles hyphenated compounds.
    /// Performance: O(n), no LINQ/regex in the hot path, allocation-light.
    /// </summary>
    public sealed class TitleCaseConverter
    {
        public static TitleCaseConverter Default { get; } =
            new TitleCaseConverter(DefaultTitleCaseLexicon.Instance,
                                   DefaultSentenceCaseLexicon.Instance,
                                   new TitleCaseOptions());

        private readonly ITitleCaseLexicon _titleLex;
        private readonly ISentenceCaseLexicon _commonLex; // reuse acronyms & proper/mixed-case hints
        private readonly TitleCaseOptions _opt;

        public TitleCaseConverter(ITitleCaseLexicon titleLexicon,
                                  ISentenceCaseLexicon commonLexicon,
                                  TitleCaseOptions options)
        {
            _titleLex = titleLexicon ?? throw new ArgumentNullException(nameof(titleLexicon));
            _commonLex = commonLexicon ?? throw new ArgumentNullException(nameof(commonLexicon));
            _opt = options ?? new TitleCaseOptions();
        }

        public string Convert(string text) => Convert(text, _opt.Culture);

        public string Convert(string text, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(text)) return text;

            text = NormalizeNewlines(text);
            var sb = new StringBuilder(text.Length);

            int firstWordStart = FindFirstWordStart(text);
            int lastWordStart = FindLastWordStart(text);

            bool forceNextWordCap = false; // for “CapitalizeAfterColon”

            int i = 0, n = text.Length;
            while (i < n)
            {
                char c = text[i];

                if (char.IsLetterOrDigit(c))
                {
                    int start = i;
                    while (i < n && (char.IsLetterOrDigit(text[i]) || text[i] == '\'' || text[i] == '’' || text[i] == '-')) i++;
                    string token = text.Substring(start, i - start);

                    bool isFirstWord = (start == firstWordStart);
                    bool isLastWord = (start == lastWordStart);

                    string outTok = ProcessWord(token, isFirstWord, isLastWord, culture, forceNextWordCap);

                    sb.Append(outTok);
                    forceNextWordCap = false; // consumed if it was set
                    continue; // i already advanced
                }

                // separators & punctuation
                sb.Append(c);
                if (_opt.CapitalizeAfterColon && c == ':')
                {
                    // AP: capitalize first word after a colon
                    forceNextWordCap = true;
                }
                i++;
            }

            return sb.ToString();
        }

        // ---------- Core word processing ----------

        private string ProcessWord(string token, bool isFirstWord, bool isLastWord,
                                   CultureInfo culture, bool forceCapBecauseOfColon)
        {
            // Hyphenated compound?
            int dash = token.IndexOf('-');
            if (dash >= 0 && _opt.CapitalizeHyphenatedSegments)
            {
                var sbTok = new StringBuilder(token.Length);
                int idx = 0, segIndex = 0;

                // Count segments once to know which is last
                int segCount = 1; for (int t = 0; t < token.Length; t++) if (token[t] == '-') segCount++;

                while (idx < token.Length)
                {
                    int segStart = idx;
                    while (idx < token.Length && token[idx] != '-') idx++;
                    string seg = token.Substring(segStart, idx - segStart);

                    bool isFirstSeg = segIndex == 0;
                    bool isLastSeg = segIndex == segCount - 1;

                    // Force-cap if: colon rule OR title first/last OR segment is first/last in the compound
                    bool forceCapThisSeg =
                        _opt.ForceCapFirstAndLast && (isFirstWord || isLastWord) ||
                        forceCapBecauseOfColon || isFirstSeg || isLastSeg;

                    if (seg.Length > 0)
                        sbTok.Append(ProcessSingleSegment(seg, isFirstWord, isLastWord, culture, forceCapThisSeg));

                    if (idx < token.Length && token[idx] == '-') { sbTok.Append('-'); idx++; }

                    segIndex++;
                }
                return sbTok.ToString();
            }


            // Single segment (no hyphens)
            return ProcessSingleSegment(token, isFirstWord, isLastWord, culture, forceCapBecauseOfColon);
        }

        private string ProcessSingleSegment(string seg, bool isFirstWord, bool isLastWord,
                                            CultureInfo culture, bool forceCapBecauseOfColon)
        {
            // Canonical brand/proper casing from shared lexicon (iphone -> iPhone, ebay -> eBay, …)
            if (_commonLex.ProperCaseMap.TryGetValue(seg, out var proper))
                return proper;

            // Acronyms: always restore to uppercase form if present in shared whitelist (gpu -> GPU, ai -> AI, ap -> AP)
            if (_opt.PreserveAcronyms && TryMapAcronym(seg, out var mappedAcr))
                return mappedAcr;

            // Single-letter words (headline letter names like "A to Z")
            if (_opt.UppercaseSingleLetterWords && seg.Length == 1 && char.IsLetter(seg[0]))
                return seg.ToUpper(culture);

            // Don’t touch protected tokens
            if (_titleLex.ProtectedAsIs.Contains(seg)) return seg;

            // Preserve acronyms (ALL CAPS, whitelisted) — e.g., GPT, NASA
            if (_opt.PreserveAcronyms && IsAllCapsAcronym(seg)) return seg;

            // Preserve mixed/camel case tokens (iPhone, eBay, macOS, McDonald’s)
            if (_opt.PreserveCamelOrMixedCase && IsCamelOrMixedCase(seg)) return seg;

            bool isSmall = _titleLex.SmallWords.Contains(seg);
            bool mustCap = forceCapBecauseOfColon ||
                           (_opt.ForceCapFirstAndLast && (isFirstWord || isLastWord));

            if (isSmall && !mustCap)
                return seg.ToLower(culture);

            return CapTokenWithApostrophes(seg, culture);
        }

        // ---------- helpers ----------

        private static string NormalizeNewlines(string s) =>
            s.Replace("\r\n", "\n").Replace("\r", "\n");

        private static int FindFirstWordStart(string s)
        {
            for (int i = 0; i < s.Length; i++)
                if (char.IsLetterOrDigit(s[i])) return i;
            return -1;
        }

        private static int FindLastWordStart(string s)
        {
            for (int i = s.Length - 1; i >= 0; i--)
            {
                if (char.IsLetterOrDigit(s[i]))
                {
                    int j = i;
                    while (j > 0 && (char.IsLetterOrDigit(s[j - 1]) || s[j - 1] == '\'' || s[j - 1] == '’' || s[j - 1] == '-')) j--;
                    return j;
                }
            }
            return -1;
        }

        private bool IsAllCapsAcronym(string token)
        {
            // Strict: letters only, all uppercase, in whitelist
            int letters = 0;
            for (int i = 0; i < token.Length; i++)
            {
                char ch = token[i];
                if (char.IsLetter(ch))
                {
                    letters++;
                    if (!char.IsUpper(ch)) return false;
                }
                else if (char.IsDigit(ch))
                {
                    // “GPT-4” segment can be “GPT4” here; allow digits but require at least one letter
                }
                else if (ch == '-' || ch == '\'' || ch == '’')
                {
                    // handled elsewhere
                }
                else
                {
                    return false;
                }
            }
            if (letters < 2) return false;
            return _commonLex.UpperAcronyms.Contains(token);
        }

        private static string CapTokenWithApostrophes(string token, CultureInfo culture)
        {
            if (token.Length == 0) return token;
            var chars = token.ToCharArray();
            bool capNext = true;
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
                    // After apostrophe, capitalize next letter: O'Neill
                    capNext = (ch == '\'' || ch == '’');
                }
            }
            return new string(chars);
        }

        // Heuristic similar to sentence case version
        private static bool IsCamelOrMixedCase(string token)
        {
            int letters = 0, uppers = 0, lowers = 0, transitions = 0;
            char? prev = null;

            for (int i = 0; i < token.Length; i++)
            {
                char ch = token[i];
                if (!char.IsLetter(ch)) continue;

                letters++;
                bool up = char.IsUpper(ch);
                if (up) uppers++; else lowers++;

                if (prev.HasValue)
                {
                    bool prevUp = char.IsUpper(prev.Value);
                    if (prevUp != up) transitions++;
                }
                prev = ch;
            }

            if (letters == 0) return false;
            if (uppers == 0 || lowers == 0) return false;
            if (transitions >= letters - 1) return false;

            // Camel-ish patterns
            for (int i = 1; i < token.Length - 1; i++)
                if (char.IsUpper(token[i]) && (char.IsLower(token[i - 1]) || char.IsLower(token[i + 1])))
                    return true;

            // McDonald’s-ish
            bool sawLowerPrefix = false, sawUpperAfter = false;
            for (int i = 0; i < token.Length; i++)
            {
                char ch = token[i];
                if (!char.IsLetter(ch)) continue;
                if (char.IsLower(ch)) { if (!sawUpperAfter) sawLowerPrefix = true; }
                else if (char.IsUpper(ch) && sawLowerPrefix) { sawUpperAfter = true; }
            }
            return sawLowerPrefix && sawUpperAfter;
        }

        private bool TryMapAcronym(string seg, out string mapped)
        {
            // Uppercase letters, keep digits/apostrophes/hyphens out of this check (segments don’t include hyphens here)
            var sb = new StringBuilder(seg.Length);
            for (int i = 0; i < seg.Length; i++)
            {
                char ch = seg[i];
                if (char.IsLetter(ch)) sb.Append(char.ToUpperInvariant(ch));
                else if (char.IsDigit(ch)) sb.Append(ch);
                else sb.Append(ch);
            }
            string upper = sb.ToString();

            if (_commonLex.UpperAcronyms.Contains(upper))
            {
                mapped = upper;
                return true;
            }
            mapped = null;
            return false;
        }
    }
}

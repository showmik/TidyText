// TidyText/Model/Casing/SentenceCaseConverter.cs
using System;
using System.Globalization;
using System.Text;
using System.Collections.Generic;

namespace TidyText.Model.Casing
{
    /// <summary>
    /// High-performance sentence case converter:
    ///  - No LINQ in hot path, minimal allocations
    ///  - Pluggable lexicon for community contributions
    /// </summary>
    public sealed class SentenceCaseConverter
    {
        public static SentenceCaseConverter Default { get; } =
            new SentenceCaseConverter(DefaultSentenceCaseLexicon.Instance, new SentenceCaseOptions());

        private readonly ISentenceCaseLexicon _lex;
        private readonly SentenceCaseOptions _opt;

        public SentenceCaseConverter(ISentenceCaseLexicon lexicon, SentenceCaseOptions options)
        {
            _lex = lexicon ?? throw new ArgumentNullException(nameof(lexicon));
            _opt = options ?? new SentenceCaseOptions();
        }

        public string Convert(string text) => Convert(text, _opt.Culture);

        public string Convert(string text, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(text)) return text;

            text = NormalizeNewlines(text);
            var sb = new StringBuilder(text.Length);
            bool atSentenceStart = true;

            string? prevOutToken = null;
            bool prevHadDigit = false;
            bool prevWasBrand = false;

            int i = 0, n = text.Length;
            while (i < n)
            {
                char c = text[i];

                if (char.IsLetterOrDigit(c))
                {
                    int start = i;
                    while (i < n && (char.IsLetterOrDigit(text[i]) || text[i] == '\'' || text[i] == '’')) i++;
                    string token = text.Substring(start, i - start);

                    bool hasDigit = HasDigit(token);
                    string outTok;

                    // Single-letter pronoun
                    if (token.Length == 1 && (token[0] == 'i' || token[0] == 'I'))
                    {
                        outTok = "I";
                    }
                    else if (atSentenceStart)
                    {
                        if (IsAllCapsAcronym(token) || hasDigit)
                            outTok = token;
                        else if (_lex.ProperCaseTokens.Contains(token))
                            outTok = CapTokenWithApostrophes(token, culture);
                        else
                            outTok = UpperFirstLowerRest(token, culture);
                    }
                    else
                    {
                        if (_lex.ProperCaseTokens.Contains(token))
                        {
                            outTok = CapTokenWithApostrophes(token, culture);
                        }
                        else if (_lex.BrandSuffixes.Contains(token) && (prevWasBrand || prevHadDigit))
                        {
                            outTok = CapTokenWithApostrophes(token, culture); // Pro/Max/…
                        }
                        else if ((_opt.PreserveAcronymsMidSentence && IsAllCapsAcronym(token)) ||
                                 LooksLikeSimpleTitleCase(token) ||
                                 IsCamelOrMixedCase(token) ||
                                 hasDigit)
                        {
                            outTok = token;
                        }
                        else
                        {
                            outTok = token.ToLower(culture);
                        }
                    }

                    sb.Append(outTok);
                    atSentenceStart = false;

                    prevOutToken = outTok;
                    prevHadDigit = hasDigit;
                    prevWasBrand = _lex.BrandTokens.Contains(outTok);
                    continue; // i already advanced
                }

                // punctuation / whitespace
                sb.Append(c);

                if (IsSentenceTerminator(c))
                {
                    // Decimal guard: "3.14" is not a sentence end
                    if (!(i > 0 && i + 1 < n && char.IsDigit(text[i - 1]) && char.IsDigit(text[i + 1])))
                    {
                        // Expand across dotted abbrev ("p.m.", "U.S.A.") and test as a whole
                        int left = i - 1;
                        while (left >= 0 && (char.IsLetter(text[left]) || text[left] == '.')) left--;
                        int right = i + 1;
                        while (right < n && (char.IsLetter(text[right]) || text[right] == '.')) right++;

                        string dotted = text.Substring(left + 1, right - (left + 1));
                        string normalizedNoDots = TrimDotsToLower(dotted);
                        string dottedLower = TrimTrailingDotToLower(dotted);

                        bool abbreviation =
                            _lex.NonTerminalAbbreviations.Contains(normalizedNoDots) ||
                            _lex.NonTerminalAbbreviations.Contains(dottedLower);

                        if (!abbreviation)
                        {
                            // next real word starts a sentence
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

        // ------- helpers (allocation-light) -------
        private static string NormalizeNewlines(string s) =>
            s.Replace("\r\n", "\n").Replace("\r", "\n");

        private static bool HasDigit(string token)
        {
            for (int k = 0; k < token.Length; k++) if (char.IsDigit(token[k])) return true;
            return false;
        }

        private static string UpperFirstLowerRest(string token, CultureInfo culture)
        {
            if (token.Length == 0) return token;
            if (token.Length == 1) return char.ToUpper(token[0], culture).ToString();
            return char.ToUpper(token[0], culture) + token.Substring(1).ToLower(culture);
        }

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
                    // After apostrophe, capitalize next letter: O'Neill
                    capNext = (ch == '\'' || ch == '’');
                }
            }
            return new string(chars);
        }

        private static string TrimDotsToLower(string dotted)
        {
            var sb = new StringBuilder(dotted.Length);
            for (int i = 0; i < dotted.Length; i++)
            {
                char ch = dotted[i];
                if (ch != '.') sb.Append(char.ToLowerInvariant(ch));
            }
            return sb.ToString();
        }

        private static string TrimTrailingDotToLower(string dotted)
        {
            // Lowercase and drop ONLY trailing dots (keep inner dots).
            int end = dotted.Length;
            while (end > 0 && dotted[end - 1] == '.') end--;

            // Build a lowercased string without the trailing dot(s)
            var sb = new StringBuilder(end);
            for (int k = 0; k < end; k++) sb.Append(char.ToLowerInvariant(dotted[k]));
            return sb.ToString();
        }

        private static bool IsSentenceTerminator(char c) => c == '.' || c == '!' || c == '?';
        private static bool IsWrapper(char c) =>
            c == '"' || c == '“' || c == '”' || c == '\'' || c == '’' ||
            c == ')' || c == ']' || c == '}';

        private bool IsAllCapsAcronym(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;

            int letters = 0;
            for (int i = 0; i < token.Length; i++)
            {
                char ch = token[i];
                if (char.IsLetter(ch))
                {
                    letters++;
                    if (!char.IsUpper(ch)) return false;
                }
                else
                {
                    // any non-letter (digits/hyphens) => handled by caller context, not here
                    return false;
                }
            }

            if (letters < 2) return false;

            // 1) Whitelist wins (the safe, explicit path)
            if (_lex.UpperAcronyms.Contains(token)) return true;

            // 2) Optional legacy/permissive behavior: short unknown ALL-CAPS as acronyms,
            //    excluding common short words (TO/IN/OF/WE/IT/...).
            if (_opt.TreatUnknownShortAllCapsAsAcronym &&
                letters <= 3 &&
                !_lex.UpperShortStopwords.Contains(token))
            {
                return true;
            }

            // 3) Default: unknown ALL-CAPS are NOT acronyms
            return false;
        }


        // "Dr", "Smith" style
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

        // Accept iPhone, eBay, OpenAI, McDonald’s, macOS; reject alternating tEsT
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
            if (uppers == 0 || lowers == 0) return false;

            if (transitions >= letters - 1) return false;

            for (int i = 1; i < token.Length - 1; i++)
            {
                if (char.IsUpper(token[i]) && (char.IsLower(token[i - 1]) || char.IsLower(token[i + 1])))
                    return true;
            }

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
    }
}

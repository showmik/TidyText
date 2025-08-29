// TidyText/Model/Casing/TitleCaseConverter.cs
using System;
using System.Globalization;
using System.Text;
using System.Collections.Generic;

namespace TidyText.Model.Casing
{
    /// <summary>
    /// AP-style Title Case converter with pragmatic rules:
    /// - Lowercase “small words” unless first/last or immediately after a colon.
    /// - Preserve acronyms (e.g., GPU, GPT, AP) and mixed/camel-case tokens (iPhone, eBay, macOS).
    /// - Handle hyphenated compounds (State-of-the-Art): first segment capped; interior small words lowered.
    /// - Treat the first word after each newline as a “first word”.
    /// - Keep emails verbatim (user@example.com).
    /// - Uppercase single letters adjacent to '&' without spaces (Q&A, R&D).
    /// - Handle math variables directly before '^' (E=MC^2).
    /// - Apostrophe-aware title casing (O’Neill, MacDonald’s, rock ’n’ roll).
    /// </summary>
    public sealed class TitleCaseConverter
    {
        public static TitleCaseConverter Default { get; } =
            new TitleCaseConverter(DefaultTitleCaseLexicon.Instance, new TitleCaseOptions());

        private readonly ITitleCaseLexicon _titleLex;
        private readonly TitleCaseOptions _opt;

        // Reuse the sentence lexicon’s maps (brands, acronyms)
        private readonly ISentenceCaseLexicon _commonLex = DefaultSentenceCaseLexicon.Instance;

        public TitleCaseConverter(ITitleCaseLexicon titleLexicon, TitleCaseOptions options)
        {
            _titleLex = titleLexicon ?? throw new ArgumentNullException(nameof(titleLexicon));
            _opt = options ?? new TitleCaseOptions();
        }

        public string Convert(string text) => Convert(text, _opt.Culture);

        public string Convert(string text, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(text)) return text;

            text = NormalizeNewlines(text);
            var sb = new StringBuilder(text.Length);

            var wordSpans = CollectWordSpans(text);
            if (wordSpans.Count == 0) return text;

            bool forceNextWordCap = false; // after colon
            bool inEmail = false;          // persist across user@example.com

            for (int wi = 0, nWords = wordSpans.Count; wi < nWords; wi++)
            {
                var span = wordSpans[wi];

                // Append any gap before this word unchanged
                if (span.Start > (wi == 0 ? 0 : wordSpans[wi - 1].End))
                {
                    sb.Append(text.Substring(wi == 0 ? 0 : wordSpans[wi - 1].End, span.Start - (wi == 0 ? 0 : wordSpans[wi - 1].End)));
                }

                string word = text.Substring(span.Start, span.Length);

                bool isFirstWord = (wi == 0);
                bool isLastWord = (wi == nWords - 1);

                string prevWord = wi > 0 ? text.Substring(wordSpans[wi - 1].Start, wordSpans[wi - 1].Length) : null;
                string nextWord = wi + 1 < nWords ? text.Substring(wordSpans[wi + 1].Start, wordSpans[wi + 1].Length) : null;
                string nextNextWord = wi + 2 < nWords ? text.Substring(wordSpans[wi + 2].Start, wordSpans[wi + 2].Length) : null;

                // Treat the first word after a newline as “first”
                int prevEnd = (wi == 0) ? 0 : wordSpans[wi - 1].End;
                bool isFirstOfLine = SegmentContainsNewline(text, prevEnd, span.Start);
                bool firstTokenForRules = isFirstWord || isFirstOfLine;

                // Determine delimiters around this word
                int afterEnd = span.End;
                int nextStart = wi + 1 < nWords ? wordSpans[wi + 1].Start : text.Length;

                bool noSpaceToNext = HasNoWhitespaceBetween(text, afterEnd, nextStart);
                char delimToNext = FirstNonSpaceBetween(text, afterEnd, nextStart);

                bool noSpaceFromPrev = HasNoWhitespaceBetween(text, prevEnd, span.Start);
                char delimFromPrev = LastNonSpaceBetween(text, prevEnd, span.Start);

                // Enter/Stay email mode: '@' or '.' with no spaces
                if (!inEmail && noSpaceToNext && delimToNext == '@') inEmail = true;
                if (inEmail)
                {
                    sb.Append(word);
                    // stay only if the next delimiter continues the email
                    bool stay = noSpaceToNext && (delimToNext == '@' || delimToNext == '.');
                    if (!stay) inEmail = false;

                    // colon rule update between tokens
                    forceNextWordCap = false;
                    for (int k = afterEnd; k < nextStart; k++)
                        if (text[k] == ':' && _opt.CapitalizeAfterColon) { forceNextWordCap = true; break; }

                    // trailing text after final word
                    if (wi == nWords - 1 && span.End < text.Length)
                        sb.Append(text.Substring(span.End));
                    continue;
                }

                // Uppercase single-letter tokens adjacent to '&' with no spaces (Q&A, R&D)
                string forced = null;
                bool nextIsAmp = noSpaceToNext && delimToNext == '&';
                bool prevIsAmp = noSpaceFromPrev && delimFromPrev == '&';
                if (word.Length == 1 && char.IsLetter(word[0]) && (nextIsAmp || prevIsAmp))
                {
                    forced = word.ToUpper(culture);
                }

                // Math variable directly before '^' → uppercase letters only
                bool isMathVar = HasExponentBetween(text, span.End, nextStart) && IsAllLetters(word);

                bool mustCap = (forceNextWordCap && !IsAllPunctuation(word)) ||
                               (_opt.ForceCapFirstAndLast && (firstTokenForRules || isLastWord));

                string outWord = forced ?? ProcessWord(
                    word, prevWord, nextWord, nextNextWord, culture,
                    firstTokenForRules, isLastWord, mustCap, isMathVar);

                sb.Append(outWord);

                // Update colon rule based on text between this and next word
                forceNextWordCap = false;
                for (int k = afterEnd; k < nextStart; k++)
                    if (text[k] == ':' && _opt.CapitalizeAfterColon) { forceNextWordCap = true; break; }

                if (wi == nWords - 1 && span.End < text.Length)
                    sb.Append(text.Substring(span.End));
            }

            return sb.ToString();
        }

        // ===== Core per-word logic =====

        private string ProcessWord(string word, string prevWord, string nextWord, string nextNextWord,
                                   CultureInfo culture, bool isFirstWord, bool isLastWord,
                                   bool mustCapBecauseOfColon, bool isMathVariableContext)
        {
            // Protected tokens (exact)
            if (_titleLex.ProtectedAsIs.Contains(word)) return word;

            // Hyphenated compound?
            int hy = IndexOfHyphenLike(word);
            if (hy >= 0 && _opt.CapitalizeHyphenatedSegments)
                return ProcessHyphenatedCompound(word, culture, isFirstWord, isLastWord, mustCapBecauseOfColon);

            // Brand/proper mapping first (e.g., iphone -> iPhone)
            if (_commonLex?.ProperCaseMap != null &&
                (_commonLex.ProperCaseMap.TryGetValue(word, out var mappedPC) ||
                 _commonLex.ProperCaseMap.TryGetValue(word.ToLowerInvariant(), out mappedPC)))
            {
                return mappedPC;
            }

            // Math variable before '^'
            if (isMathVariableContext && IsAllLetters(word))
                return word.ToUpper(culture);

            // Acronyms (GPU, GPT, AP) stay as ALL CAPS if known
            if (_opt.PreserveAcronyms && TryMapKnownAcronymOrAllCaps(word, out var mappedAcr))
                return mappedAcr;

            // Mixed/camel case (iPhone, eBay, macOS) preserved
            if (_opt.PreserveCamelOrMixedCase && IsCamelOrMixedCase(word))
                return word;

            // Small word logic (a, an, the, in, of, to, ...)
            // Small word logic (a, an, the, in, of, to, ...)
            bool isSmall = _titleLex.SmallWords.Contains(word) || _titleLex.SmallWords.Contains(word?.ToLowerInvariant());
            bool mustCap = mustCapBecauseOfColon || (_opt.ForceCapFirstAndLast && (isFirstWord || isLastWord));
            if (isSmall && !mustCap)
            {
                // Letter-name exception: A to Z → keep single-letter “A” uppercased
                if (_opt.UppercaseSingleLetterWords &&
                    word.Length == 1 && char.IsLetter(word[0]) &&
                    LooksLikeLetterNameContext(prevWord, word, nextWord, nextNextWord))
                {
                    return word.ToUpper(culture);
                }
                return word.ToLower(culture);
            }

            // Single-letter words: uppercase when requested by options
            if (_opt.UppercaseSingleLetterWords && word.Length == 1 && char.IsLetter(word[0]))
                return word.ToUpper(culture);

            // Default: TitleCase with apostrophe awareness (O'Neill / MacDonald's / rock ’n’ roll)
            return CapTokenWithApostrophes(word, culture);
        }

        private string ProcessHyphenatedCompound(string compound, CultureInfo culture,
                                         bool isFirstWord, bool isLastWord, bool mustCapBecauseOfColon)
        {
            // ASCII hyphen only; en/em dashes should be tokenized as separators upstream.
            string[] parts = compound.Split('-');
            var pieces = new string[parts.Length];

            for (int pi = 0; pi < parts.Length; pi++)
            {
                string seg = parts[pi];
                string segLower = seg.ToLowerInvariant();
                bool segIsFirst = (pi == 0);
                bool segIsLast = (pi == parts.Length - 1);

                // Only force-cap the very first/last segment when the WHOLE hyphenated word
                // is first/last (or after a colon)
                bool mustCap = mustCapBecauseOfColon ||
                               (_opt.ForceCapFirstAndLast && ((segIsFirst && isFirstWord) || (segIsLast && isLastWord)));

                string outSeg;

                // 1) Brand / proper mapping first (e.g., iphone-case → iPhone-Case)
                if (_commonLex?.ProperCaseMap != null &&
                    (_commonLex.ProperCaseMap.TryGetValue(seg, out var mappedPC) ||
                     _commonLex.ProperCaseMap.TryGetValue(segLower, out mappedPC)))
                {
                    outSeg = mappedPC;
                }
                // 2) Acronyms (GPU, AP, GPT) as ALL CAPS
                else if (_opt.PreserveAcronyms && TryMapKnownAcronymOrAllCaps(seg, out var mappedAcr))
                {
                    outSeg = mappedAcr;
                }
                // 3) Mixed/camel case preserved (iPhone, eBay, macOS)
                else if (_opt.PreserveCamelOrMixedCase && IsCamelOrMixedCase(seg))
                {
                    outSeg = seg;
                }
                else
                {
                    // Small word detection (case-insensitive + fallback list)
                    bool isSmall =
                        _titleLex.SmallWords.Contains(seg) ||
                        _titleLex.SmallWords.Contains(segLower) ||
                        segLower == "a" || segLower == "an" || segLower == "the" ||
                        segLower == "and" || segLower == "but" || segLower == "for" ||
                        segLower == "nor" || segLower == "or" || segLower == "so" || segLower == "yet" ||
                        segLower == "as" || segLower == "at" || segLower == "by" || segLower == "in" ||
                        segLower == "of" || segLower == "off" || segLower == "on" || segLower == "per" ||
                        segLower == "to" || segLower == "up" || segLower == "via" || segLower == "vs" || segLower == "vs.";

                    // AP style:
                    // - FIRST segment: always TitleCase
                    // - Interior small words: lowercase unless a rule forces caps
                    if (segIsFirst)
                    {
                        outSeg = (_opt.UppercaseSingleLetterWords && seg.Length == 1 && char.IsLetter(seg[0]))
                            ? seg.ToUpper(culture)
                            : CapTokenWithApostrophes(seg, culture);
                    }
                    else if (isSmall && !mustCap)
                    {
                        outSeg = segLower; // interior small words lowered
                    }
                    else
                    {
                        outSeg = (_opt.UppercaseSingleLetterWords && seg.Length == 1 && char.IsLetter(seg[0]))
                            ? seg.ToUpper(culture)
                            : CapTokenWithApostrophes(seg, culture);
                    }
                }

                pieces[pi] = outSeg;
            }

            // 🔧 FINAL SAFETY PASS:
            // Force-lower interior small words regardless of what earlier steps did.
            for (int pi = 1; pi < pieces.Length - 1; pi++)
            {
                string p = pieces[pi];
                string lower = p.ToLowerInvariant();
                if (_titleLex.SmallWords.Contains(p) || _titleLex.SmallWords.Contains(lower) ||
                    lower == "a" || lower == "an" || lower == "the" ||
                    lower == "and" || lower == "but" || lower == "for" ||
                    lower == "nor" || lower == "or" || lower == "so" || lower == "yet" ||
                    lower == "as" || lower == "at" || lower == "by" || lower == "in" ||
                    lower == "of" || lower == "off" || lower == "on" || lower == "per" ||
                    lower == "to" || lower == "up" || lower == "via" || lower == "vs" || lower == "vs.")
                {
                    pieces[pi] = lower;
                }
            }

            return string.Join("-", pieces);
        }



        // ===== Helpers =====

        private static string NormalizeNewlines(string s) => s;

        private struct Span { public int Start; public int Length; public int End => Start + Length; }

        // Include '-' so hyphenated compounds are processed together
        private static List<Span> CollectWordSpans(string s)
        {
            var spans = new List<Span>(Math.Max(8, s.Length / 6));
            int i = 0, n = s.Length;
            while (i < n)
            {
                char c = s[i];
                if (char.IsLetterOrDigit(c) || c == '\'' || c == '’' || c == '-')
                {
                    int start = i;
                    while (i < n && (char.IsLetterOrDigit(s[i]) || s[i] == '\'' || s[i] == '’' || s[i] == '-')) i++;
                    spans.Add(new Span { Start = start, Length = i - start });
                }
                else
                {
                    i++;
                }
            }
            return spans;
        }

        private static bool IsAllPunctuation(string s)
        {
            for (int i = 0; i < s.Length; i++) if (char.IsLetterOrDigit(s[i])) return false;
            return true;
        }

        private static int IndexOfHyphenLike(string s)
        {
            for (int i = 0; i < s.Length; i++) if (s[i] == '-') return i;
            return -1;
        }

        private bool TryMapKnownAcronymOrAllCaps(string seg, out string mapped)
        {
            if (seg.Length == 0) { mapped = null; return false; }

            // Build upper-only copy (letters uppercased; digits preserved)
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

        private static bool HasNoWhitespaceBetween(string s, int from, int to)
        {
            int start = Math.Max(0, Math.Min(from, s.Length));
            int end = Math.Max(0, Math.Min(to, s.Length));
            for (int i = start; i < end; i++)
                if (char.IsWhiteSpace(s[i])) return false;
            return true;
        }

        private static char FirstNonSpaceBetween(string s, int from, int to)
        {
            int start = Math.Max(0, Math.Min(from, s.Length));
            int end = Math.Max(0, Math.Min(to, s.Length));
            for (int i = start; i < end; i++)
            {
                char ch = s[i];
                if (!char.IsWhiteSpace(ch)) return ch;
            }
            return '\0';
        }

        private static char LastNonSpaceBetween(string s, int from, int to)
        {
            int start = Math.Max(0, Math.Min(from, s.Length));
            int end = Math.Max(0, Math.Min(to, s.Length));
            for (int i = end - 1; i >= start; i--)
            {
                char ch = s[i];
                if (!char.IsWhiteSpace(ch)) return ch;
            }
            return '\0';
        }

        // Only treat as exponent when the *first* non-space after the word is '^'
        private static bool HasExponentBetween(string s, int from, int to)
        {
            if (string.IsNullOrEmpty(s)) return false;
            int start = Math.Max(0, Math.Min(from, s.Length));
            int end = Math.Max(0, Math.Min(to, s.Length));
            for (int i = start; i < end; i++)
            {
                char ch = s[i];
                if (!char.IsWhiteSpace(ch)) return ch == '^';
            }
            return false;
        }

        private static bool IsAllLetters(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            for (int i = 0; i < s.Length; i++) if (!char.IsLetter(s[i])) return false;
            return true;
        }

        private static bool SegmentContainsNewline(string s, int from, int to)
        {
            if (string.IsNullOrEmpty(s)) return false;
            int start = Math.Max(0, Math.Min(from, s.Length));
            int end = Math.Max(0, Math.Min(to, s.Length));
            for (int i = start; i < end; i++)
            {
                char ch = s[i];
                if (ch == '\n' || ch == '\r') return true; // detect LF or CR (covers CRLF too)
            }
            return false;
        }

        // Tight mixed/camel case detector (rejects CaSe/inPUT noise; allows iPhone/eBay/macOS)
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
            if (transitions >= letters - 1) return false; // reject alternating noise

            // Reject trailing 2+ uppers (e.g., inPUT)
            int trailingUpper = 0;
            for (int i = token.Length - 1; i >= 0; i--)
            {
                char ch = token[i];
                if (!char.IsLetter(ch)) continue;
                if (char.IsUpper(ch)) trailingUpper++;
                else break;
            }
            if (trailingUpper >= 2) return false;

            // Require lower prefix -> internal upper -> later a lower again (iPhone, eBay, macOS)
            bool sawLowerPrefix = false, sawInternalUpper = false, sawLowerAfter = false;
            for (int i = 0; i < token.Length; i++)
            {
                char ch = token[i];
                if (!char.IsLetter(ch)) continue;

                if (!sawLowerPrefix && char.IsLower(ch)) { sawLowerPrefix = true; continue; }
                if (sawLowerPrefix && !sawInternalUpper && char.IsUpper(ch)) { sawInternalUpper = true; continue; }
                if (sawInternalUpper && char.IsLower(ch)) { sawLowerAfter = true; break; }
            }
            return sawLowerPrefix && sawInternalUpper && sawLowerAfter;
        }

        private static string CapTokenWithApostrophes(string token, CultureInfo culture)
        {
            if (token.Length == 0) return token;

            var chars = token.ToCharArray();
            bool capNext = true; // capitalize the first letter

            for (int i = 0; i < chars.Length; i++)
            {
                char ch = chars[i];

                if (char.IsLetter(ch))
                {
                    chars[i] = capNext ? char.ToUpper(ch, culture) : char.ToLower(ch, culture);
                    capNext = false;
                }
                else if (ch == '\'' || ch == '’')
                {
                    // Look only at contiguous LETTER tail after the apostrophe
                    int j = i + 1;
                    var tail = new StringBuilder(4);
                    while (j < token.Length && char.IsLetter(token[j]))
                    {
                        tail.Append(char.ToLowerInvariant(token[j]));
                        j++;
                    }
                    string tailLetters = tail.ToString();

                    // Common contraction/possessive/short tails that should NOT trigger capitalization
                    // John's, it’s, I’d, I’m, ’n’ (rock ’n’ roll), we’ll, we’re, we’ve, ’em
                    if (tailLetters == "s" || tailLetters == "t" || tailLetters == "d" ||
                        tailLetters == "m" || tailLetters == "n" || tailLetters == "ll" ||
                        tailLetters == "re" || tailLetters == "ve" || tailLetters == "em")
                    {
                        capNext = false; // keep next letter lowercase
                    }
                    else
                    {
                        // Surname/brand-style: O'Neill → capitalize N
                        capNext = true;
                    }
                }
                else
                {
                    capNext = false; // other punctuation: do not auto-cap next
                }
            }

            return new string(chars);
        }

        private static bool LooksLikeLetterNameContext(string prev, string cur, string next, string nextNext)
        {
            if (string.IsNullOrEmpty(cur) || cur.Length != 1 || !char.IsLetter(cur[0])) return false;

            // Forward pattern: A to Z
            if (!string.IsNullOrEmpty(next) &&
                next.Equals("to", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(nextNext) &&
                nextNext.Length == 1 && char.IsLetter(nextNext[0]))
            {
                return true;
            }

            // Backward pattern: (From) A to Z  — the current cur is the single-letter before "to"
            if (!string.IsNullOrEmpty(prev) &&
                prev.Length == 1 && char.IsLetter(prev[0]) &&
                !string.IsNullOrEmpty(next) &&
                next.Equals("to", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

    }
}
// Core/TextSpacing.cs
using System;
using System.Text;

namespace TidyText.Model
{
    public static class TextSpacing
    {
        /// <summary>
        /// Fixes spaces around ., !, ?, ; (and optionally :) without breaking URLs/emails/versions/decimals/times/ratios/domains/paths/code spans.
        /// </summary>
        /// <param name="input">Source text</param>
        /// <param name="treatColonAsSentencePunct">If true, ':' behaves like sentence punctuation (space after) except for times/ratios/URLs.</param>
        public static string FixPunctuationSpacing(string input, bool treatColonAsSentencePunct = false, bool spaceAfterColon = false)
        {
            if (string.IsNullOrEmpty(input)) return input;
            string s = input.Replace("\r\n", "\n").Replace("\r", "\n");

            var sb = new StringBuilder(s.Length + s.Length / 10);
            int i = 0, n = s.Length;

            while (i < n)
            {
                // 1) Protect code spans ---------------------------------------------------------
                if (s[i] == '`')
                {
                    // triple ```
                    if (i + 2 < n && s[i + 1] == '`' && s[i + 2] == '`')
                    {
                        int j = i + 3;
                        while (j + 2 < n && !(s[j] == '`' && s[j + 1] == '`' && s[j + 2] == '`')) j++;
                        j = Math.Min(j + 3, n);
                        sb.Append(s, i, j - i); i = j; continue;
                    }
                    // inline `code`
                    else
                    {
                        int j = i + 1;
                        while (j < n && s[j] != '`' && s[j] != '\n') j++;
                        if (j < n && s[j] == '`') j++;
                        sb.Append(s, i, j - i); i = j; continue;
                    }
                }

                // 2) Protect URLs / www. -------------------------------------------------------
                if (StartsWithScheme(s, i, out int end))
                { sb.Append(s, i, end - i); i = end; continue; }

                if (StartsWithWww(s, i, out end))
                { sb.Append(s, i, end - i); i = end; continue; }

                // 3) Protect paths -------------------------------------------------------------
                if (TryConsumeWinPath(s, i, out end))
                { sb.Append(s, i, end - i); i = end; continue; }

                if (TryConsumeNixPath(s, i, out end))
                { sb.Append(s, i, end - i); i = end; continue; }

                // 4) Protect emails/domains/versions/decimals/times/ratios/ellipsis -----------
                if (TryConsumeEmail(s, i, out end))
                { sb.Append(s, i, end - i); i = end; continue; }

                if (TryConsumeDomain(s, i, out end))
                { sb.Append(s, i, end - i); i = end; continue; }

                if (TryConsumeVersion(s, i, out end))
                { sb.Append(s, i, end - i); i = end; continue; }

                if (TryConsumeDecimal(s, i, out end))
                { sb.Append(s, i, end - i); i = end; continue; }

                if (TryConsumeTimeOrRatio(s, i, out end))
                { sb.Append(s, i, end - i); i = end; continue; }

                if (TryConsumeEllipsis(s, i, out end))
                {
                    sb.Append(s, i, end - i);
                    // add a space after ellipsis if next non-space is word char
                    int k = end; while (k < n && s[k] == ' ') k++;
                    if (k < n && IsWordChar(s[k])) sb.Append(' ');
                    i = end; continue;
                }

                // 5) Protect dotted abbreviations like U.S.A., e.g., i.e. ---------------------
                if (TryConsumeDottedAbbrev(s, i, out end))
                { sb.Append(s, i, end - i); i = end; continue; }

                // 6) Punctuation spacing -------------------------------------------------------
                char c = s[i];
                bool isColon = c == ':';
                if (IsSentencePunct(c) || (treatColonAsSentencePunct && isColon))
                {
                    // remove spaces before
                    while (sb.Length > 0 && sb[^1] == ' ') sb.Length--;

                    // Guard: do NOT space a colon that looks like time/ratio/URL fragment
                    if (isColon)
                    {
                        // If looks like :// (URL) → copy raw and continue
                        if (i + 2 < n && s[i + 1] == '/' && s[i + 2] == '/')
                        { sb.Append(':'); i++; continue; }

                        // If digit:digit ahead OR prev is digit → likely time/ratio; copy raw and continue
                        if ((i > 0 && char.IsDigit(sb[^1])) ||
                            (i + 1 < n && char.IsDigit(s[i + 1])))
                        {
                            sb.Append(':'); i++; continue;
                        }
                    }

                    sb.Append(c);

                    // absorb immediately-following closing quotes/brackets
                    int j = i + 1;
                    while (j < n && IsClosing(s[j])) { sb.Append(s[j]); j++; }

                    // ensure one space after if next isn’t whitespace or punctuation
                    if (j < n)
                    {
                        char nx = s[j];
                        if (nx != ' ' && nx != '\n' && nx != '\t' && !IsSentencePunct(nx))
                            sb.Append(' ');
                    }

                    i = j;
                    continue;
                }

                // default copy
                sb.Append(c);
                i++;
            }

            // Collapse 2+ spaces between non-newline tokens (preserve newlines)
            return CollapseRunsOfSpaces(sb.ToString());
        }

        // ---------------------------- helpers ----------------------------

        private static bool StartsWithScheme(string s, int i, out int end)
        {
            end = 0;
            // Use a string array instead of stackalloc ReadOnlySpan<string>
            string[] schemes = { "http://", "https://", "ftp://", "file://" };
            foreach (var sch in schemes)
            {
                if (i + sch.Length <= s.Length && string.Compare(s, i, sch, 0, sch.Length, ignoreCase: true) == 0)
                {
                    end = ScanUrlLikeEnd(s, i + sch.Length);
                    return true;
                }
            }
            return false;
        }

        private static bool StartsWithWww(string s, int i, out int end)
        {
            const string w = "www.";
            if (i + w.Length <= s.Length && string.Compare(s, i, w, 0, w.Length, ignoreCase: true) == 0)
            {
                end = ScanUrlLikeEnd(s, i + w.Length);
                return true;
            }
            end = 0; return false;
        }

        private static int ScanUntilSpace(string s, int j)
        {
            while (j < s.Length && !char.IsWhiteSpace(s[j])) j++;
            return j;
        }

        private static bool TryConsumeWinPath(string s, int i, out int end)
        {
            end = 0;
            if (i + 3 <= s.Length && char.IsLetter(s[i]) && s[i + 1] == ':' && (s[i + 2] == '\\' || s[i + 2] == '/'))
            {
                end = ScanUrlLikeEnd(s, i + 3);
                return true;
            }
            return false;
        }

        private static bool TryConsumeNixPath(string s, int i, out int end)
        {
            end = 0;
            if (s[i] == '/' && !(i + 1 < s.Length && (s[i + 1] == '/' || s[i + 1] == ' ' || s[i + 1] == '\n')))
            {
                end = ScanUrlLikeEnd(s, i + 1);
                return true;
            }
            return false;
        }

        private static bool TryConsumeEmail(string s, int i, out int end)
        {
            end = 0;
            int n = s.Length;
            int j = i;
            if (!IsEmailLocalChar(s[j])) return false;
            while (j < n && IsEmailLocalChar(s[j])) j++;
            if (j >= n || s[j] != '@') return false;
            j++;
            if (j >= n || !IsDomainLabelStart(s[j])) return false;

            int dots = 0, labelLen = 0;
            while (j < n && (char.IsLetterOrDigit(s[j]) || s[j] == '-' || s[j] == '.'))
            {
                if (s[j] == '.')
                {
                    if (labelLen == 0) return false; // empty label
                    dots++; labelLen = 0;
                }
                else labelLen++;
                j++;
            }
            if (dots == 0 || labelLen == 0) return false;

            // TLD letters only, len>=2
            int tldLen = labelLen;
            bool tldLettersOnly = true;
            for (int k = j - tldLen; k < j; k++) if (!char.IsLetter(s[k])) { tldLettersOnly = false; break; }
            if (!tldLettersOnly || tldLen < 2) return false;

            end = j; return true;
        }

        private static bool TryConsumeDomain(string s, int i, out int end)
        {
            end = 0;
            int n = s.Length, j = i;
            if (!IsDomainLabelStart(s[j])) return false;

            int dots = 0, labelLen = 0, lastLabelLen = 0;
            while (j < n && (char.IsLetterOrDigit(s[j]) || s[j] == '-' || s[j] == '.'))
            {
                if (s[j] == '.')
                {
                    if (labelLen == 0) return false;
                    dots++; lastLabelLen = labelLen; labelLen = 0; j++;
                }
                else { labelLen++; j++; }
            }
            if (dots == 0 || labelLen == 0) return false;
            lastLabelLen = labelLen;

            // TLD letters only, len>=2
            bool tldLettersOnly = true;
            for (int k = j - lastLabelLen; k < j; k++) if (!char.IsLetter(s[k])) { tldLettersOnly = false; break; }
            if (!tldLettersOnly || lastLabelLen < 2) return false;

            end = j; return true;
        }

        private static bool TryConsumeVersion(string s, int i, out int end)
        {
            end = 0;
            int n = s.Length, j = i;
            if (s[j] == 'v' || s[j] == 'V') j++;
            int groups = 0;
            if (!TryConsumeDigits(s, ref j)) return false;
            while (j < n && s[j] == '.')
            {
                int save = j; j++;
                if (!TryConsumeDigits(s, ref j)) { j = save; break; }
                groups++;
            }
            if (groups == 0) return false;
            end = j; return true;
        }

        private static bool TryConsumeDecimal(string s, int i, out int end)
        {
            end = 0;
            int j = i;
            if (!TryConsumeDigits(s, ref j)) return false;

            // allow thousand separators in integer part
            while (j < s.Length && s[j] == ',' && j + 1 < s.Length && char.IsDigit(s[j + 1]))
            {
                j++; if (!TryConsumeDigits(s, ref j)) break;
            }

            if (j < s.Length && (s[j] == '.' || s[j] == ','))
            {
                int save = j; j++;
                if (TryConsumeDigits(s, ref j)) { end = j; return true; }
                j = save;
            }
            return false;
        }

        private static bool TryConsumeTimeOrRatio(string s, int i, out int end)
        {
            end = 0;
            int n = s.Length, j = i;
            if (!TryConsumeDigitsMax(s, ref j, 2)) return false;
            if (j >= n || s[j] != ':') return false;
            j++;

            if (!TryConsumeDigitsMax(s, ref j, 2)) return false;

            // optional :ss
            if (j + 2 < n && s[j] == ':' && char.IsDigit(s[j + 1]) && char.IsDigit(s[j + 2]))
                j += 3;

            // optional AM/PM with optional space
            int k = j; while (k < n && s[k] == ' ') k++;
            if (k + 1 < n && (s[k] == 'a' || s[k] == 'A' || s[k] == 'p' || s[k] == 'P') && (s[k + 1] == 'm' || s[k + 1] == 'M'))
                j = k + 2;

            end = j; return true;
        }

        private static bool TryConsumeEllipsis(string s, int i, out int end)
        {
            end = 0;
            if (s[i] == '…') { end = i + 1; return true; }
            if (i + 2 < s.Length && s[i] == '.' && s[i + 1] == '.' && s[i + 2] == '.')
            {
                int j = i + 3;
                while (j < s.Length && s[j] == '.') j++; // handle 4+ dots
                end = j; return true;
            }
            return false;
        }

        private static bool TryConsumeDottedAbbrev(string s, int i, out int end)
        {
            end = 0;
            int n = s.Length, j = i, groups = 0;
            while (j + 1 < n && IsAsciiLetter(s[j]) && s[j + 1] == '.')
            { groups++; j += 2; }
            if (groups >= 2) { end = j; return true; }
            return false;
        }

        private static bool IsSentencePunct(char c) => c == '.' || c == ',' || c == '!' || c == '?' || c == ';';
        private static bool IsClosing(char c) => c == ')' || c == ']' || c == '}' || c == '”' || c == '’' || c == '"' || c == '\'' || c == '»';
        private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';
        private static bool IsAsciiLetter(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        private static bool IsEmailLocalChar(char c) => char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '%' || c == '+' || c == '-';
        private static bool IsDomainLabelStart(char c) => char.IsLetterOrDigit(c);
        private static bool TryConsumeDigits(string s, ref int j)
        {
            int start = j; while (j < s.Length && char.IsDigit(s[j])) j++;
            return j > start;
        }
        private static bool TryConsumeDigitsMax(string s, ref int j, int max)
        {
            int start = j, count = 0;
            while (j < s.Length && char.IsDigit(s[j]) && count < max) { j++; count++; }
            return count > 0;
        }

        private static string CollapseRunsOfSpaces(string s)
        {
            var sb = new StringBuilder(s.Length);
            bool prevSpace = false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == ' ')
                {
                    if (!prevSpace) { sb.Append(c); prevSpace = true; }
                }
                else
                {
                    sb.Append(c);
                    prevSpace = false;
                }
            }
            return sb.ToString();
        }

        // Trim trailing punctuation that typically does NOT belong to URLs/paths in prose.
        private static bool IsUrlTerminalPunct(char c) =>
            c == '.' || c == ',' || c == '!' || c == '?' || c == ';' || c == ':' ||
            c == ')' || c == ']' || c == '}' || c == '»' || c == '”' || c == '’' ||
            c == '"' || c == '\'' || c == '…';

        // Simple unmatched-open check for a bracket type; if there's an unmatched opener inside,
        // don't trim the closing bracket (e.g., "(https://ex.com/path)").
        private static bool HasUnmatchedOpening(ReadOnlySpan<char> span, char openCh, char closeCh)
        {
            int open = 0, close = 0;
            foreach (var ch in span)
            {
                if (ch == openCh) open++;
                else if (ch == closeCh) close++;
            }
            return open > close;
        }

        // Scan to next whitespace, then drop any trailing terminal punctuation that isn't part of the URL/path.
        private static int ScanUrlLikeEnd(string s, int start)
        {
            int n = s.Length;
            int j = start;

            // 1) Walk until whitespace, with an early-stop heuristic:
            //    If we see DIGIT '.' LETTER (e.g., "...=3.Please"), treat the '.' as sentence punctuation.
            while (j < n && !char.IsWhiteSpace(s[j]))
            {
                if (s[j] == '.' &&
                    j > start && char.IsDigit(s[j - 1]) &&
                    j + 1 < n && char.IsLetter(s[j + 1]))
                {
                    // stop BEFORE the '.', so the '.' is handled by punctuation spacing later
                    break;
                }
                j++;
            }

            // 2) Trim common trailing punctuation that tends to cling to URLs in prose
            int k = j;
            while (k > start && IsUrlTerminalPunct(s[k - 1]))
            {
                char tail = s[k - 1];

                // Keep a balancing closer if there was an opener inside the token (e.g., "(https://ex.com)")
                if ((tail == ')' && HasUnmatchedOpening(s.AsSpan(start, k - start - 1), '(', ')')) ||
                    (tail == ']' && HasUnmatchedOpening(s.AsSpan(start, k - start - 1), '[', ']')) ||
                    (tail == '}' && HasUnmatchedOpening(s.AsSpan(start, k - start - 1), '{', '}')))
                {
                    break;
                }

                k--;
            }

            return k;
        }

    }
}

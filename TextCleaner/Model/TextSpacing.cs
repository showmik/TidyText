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
                if (TryConsumeIPv6(s, i, out end))
                { sb.Append(s, i, end - i); i = end; continue; }

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

                // Apostrophes used inside words (It’s, I’ll): keep tight and remove any stray space before them.
                if (s[i] == '\'' || s[i] == '’')
                {
                    char prev = sb.Length > 0 ? sb[^1] : '\0';
                    char next = (i + 1 < n) ? s[i + 1] : '\0';
                    if (IsWordChar(prev) && IsWordChar(next))
                    {
                        // e.g., "I ’ ll" → "I’ll"
                        while (sb.Length > 0 && sb[^1] == ' ') sb.Length--;
                        sb.Append(s[i]);
                        i++;
                        continue;
                    }
                }

                // 6) Punctuation spacing -------------------------------------------------------
                char c = s[i];
                bool isColon = c == ':';
                if (IsClosing(s[i]))
                {
                    sb.Append(s[i]);

                    int j = i + 1;
                    if (j < n)
                    {
                        char nx = s[j];
                        if (!char.IsWhiteSpace(nx) && IsWordChar(nx))
                            sb.Append(' ');
                    }

                    i = i + 1;
                    continue;
                }
                if (IsSentencePunct(c) || (treatColonAsSentencePunct && isColon))
                {
                    // remove spaces before
                    while (sb.Length > 0 && sb[^1] == ' ') sb.Length--;

                    // colon guards: don't treat ':' as sentence punct in URLs or times/ratios
                    if (isColon)
                    {
                        if (i + 2 < n && s[i + 1] == '/' && s[i + 2] == '/')
                        { sb.Append(':'); i++; continue; }

                        if ((i > 0 && char.IsDigit(sb[^1])) ||
                            (i + 1 < n && char.IsDigit(s[i + 1])))
                        { sb.Append(':'); i++; continue; }
                    }

                    sb.Append(c);

                    // absorb immediately-following true closers (brackets, curly/guillemet closers)
                    int j = i + 1;
                    while (j < n && (IsClosing(s[j]) || IsLikelyClosingStraightQuote(s, j)))
                    {
                        sb.Append(s[j]);
                        j++;
                    }

                    // decide whether to add a space
                    bool addSpace = false;
                    if (j < n)
                    {
                        char nx = s[j];

                        // Opening quote + word: tighten only after . ! ?  (never after , ; or colon-mode)
                        if (IsOpeningQuote(nx) && j + 1 < n && IsWordChar(s[j + 1]))
                        {
                            bool allowTight = (c == '.' || c == '!' || c == '?');
                            if ((isColon && treatColonAsSentencePunct) || c == ',' || c == ';' || !allowTight)
                            {
                                addSpace = true; // space before opening quote
                            }
                            else
                            {
                                sb.Append(nx);   // tight form: ."Yes"
                                j++;
                                addSpace = false;
                            }
                        }
                        else if (!char.IsWhiteSpace(nx) && !IsSentencePunct(nx))
                        {
                            // next is a “wordish” thing — insert exactly one space
                            addSpace = true;
                        }
                    }
                    if (addSpace) sb.Append(' ');
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

        // Treat " and ' as closing quotes when followed by whitespace, sentence punctuation, another closer, or end-of-text.
        private static bool IsLikelyClosingStraightQuote(string s, int j)
        {
            if (j >= s.Length) return false;
            char q = s[j];
            if (q != '"' && q != '\'') return false;
            int k = j + 1;
            if (k >= s.Length) return true; // end-of-text → closing
            char nx = s[k];
            return char.IsWhiteSpace(nx) || IsSentencePunct(nx) || IsClosing(nx);
        }

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
                end = ScanPathLikeEnd(s, i + 3);   // <— was ScanUrlLikeEnd
                return true;
            }
            return false;
        }

        private static bool TryConsumeNixPath(string s, int i, out int end)
        {
            end = 0;
            if (s[i] == '/' && !(i + 1 < s.Length && (s[i + 1] == '/' || s[i + 1] == ' ' || s[i + 1] == '\n')))
            {
                end = ScanPathLikeEnd(s, i + 1);   // <— was ScanUrlLikeEnd
                return true;
            }
            return false;
        }

        private static int ScanPathLikeEnd(string s, int start)
        {
            int j = start, n = s.Length;
            while (j < n)
            {
                char ch = s[j];
                if (char.IsWhiteSpace(ch)) break;
                if (ch == ',' || ch == ';' || ch == '!' || ch == '?' || ch == ':' ||
                    ch == ')' || ch == ']' || ch == '}' ||
                    ch == '"' || ch == '\'' || ch == '»' || ch == '”' || ch == '’' || ch == '…')
                    break;
                j++;
            }
            return j;
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
            int n = s.Length;

            // Require a sensible left boundary (start/whitespace/opener/punct).
            if (i > 0)
            {
                char prev = s[i - 1];
                if (char.IsLetterOrDigit(prev) || prev == '_' || prev == '-')
                    return false;
            }

            int j = i;
            if (!IsDomainLabelStart(s[j])) return false;

            int dots = 0, labelLen = 0, lastLabelLen = 0;
            while (j < n && (char.IsLetterOrDigit(s[j]) || s[j] == '-' || s[j] == '.'))
            {
                if (s[j] == '.')
                {
                    if (labelLen == 0) return false; // empty label like "example..com"
                    dots++; lastLabelLen = labelLen; labelLen = 0; j++;
                }
                else { labelLen++; j++; }
            }
            if (dots == 0 || labelLen == 0) return false;
            lastLabelLen = labelLen;

            // TLD policy:
            //  - Accept if TLD is all lowercase letters (len>=2)  → typical "example.com"
            //  - OR accept if TLD is all UPPERCASE AND the WHOLE domain token is ALL UPPERCASE
            //    (labels may include digits and '-'; any lowercase anywhere rejects)
            bool tldAllLower = true, tldAllUpper = true;
            for (int k = j - lastLabelLen; k < j; k++)
            {
                char ch = s[k];
                if (!char.IsLetter(ch)) { tldAllLower = tldAllUpper = false; break; }
                if (!char.IsLower(ch)) tldAllLower = false;
                if (!char.IsUpper(ch)) tldAllUpper = false;
            }
            if (lastLabelLen < 2) return false;

            bool domainAllUpper = true;
            for (int k = i; k < j; k++)
            {
                char ch = s[k];
                if (char.IsLetter(ch) && !char.IsUpper(ch)) { domainAllUpper = false; break; }
                // digits, '.', '-' are fine
            }

            if (!(tldAllLower || (tldAllUpper && domainAllUpper)))
                return false;

            // Right boundary must be sensible (end/whitespace/closer/punct).
            if (j < n)
            {
                char next = s[j];
                if (char.IsLetterOrDigit(next) || next == '_' || next == '-')
                    return false;
            }

            end = j;
            return true;
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

        // Accepts IPv6 (including leading '::' compression) and IPv6 with embedded IPv4
        // (::ffff:192.0.2.128 and ::192.0.2.128). We DO NOT consume trailing sentence punctuation.
        private static bool TryConsumeIPv6(string s, int i, out int end)
        {
            end = 0;
            int n = s.Length, j = i;
            bool seenDouble = false;   // saw '::'
            int groups = 0;            // number of h16 groups read
            bool ipv4Tail = false;     // read an embedded IPv4 dotted-quad

            bool ReadH16(ref int k)
            {
                int start = k, cnt = 0;
                while (k < n && cnt < 4 && IsHex(s[k])) { k++; cnt++; }
                return cnt > 0; // 1..4 hex
            }

            bool TryReadIPv4(ref int k)
            {
                int start = k;
                for (int oct = 0; oct < 4; oct++)
                {
                    int digits = 0;
                    while (k < n && digits < 3 && char.IsDigit(s[k])) { k++; digits++; }
                    if (digits == 0) { k = start; return false; }
                    if (oct < 3)
                    {
                        if (k >= n || s[k] != '.') { k = start; return false; }
                        k++; // consume '.'
                    }
                }
                return true;
            }

            // ---- start: either '::' or an initial h16
            if (j + 1 < n && s[j] == ':' && s[j + 1] == ':')
            {
                seenDouble = true;
                j += 2;

                // Allow an immediate IPv4 tail after '::'  → ::192.0.2.128
                int k0 = j;
                if (TryReadIPv4(ref k0))
                {
                    ipv4Tail = true;
                    j = k0;
                }
                else
                {
                    // Or allow an immediate h16 group  → ::ffff
                    int k1 = j;
                    if (ReadH16(ref k1))
                    {
                        groups++;
                        j = k1;
                    }
                }
            }
            else
            {
                if (!ReadH16(ref j)) return false;
                groups++;
            }

            while (true)
            {
                if (j >= n)
                {
                    end = j;
                    return groups >= 2 || seenDouble || ipv4Tail;
                }

                char ch = s[j];

                // prose terminators (do not consume)
                if (char.IsWhiteSpace(ch) || ch == '.' || ch == ',' || ch == '!' || ch == '?' ||
                    ch == ';' || ch == ')' || ch == ']' || ch == '}' || ch == '"' || ch == '\'' ||
                    ch == '»' || ch == '”' || ch == '’')
                {
                    end = j;
                    return groups >= 2 || seenDouble || ipv4Tail;
                }

                if (ch == ':')
                {
                    // double-colon compression inside address
                    if (j + 1 < n && s[j + 1] == ':')
                    {
                        if (seenDouble)
                        {
                            // can't have '::' twice; stop before this pair
                            end = j;
                            return groups >= 2 || ipv4Tail;
                        }
                        seenDouble = true;
                        j += 2;

                        // After '::', allow immediate IPv4 or h16
                        int k2 = j;
                        if (TryReadIPv4(ref k2))
                        {
                            ipv4Tail = true;
                            j = k2;
                            continue;
                        }
                        int k3 = j;
                        if (ReadH16(ref k3))
                        {
                            groups++;
                            j = k3;
                            continue;
                        }
                        // allow nothing immediately; next loop will handle terminator or further parts
                        continue;
                    }

                    // single colon: expect h16 or an embedded IPv4 tail
                    j++; // consume ':'

                    int k = j;
                    if (TryReadIPv4(ref k))
                    {
                        ipv4Tail = true;
                        j = k;
                        continue;
                    }

                    if (ReadH16(ref j))
                    {
                        groups++;
                        continue;
                    }

                    return false; // colon not followed by valid chunk
                }

                // unexpected char inside IPv6
                return false;
            }
        }

        private static bool IsHex(char c) =>
            (c >= '0' && c <= '9') ||
            (c >= 'a' && c <= 'f') ||
            (c >= 'A' && c <= 'F');


        private static bool IsSentencePunct(char c) => c == '.' || c == ',' || c == '!' || c == '?' || c == ';' || c == '。' || c == '！' || c == '？';
        private static bool IsClosing(char c) => c == ')' || c == ']' || c == '}' || c == '»' || c == '”' || c == '’';
        private static bool IsOpeningQuote(char c) => c == '"' || c == '\'' || c == '“' || c == '‘' || c == '«';
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
            int i = 0, n = s.Length;

            while (i < n)
            {
                char c = s[i];

                if (c == ' ')
                {
                    // Consume a run of spaces
                    int j = i;
                    while (j < n && s[j] == ' ') j++;

                    // Look at the next non-space char
                    char next = j < n ? s[j] : '\0';

                    // If the run is immediately before a newline or end-of-text, DROP the spaces entirely.
                    if (next == '\n' || next == '\0')
                    {
                        // no append
                    }
                    else
                    {
                        // Otherwise keep exactly one space
                        sb.Append(' ');
                    }

                    i = j;
                    continue;
                }

                sb.Append(c);
                i++;
            }

            return sb.ToString();
        }

        // Trim trailing punctuation that typically does NOT belong to URLs/paths in prose.
        private static bool IsUrlTerminalPunct(char c) =>
            c == '.' || c == ',' || c == '!' || c == '?' || c == ';' || c == ':' ||
            c == '。' || c == '！' || c == '？' ||
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
        // Replace your existing ScanUrlLikeEnd with this version
        private static int ScanUrlLikeEnd(string s, int start)
        {
            int n = s.Length;
            int j = start;

            bool inBracketHost = false; // handle http://[2001:db8::1]:443/...
            while (j < n && !char.IsWhiteSpace(s[j]))
            {
                char ch = s[j];

                // bracketed IPv6 host: consume until matching ']' (no early-stops inside)
                if (!inBracketHost && ch == '[')
                { inBracketHost = true; j++; continue; }
                if (inBracketHost)
                {
                    if (ch == ']') inBracketHost = false;
                    j++; continue;
                }

                // prose delimiters right after a URL
                if (ch == ',' && j + 1 < n && (char.IsLetter(s[j + 1]) || IsOpeningQuote(s[j + 1]))) break;
                if (ch == ';' && j + 1 < n && (char.IsLetter(s[j + 1]) || IsOpeningQuote(s[j + 1]))) break;

                // sentence boundary patterns: DIGIT '.' LETTER  or  CLOSER '.' LETTER
                if (ch == '.' && j + 1 < n && char.IsLetter(s[j + 1]))
                {
                    bool prevIsDigit = j > start && char.IsDigit(s[j - 1]);
                    bool prevIsCloser = j > start && (s[j - 1] == ')' || s[j - 1] == ']' || s[j - 1] == '}' ||
                                                      s[j - 1] == '"' || s[j - 1] == '\'' || s[j - 1] == '»' ||
                                                      s[j - 1] == '”' || s[j - 1] == '’');
                    if (prevIsDigit || prevIsCloser) break; // stop BEFORE the '.'
                }

                j++;
            }

            // Trim trailing punctuation that clings to URLs, but keep balancing closers
            int k = j;
            while (k > start && IsUrlTerminalPunct(s[k - 1]))
            {
                char tail = s[k - 1];
                if ((tail == ')' && HasUnmatchedOpening(s.AsSpan(start, k - start - 1), '(', ')')) ||
                    (tail == ']' && HasUnmatchedOpening(s.AsSpan(start, k - start - 1), '[', ']')) ||
                    (tail == '}' && HasUnmatchedOpening(s.AsSpan(start, k - start - 1), '{', '}')))
                {
                    break; // keep balancing closer
                }
                k--;
            }

            return k;
        }
    }
}

using System.Globalization;
using System.Text.RegularExpressions;

namespace TidyText.Model;

internal static class Cleaner
{
    public static string RemoveMultipleSpaces(string text) => Regex.Replace(text, @"[ ]{2,}", " ");

    public static string RemoveMultipleLines(string text) => Regex.Replace(text, @"(\n\s*){2,}", "\n\n");

    public static string RemoveAllLineBreaks(string text) => Regex.Replace(text, @"\r\n?|\n", "");

    public static string FixSpacesAfterPuntuation(string text) => Regex.Replace(text, @"(?<=[^\s—–])\s*(\p{P})(?<!-)\s*", "$1 ");

    // Letter case related methods
    public static string ConvertToSentenceCase(string text)
    {
        string[] sentences = Regex.Split(text, @"(?<=[\.!\?])\s+");

        for (int i = 0; i < sentences.Length; i++)
        {
            string sentence = sentences[i];

            if (!string.IsNullOrEmpty(sentence))
            {
                sentence = char.ToUpper(sentence[0]) + sentence.Substring(1).ToLower(CultureInfo.CurrentCulture);
            }

            sentences[i] = sentence;
        }
        return string.Join(" ", sentences);
    }

    public static string ConvertToTitleCase(string text) => new CultureInfo("en-US", false).TextInfo.ToTitleCase(text);
}
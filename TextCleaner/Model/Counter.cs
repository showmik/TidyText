using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace TidyText.Model;

public static class Counter
{
    public static int CountWords(string text) => text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

    public static int CountCharacters(string text) => text.Length;

    public static int CountParagraphs(string text) => Regex.Split(text, @"\n+").Count(paragraph => !string.IsNullOrWhiteSpace(paragraph));

    public static int CountLineBreaks(string text) => text.Split('\n').Length;

    public static int CountSentences(string text)
    {
        string[] sentences = Regex.Split(text, @"(?<=[.!?])\s+");
        int count = 0;

        foreach (string sentence in sentences)
        {
            if (!string.IsNullOrWhiteSpace(sentence)) { count++; }
        }
        return count;
    }
}
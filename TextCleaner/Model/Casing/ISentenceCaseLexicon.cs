// TidyText/Model/Casing/ISentenceCaseLexicon.cs
using System.Collections.Generic;

namespace TidyText.Model.Casing
{
    /// <summary>Wordlists and tokens that guide sentence casing.</summary>
    public interface ISentenceCaseLexicon
    {
        ISet<string> NonTerminalAbbreviations { get; } // e.g., "a.m", "u.s.a"
        ISet<string> UpperShortStopwords { get; }      // "TO/IN/OF/..." → never treat as acronyms
        ISet<string> UpperAcronyms { get; }            // explicit ALL-CAPS acronyms to keep (NASA, GPT, …)
        ISet<string> ProperCaseTokens { get; }         // names/brands to TitleCase mid-sentence
        ISet<string> BrandTokens { get; }              // tokens that mark model families (Gemini, iPhone, …)
        ISet<string> BrandSuffixes { get; }            // Pro/Max/Ultra/Plus/Mini → TitleCase w/ brand or digits
    }
}
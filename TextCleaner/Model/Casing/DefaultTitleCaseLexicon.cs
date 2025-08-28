// TidyText/Model/Casing/DefaultTitleCaseLexicon.cs
using System;
using System.Collections.Generic;

namespace TidyText.Model.Casing
{
    /// <summary>Default AP-ish title lexicon. Easy to extend via PRs.</summary>
    public sealed class DefaultTitleCaseLexicon : ITitleCaseLexicon
    {
        public static ITitleCaseLexicon Instance { get; } = new DefaultTitleCaseLexicon();
        private DefaultTitleCaseLexicon() { }

        // AP rule: lowercase articles; coordinating conjunctions; and prepositions of 3 letters or fewer.
        // (Not verbs/adverbs: “Is/Are/Be/Not/Yet” should be capitalized.)
        public ISet<string> SmallWords { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a","an","the",
            "and","but","for","nor","or","so","yet",
            "as","at","by","in","of","off","on","per","to","up","via",
            "vs" // <- keep “vs.” lowercase
        };

        public ISet<string> ProtectedAsIs { get; } = new HashSet<string>(StringComparer.Ordinal)
        {
            // Put trademarks/special tokens here if they must not be touched.
            // (Most cases are handled heuristically; keep this minimal.)
        };
    }
}

// TidyText/Model/Casing/DefaultSentenceCaseLexicon.cs
using System;
using System.Collections.Generic;

namespace TidyText.Model.Casing
{
    /// <summary>Default, immutable lexicon. Easy to extend via PRs or by swapping implementations.</summary>
    public sealed class DefaultSentenceCaseLexicon : ISentenceCaseLexicon
    {
        public static ISentenceCaseLexicon Instance { get; } = new DefaultSentenceCaseLexicon();

        private DefaultSentenceCaseLexicon() { }

        public ISet<string> NonTerminalAbbreviations { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "mr","mrs","ms","dr","prof","sr","jr","st",
            "vs","v","etc","e.g","eg","i.e","ie",
            "a.m","am","p.m","pm",
            "u.s","u.s.a","us","usa","u.k","uk","u.n","un",
            "ai", "a.i."
        };

        public ISet<string> UpperShortStopwords { get; } = new HashSet<string>(StringComparer.Ordinal)
        {
            // articles, conjunctions, prepositions (existing)
            "A","AN","THE","AND","OR","NOR","BUT","SO",
            "TO","IN","ON","AT","OF","BY","AS",
            "IS","AM","ARE","WAS","WERE","BE","BEEN",
            "DO","DID","DONE",
            "FOR","FROM","WITH","WITHOUT","OVER","UNDER",
            "OUT","OFF","UP","DOWN",
            "NEW","ALL","ANY","NOT","ONE","TWO",

            // pronouns & determiners
            "I","ME","MY","YOU","YOUR","WE","US","OUR",
            "HE","HIM","HIS","SHE","HER","IT","ITS",
            "THEY","THEM","THEIR","THIS","THAT","THESE","THOSE",

            // comparatives / conditionals / misc short words
            "IF","THAN","THEN","PER","ET","AL",

            // common modals/auxiliaries (≤3)
            "CAN","MAY","HAS","HAD"
        };

        public ISet<string> UpperAcronyms { get; } = new HashSet<string>(StringComparer.Ordinal)
        {
            // 2–3 letters
            "AI","ML","API","SDK","CLI","UI","UX","ID","IP","DNS","TCP","UDP","SSL","TLS","SSH",
            "CPU","GPU","RAM","ROM","SSD","HDD","USB","WPF","GPT","USA","UK","EU","UN","UAE","SLS",
            // trusted 4+ letters
            "NASA","HTTP","HTTPS","HTML","JSON","XML","SQL","UUID","GUID","JPEG","PNG","WASM","WLAN","SSID"
        };

        public ISet<string> ProperCaseTokens { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // seed with what you need; contributors can expand
            "Claude","Sonnet","Gemini"
        };

        public ISet<string> BrandTokens { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Claude","Gemini","iPhone","iPad","Pixel","Galaxy","MacBook","ThinkPad"
        };

        public ISet<string> BrandSuffixes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Pro","Max","Ultra","Plus","Mini"
        };
    }
}

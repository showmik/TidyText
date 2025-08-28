// TidyText/Model/Casing/ITitleCaseLexicon.cs
using System.Collections.Generic;

namespace TidyText.Model.Casing
{
    /// <summary>Word lists that guide AP-style Title Case.</summary>
    public interface ITitleCaseLexicon
    {
        /// <summary>“Small words” to lowercase unless they’re the first/last word or forced-cap (e.g., after a colon).</summary>
        ISet<string> SmallWords { get; }

        /// <summary>Tokens to preserve as-is (symbols, trademarks, special brands if you want exact casing).</summary>
        ISet<string> ProtectedAsIs { get; }
    }
}
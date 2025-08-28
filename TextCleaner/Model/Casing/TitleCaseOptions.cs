// TidyText/Model/Casing/TitleCaseOptions.cs
using System.Globalization;

namespace TidyText.Model.Casing
{
    public sealed class TitleCaseOptions
    {
        public CultureInfo Culture { get; init; } = CultureInfo.CurrentCulture;

        /// <summary>Capitalize the first word after a colon. AP headline style: YES.</summary>
        public bool CapitalizeAfterColon { get; init; } = true;

        /// <summary>Capitalize first and last word of the title regardless of small-words list.</summary>
        public bool ForceCapFirstAndLast { get; init; } = true;

        /// <summary>Capitalize significant parts in hyphenated compounds. (Small words inside remain lowercase.)</summary>
        public bool CapitalizeHyphenatedSegments { get; init; } = true;

        /// <summary>Preserve known acronyms from the sentence lexicon (GPU/GPT/NASA) even if surrounded by small words.</summary>
        public bool PreserveAcronyms { get; init; } = true;

        /// <summary>Preserve tokens detected as camel/mixed case (iPhone, eBay, macOS, McDonald’s).</summary>
        public bool PreserveCamelOrMixedCase { get; init; } = true;

        public bool UppercaseSingleLetterWords { get; init; } = true;
    }
}

// TidyText/Model/Casing/SentenceCaseOptions.cs
using System.Globalization;

namespace TidyText.Model.Casing
{
    public sealed class SentenceCaseOptions
    {
        public CultureInfo Culture { get; init; } = CultureInfo.CurrentCulture;

        /// <summary>Always preserve acronyms mid-sentence (CPU/GPU/GPT)? If false, only whitelist stays ALL-CAPS.</summary>
        public bool PreserveAcronymsMidSentence { get; init; } = true;
        public bool TreatUnknownShortAllCapsAsAcronym { get; init; } = false;
    }
}

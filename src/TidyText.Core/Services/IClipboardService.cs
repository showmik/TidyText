namespace TidyText.Core.Services
{
    /// <summary>
    /// Abstracts clipboard access so ViewModels don't depend
    /// on the WPF Clipboard static class directly.
    /// </summary>
    public interface IClipboardService
    {
        void SetText(string text);
    }
}

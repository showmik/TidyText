using System.Windows;
using TidyText.Core.Services;

namespace TidyText.App.Services
{
    /// <summary>
    /// WPF implementation of IClipboardService.
    /// This is the only place in the entire solution that touches
    /// System.Windows.Clipboard, keeping the ViewModel layer clean.
    /// </summary>
    public class WpfClipboardService : IClipboardService
    {
        public void SetText(string text)
        {
            Clipboard.SetText(text);
        }
    }
}

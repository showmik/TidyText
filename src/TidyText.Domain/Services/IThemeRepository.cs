namespace TidyText.Domain.Services
{
    /// <summary>
    /// Abstracts theme persistence to remove File I/O from the UI.
    /// </summary>
    public interface IThemeRepository
    {
        string LoadTheme();
        void SaveTheme(string themeName);
    }
}

using System.IO;
using TidyText.Domain.Services;

namespace TidyText.Infrastructure.Persistence
{
    public class FileThemeRepository : IThemeRepository
    {
        private readonly string _filePath;

        public FileThemeRepository(string appDataFolderPath)
        {
            _filePath = Path.Combine(appDataFolderPath, "theme.txt");
        }

        public string LoadTheme()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    return File.ReadAllText(_filePath).Trim();
                }
            }
            catch { }
            return "Dark"; // default
        }

        public void SaveTheme(string themeName)
        {
            try
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(_filePath, themeName);
            }
            catch { }
        }
    }
}

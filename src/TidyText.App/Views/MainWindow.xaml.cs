using System.Windows;
using TidyText.App.ViewModels;
using TidyText.Core.Security;

namespace TidyText.App.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadTheme();
        }

        private string GetThemeFilePath() => System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "TidyText", "theme.txt");

        private void LoadTheme()
        {
            try
            {
                var path = GetThemeFilePath();
                if (System.IO.File.Exists(path))
                {
                    var theme = System.IO.File.ReadAllText(path).Trim();
                    if (theme == "Light")
                    {
                        _isDarkTheme = false;
                        ApplyTheme(save: false);
                    }
                }
            }
            catch { /* Ignore errors on load */ }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Resolve the shared ISecureKeyVault from the App-level composition
            // instead of creating a new concrete SecureKeyVault instance.
            var app = (App)Application.Current;
            var keyVault = app.KeyVault;

            var settingsWindow = new SettingsWindow
            {
                Owner = this,
                DataContext = new ViewModels.SettingsViewModel(keyVault)
            };
            settingsWindow.ShowDialog();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        
        private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        
        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private bool _isDarkTheme = true;
        
        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;
            ApplyTheme(save: true);
        }

        private void ApplyTheme(bool save)
        {
            var newTheme = new ResourceDictionary { Source = new System.Uri($"Themes/{(_isDarkTheme ? "DarkTheme" : "LightTheme")}.xaml", System.UriKind.Relative) };
            
            var appResources = Application.Current.Resources.MergedDictionaries;
            appResources.RemoveAt(0);
            appResources.Insert(0, newTheme);

            if (save)
            {
                try
                {
                    var path = GetThemeFilePath();
                    var dir = System.IO.Path.GetDirectoryName(path);
                    if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir!);
                    System.IO.File.WriteAllText(path, _isDarkTheme ? "Dark" : "Light");
                }
                catch { /* Ignore errors on save */ }
            }
        }

    }
}

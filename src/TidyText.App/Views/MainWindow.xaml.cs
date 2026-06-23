using System.Windows;
using TidyText.App.ViewModels;
using TidyText.Domain.Security;
using TidyText.Domain.Services;

namespace TidyText.App.Views
{
    public partial class MainWindow : Window
    {
        private readonly IThemeRepository _themeRepository;
        private bool _isDarkTheme = true;

        public MainWindow(IThemeRepository themeRepository)
        {
            _themeRepository = themeRepository;
            InitializeComponent();
            LoadTheme();
        }

        private void LoadTheme()
        {
            var theme = _themeRepository.LoadTheme();
            if (theme == "Light")
            {
                _isDarkTheme = false;
                ApplyTheme(save: false);
            }
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
                _themeRepository.SaveTheme(_isDarkTheme ? "Dark" : "Light");
            }
        }

    }
}

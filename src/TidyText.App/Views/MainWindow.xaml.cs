using System.Windows;
using TidyText.App.ViewModels;

namespace TidyText.App.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow
            {
                Owner = this,
                DataContext = new ViewModels.SettingsViewModel(new Core.Security.SecureKeyVault(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "TidyText")))
            };
            settingsWindow.ShowDialog();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        
        private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        
        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}

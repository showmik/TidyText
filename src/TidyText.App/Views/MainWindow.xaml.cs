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
    }
}

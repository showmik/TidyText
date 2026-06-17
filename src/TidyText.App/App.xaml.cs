using System;
using System.Net.Http;
using System.Windows;
using TidyText.App.ViewModels;
using TidyText.App.Views;
using TidyText.Core.AI;
using TidyText.Core.AI.Providers;
using TidyText.Core.Security;

namespace TidyText.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Dependency Injection Setup (simplified for immediate wire-up)
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string tidyTextDataPath = System.IO.Path.Combine(appData, "TidyText");
            
            var keyVault = new SecureKeyVault(tidyTextDataPath);
            
            var httpClient = new HttpClient();
            var providers = new IAIProvider[]
            {
                new OllamaProvider(httpClient),
                new GeminiProvider(keyVault.GetKey("Gemini"), httpClient),
                new OpenAIProvider(keyVault.GetKey("OpenAI"), httpClient),
                new DeepSeekProvider(keyVault.GetKey("DeepSeek"), httpClient),
                new AnthropicProvider(keyVault.GetKey("Anthropic"), httpClient)
            };
            
            var aiRouter = new AIProviderRouter(providers);

            var mainViewModel = new MainViewModel();
            var aiViewModel = new AIAssistantViewModel(aiRouter, mainViewModel, keyVault);
            var settingsViewModel = new SettingsViewModel(keyVault);

            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
            
            // Assuming AIAssistantPanel DataContext is bound in XAML or passed here
            mainViewModel.AIAssistantVM = aiViewModel;

            mainWindow.Show();
        }
    }
}

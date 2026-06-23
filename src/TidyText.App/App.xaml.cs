using System;
using CommunityToolkit.Mvvm.Messaging;
using System.Net.Http;
using System.Windows;
using TidyText.App.Services;
using TidyText.App.ViewModels;
using TidyText.App.Views;
using TidyText.Domain.AI;
using TidyText.Infrastructure.AI.Providers;
using TidyText.Domain.Security;
using TidyText.Infrastructure.Security;
using TidyText.Domain.Services;

namespace TidyText.App
{
    public partial class App : Application
    {
        /// <summary>
        /// Shared key vault instance — exposed so Views can resolve it
        /// without creating duplicate SecureKeyVault instances.
        /// </summary>
        public ISecureKeyVault KeyVault { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ── Composition Root ─────────────────────────────────────
            // All interface → implementation wiring happens here and
            // ONLY here. No other class in the solution calls new() on
            // an infrastructure type.

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string tidyTextDataPath = System.IO.Path.Combine(appData, "TidyText");
            
            // Infrastructure services
            ISecureKeyVault keyVault = new SecureKeyVault(tidyTextDataPath);
            KeyVault = keyVault;
            IClipboardService clipboard = new WpfClipboardService();
            IAIHistoryRepository historyRepository = new JsonAIHistoryRepository(tidyTextDataPath);
            
            var httpClient = new HttpClient();
            var providers = new IAIProvider[]
            {
                new OllamaProvider(httpClient),
                new LocalLMProvider(httpClient, keyVault),
                new GeminiProvider(keyVault.GetKey("Gemini"), httpClient),
                new OpenAIProvider(keyVault.GetKey("OpenAI"), httpClient),
                new DeepSeekProvider(keyVault.GetKey("DeepSeek"), httpClient),
                new AnthropicProvider(keyVault.GetKey("Anthropic"), httpClient)
            };
            
            IAIProviderRouter aiRouter = new AIProviderRouter(providers);

            // ViewModels
            IMessenger messenger = WeakReferenceMessenger.Default;
            IUndoRedoService undoRedoService = new UndoRedoService();
            var templateProviders = new[] { new TidyText.Domain.AI.Templates.BuiltInTemplateProvider() };
            var aiViewModel = new AIAssistantViewModel(aiRouter, messenger, keyVault, historyRepository, templateProviders);
            var mainViewModel = new MainViewModel(clipboard, undoRedoService, messenger, aiViewModel);
            var settingsViewModel = new SettingsViewModel(keyVault);

            IThemeRepository themeRepository = new TidyText.Infrastructure.Persistence.FileThemeRepository(tidyTextDataPath);

            var mainWindow = new MainWindow(themeRepository)
            {
                DataContext = mainViewModel
            };
            
            // Note: AIAssistantPanel DataContext should be bound via DI or a Locator in XAML now, 
            // but for simplicity we assume it's set properly or the view resolves it.

            mainWindow.Show();
        }
    }
}

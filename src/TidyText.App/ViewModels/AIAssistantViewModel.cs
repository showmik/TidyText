using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TidyText.Core.AI;
using TidyText.Core.AI.Templates;
using TidyText.Core.Security;

namespace TidyText.App.ViewModels
{
    public partial class AIAssistantViewModel : ObservableObject
    {
        private readonly AIProviderRouter _router;
        private readonly MainViewModel _mainViewModel;
        private readonly SecureKeyVault _keyVault;
        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty]
        private ObservableCollection<IPromptTemplate> _templates = new();

        [ObservableProperty]
        private IPromptTemplate? _selectedTemplate;

        [ObservableProperty]
        private ObservableCollection<string> _availableProviders = new();

        [ObservableProperty]
        private string _activeProviderName = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _availableModels = new();

        [ObservableProperty]
        private string _activeModel = string.Empty;

        [ObservableProperty]
        private string _responseContent = string.Empty;

        [ObservableProperty]
        private bool _isProcessing = false;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        public AIAssistantViewModel(AIProviderRouter router, MainViewModel mainViewModel, SecureKeyVault keyVault)
        {
            _router = router;
            _mainViewModel = mainViewModel;
            _keyVault = keyVault;
            
            var engine = new PromptTemplateEngine();
            foreach (var template in engine.GetBuiltInTemplates())
            {
                Templates.Add(template);
            }

            AvailableProviders.Add("Gemini");
            AvailableProviders.Add("OpenAI");
            AvailableProviders.Add("DeepSeek");
            AvailableProviders.Add("Anthropic");

            var savedProvider = _keyVault.GetKey("ActiveProviderName");
            if (!string.IsNullOrEmpty(savedProvider) && AvailableProviders.Contains(savedProvider))
            {
                ActiveProviderName = savedProvider;
            }
            else
            {
                ActiveProviderName = "Gemini";
            }
        }

        private void UpdateAvailableModels(string provider)
        {
            AvailableModels.Clear();
            switch (provider)
            {
                case "Gemini":
                    AvailableModels.Add("gemini-3.5-flash");
                    AvailableModels.Add("gemini-3.5-pro");
                    AvailableModels.Add("gemini-3.1-pro");
                    AvailableModels.Add("gemini-3.1-flash-lite");
                    break;
                case "OpenAI":
                    AvailableModels.Add("gpt-5.5-instant");
                    AvailableModels.Add("gpt-5.5-pro");
                    AvailableModels.Add("gpt-5.4-mini");
                    AvailableModels.Add("gpt-5.4-pro");
                    AvailableModels.Add("gpt-5.4-nano");
                    break;
                case "Anthropic":
                    AvailableModels.Add("claude-fable-5");
                    AvailableModels.Add("claude-opus-4.8");
                    AvailableModels.Add("claude-sonnet-4.6");
                    AvailableModels.Add("claude-haiku-4.5");
                    break;
                case "DeepSeek":
                    AvailableModels.Add("deepseek-v4-pro");
                    AvailableModels.Add("deepseek-v4-flash");
                    break;
            }

            var savedModel = _keyVault?.GetKey($"ActiveModel_{provider}");
            if (!string.IsNullOrEmpty(savedModel) && AvailableModels.Contains(savedModel))
            {
                ActiveModel = savedModel;
            }
            else if (AvailableModels.Count > 0)
            {
                ActiveModel = AvailableModels[0];
            }
        }

        partial void OnActiveProviderNameChanged(string value)
        {
            if (_keyVault != null)
            {
                _keyVault.SetKey("ActiveProviderName", value);
                UpdateAvailableModels(value);
            }
        }

        partial void OnActiveModelChanged(string value)
        {
            if (_keyVault != null && !string.IsNullOrEmpty(ActiveProviderName) && !string.IsNullOrEmpty(value))
            {
                _keyVault.SetKey($"ActiveModel_{ActiveProviderName}", value);
            }
        }

        [RelayCommand]
        public async Task ExecuteTemplateAsync(IPromptTemplate template)
        {
            if (template == null || string.IsNullOrWhiteSpace(_mainViewModel.MainText)) return;

            SelectedTemplate = template;
            IsProcessing = true;
            StatusMessage = "Processing with " + ActiveProviderName + "...";
            ResponseContent = string.Empty;

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            _cancellationTokenSource = new CancellationTokenSource();

            var prompt = template.GetPrompt(_mainViewModel.MainText);
            var options = new AIOptions 
            { 
                SystemPrompt = template.SystemPrompt,
                Temperature = 0.4,
                Model = ActiveModel
            };

            try
            {
                var response = await _router.RouteAsync(ActiveProviderName, prompt, options, _cancellationTokenSource.Token);

                if (response.IsError)
                {
                    StatusMessage = "Error occurred.";
                    ResponseContent = response.ErrorMessage;
                }
                else
                {
                    StatusMessage = "Done.";
                    ResponseContent = response.Text;
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Cancelled.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error occurred.";
                ResponseContent = ex.Message;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        public void CancelProcessing()
        {
            _cancellationTokenSource?.Cancel();
        }

        [RelayCommand]
        public void ApplyResponseToEditor()
        {
            if (!string.IsNullOrWhiteSpace(ResponseContent) && StatusMessage == "Done.")
            {
                _mainViewModel.MainText = ResponseContent;
            }
        }
    }
}

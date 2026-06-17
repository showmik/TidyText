using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TidyText.Core.AI;
using TidyText.Core.AI.Templates;

namespace TidyText.App.ViewModels
{
    public partial class AIAssistantViewModel : ObservableObject
    {
        private readonly AIProviderRouter _router;
        private readonly MainViewModel _mainViewModel;
        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty]
        private ObservableCollection<IPromptTemplate> _templates = new();

        [ObservableProperty]
        private IPromptTemplate? _selectedTemplate;

        [ObservableProperty]
        private string _activeProviderName = "Gemini";

        [ObservableProperty]
        private string _responseContent = string.Empty;

        [ObservableProperty]
        private bool _isProcessing = false;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        public AIAssistantViewModel(AIProviderRouter router, MainViewModel mainViewModel)
        {
            _router = router;
            _mainViewModel = mainViewModel;
            
            var engine = new PromptTemplateEngine();
            foreach (var template in engine.GetBuiltInTemplates())
            {
                Templates.Add(template);
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
                Temperature = 0.4
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

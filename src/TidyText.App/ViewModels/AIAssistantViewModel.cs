using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using TidyText.Domain.AI;
using TidyText.Domain.AI.Templates;
using TidyText.Domain.Security;

namespace TidyText.App.ViewModels
{
    public partial class AIAssistantViewModel : ObservableObject
    {
        private readonly IAIProviderRouter _router;
        private readonly MainViewModel _mainViewModel;
        private readonly ISecureKeyVault _keyVault;
        private readonly IAIHistoryRepository _historyRepository;
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
        private string _customPrompt = string.Empty;

        [ObservableProperty]
        private ObservableCollection<AIHistoryItem> _history = new();

        [ObservableProperty]
        private bool _isReviewing = false;

        [ObservableProperty]
        private ObservableCollection<DiffChunk> _diffChunks = new();

        private string _proposedText = string.Empty;
        private string _currentPromptOrTemplate = string.Empty;

        [ObservableProperty]
        private bool _isProcessing = false;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        public AIAssistantViewModel(
            IAIProviderRouter router,
            MainViewModel mainViewModel,
            ISecureKeyVault keyVault,
            IAIHistoryRepository historyRepository)
        {
            _router = router;
            _mainViewModel = mainViewModel;
            _keyVault = keyVault;
            _historyRepository = historyRepository;
            
            LoadHistory();
            
            var engine = new PromptTemplateEngine();
            foreach (var template in engine.GetBuiltInTemplates())
            {
                Templates.Add(template);
            }

            AvailableProviders.Add("Gemini");
            AvailableProviders.Add("OpenAI");
            AvailableProviders.Add("DeepSeek");
            AvailableProviders.Add("Anthropic");
            AvailableProviders.Add("Ollama");
            AvailableProviders.Add("Local LM");

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
                case "Ollama":
                    AvailableModels.Add("llama3");
                    AvailableModels.Add("phi3");
                    AvailableModels.Add("mistral");
                    AvailableModels.Add("gemma2");
                    break;
                case "Local LM":
                    AvailableModels.Add("local-model");
                    AvailableModels.Add("lmstudio-default");
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
                    StatusMessage = "Error occurred: " + response.ErrorMessage;
                }
                else
                {
                    StatusMessage = "Reviewing proposed changes...";
                    _proposedText = response.Text;
                    _currentPromptOrTemplate = template.Name;
                    GenerateDiff(_mainViewModel.MainText, _proposedText);
                    IsReviewing = true;
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Cancelled.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error occurred: " + ex.Message;
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
        public async Task ExecuteCustomPromptAsync()
        {
            if (string.IsNullOrWhiteSpace(CustomPrompt) || string.IsNullOrWhiteSpace(_mainViewModel.MainText)) return;

            IsProcessing = true;
            StatusMessage = "Processing custom prompt with " + ActiveProviderName + "...";

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            _cancellationTokenSource = new CancellationTokenSource();

            // The template essentially just prepends the custom prompt to the text
            var prompt = $"{CustomPrompt}\n\n<text>\n{_mainViewModel.MainText}\n</text>";
            var options = new AIOptions 
            { 
                Temperature = 0.5,
                Model = ActiveModel,
                SystemPrompt = "You are a highly capable AI assistant embedded in a text editor. Execute the user's instructions on the provided text. Return ONLY the modified text without conversational filler, pleasantries, or markdown formatting, unless specifically requested."
            };

            try
            {
                var response = await _router.RouteAsync(ActiveProviderName, prompt, options, _cancellationTokenSource.Token);

                if (response.IsError)
                {
                    StatusMessage = "Error occurred: " + response.ErrorMessage;
                }
                else
                {
                    StatusMessage = "Reviewing proposed changes...";
                    _proposedText = response.Text;
                    _currentPromptOrTemplate = CustomPrompt;
                    GenerateDiff(_mainViewModel.MainText, _proposedText);
                    IsReviewing = true;
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Cancelled.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error occurred: " + ex.Message;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void GenerateDiff(string oldText, string newText)
        {
            DiffChunks.Clear();
            var diffBuilder = new InlineDiffBuilder(new Differ());
            var diff = diffBuilder.BuildDiffModel(oldText ?? string.Empty, newText ?? string.Empty);

            foreach (var line in diff.Lines)
            {
                var chunkType = line.Type switch
                {
                    ChangeType.Inserted => DiffChunkType.Inserted,
                    ChangeType.Deleted => DiffChunkType.Deleted,
                    _ => DiffChunkType.Unchanged
                };

                DiffChunks.Add(new DiffChunk
                {
                    Text = line.Text + "\n",
                    Type = chunkType
                });
            }
        }

        [RelayCommand]
        public void AcceptChanges()
        {
            _mainViewModel.MainText = _proposedText;

            History.Insert(0, new AIHistoryItem(RestoreHistoryItem, DeleteHistoryItem)
            {
                Prompt = _currentPromptOrTemplate,
                GeneratedText = _proposedText,
                Timestamp = DateTime.Now
            });
            
            SaveHistory();

            if (_currentPromptOrTemplate == CustomPrompt)
            {
                CustomPrompt = string.Empty;
            }

            IsReviewing = false;
            StatusMessage = "Changes applied.";
            DiffChunks.Clear();
        }

        [RelayCommand]
        public void RejectChanges()
        {
            IsReviewing = false;
            StatusMessage = "Changes rejected.";
            DiffChunks.Clear();
        }
        
        private void DeleteHistoryItem(AIHistoryItem item)
        {
            if (item != null && History.Contains(item))
            {
                History.Remove(item);
                SaveHistory();
            }
        }
        
        [RelayCommand]
        public void ClearHistory()
        {
            History.Clear();
            SaveHistory();
        }

        private void SaveHistory()
        {
            var dtos = History.Select(h => new AIHistoryDto
            {
                Prompt = h.Prompt,
                GeneratedText = h.GeneratedText,
                Timestamp = h.Timestamp
            });
            _historyRepository.Save(dtos);
        }

        private void LoadHistory()
        {
            var items = _historyRepository.Load();
            History.Clear();
            foreach (var item in items)
            {
                History.Add(new AIHistoryItem(RestoreHistoryItem, DeleteHistoryItem)
                {
                    Prompt = item.Prompt ?? "",
                    GeneratedText = item.GeneratedText ?? "",
                    Timestamp = item.Timestamp
                });
            }
        }

        private void RestoreHistoryItem(string text)
        {
            _mainViewModel.MainText = text;
        }
    }
}

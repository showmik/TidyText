using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
using TidyText.Domain.Services;
using CommunityToolkit.Mvvm.Messaging;
using TidyText.App.Messages;

namespace TidyText.App.ViewModels
{
    public partial class AIAssistantViewModel : ObservableObject
    {
        private readonly IAIProviderRouter _router;
        private readonly IMessenger _messenger;
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
        private readonly StringBuilder _streamingBuffer = new();
        private readonly Stopwatch _diffThrottle = new();

        /// <summary>
        /// Minimum interval between diff recomputations during streaming.
        /// Prevents UI thread saturation when tokens arrive faster than
        /// the diff can be rendered.
        /// </summary>
        private const int DiffThrottleMs = 150;

        [ObservableProperty]
        private bool _isProcessing = false;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        public AIAssistantViewModel(
            IAIProviderRouter router,
            IMessenger messenger,
            ISecureKeyVault keyVault,
            IAIHistoryRepository historyRepository,
            System.Collections.Generic.IEnumerable<IPromptTemplateProvider> templateProviders)
        {
            _router = router;
            _messenger = messenger;
            _keyVault = keyVault;
            _historyRepository = historyRepository;
            
            LoadHistory();
            
            foreach (var provider in templateProviders)
            {
                foreach (var template in provider.GetTemplates())
                {
                    Templates.Add(template);
                }
            }

            foreach (var p in _router.GetProviderNames())
            {
                AvailableProviders.Add(p);
            }

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
            var p = _router.GetProvider(provider);
            if (p != null)
            {
                foreach (var model in p.AvailableModels)
                {
                    AvailableModels.Add(model);
                }
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
            string currentText = _messenger.Send(new CurrentTextRequestMessage());
            if (template == null || string.IsNullOrWhiteSpace(currentText)) return;

            SelectedTemplate = template;
            IsProcessing = true;
            StatusMessage = "Processing with " + ActiveProviderName + "...";

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            _cancellationTokenSource = new CancellationTokenSource();

            var prompt = template.GetPrompt(currentText);
            var options = new AIOptions 
            { 
                SystemPrompt = template.SystemPrompt,
                Temperature = 0.4,
                Model = ActiveModel
            };

            try
            {
                StatusMessage = "Streaming proposed changes...";
                _streamingBuffer.Clear();
                _diffThrottle.Restart();
                IsReviewing = true;
                _currentPromptOrTemplate = template.Name;

                await foreach (var chunk in _router.StreamAsync(ActiveProviderName, prompt, options, _cancellationTokenSource.Token))
                {
                    if (chunk.StartsWith("[Error] "))
                    {
                        StatusMessage = "Error occurred: " + chunk.Substring(8);
                        break;
                    }
                    
                    _streamingBuffer.Append(chunk);

                    // Throttle: only recompute diff at most once per DiffThrottleMs
                    if (_diffThrottle.ElapsedMilliseconds >= DiffThrottleMs)
                    {
                        _proposedText = _streamingBuffer.ToString();
                        await GenerateDiffAsync(currentText, _proposedText);
                        _diffThrottle.Restart();
                    }
                }

                // Final flush — always render the complete diff
                _proposedText = _streamingBuffer.ToString();
                await GenerateDiffAsync(currentText, _proposedText);

                if (!StatusMessage.StartsWith("Error"))
                {
                    StatusMessage = "Reviewing proposed changes...";
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
            string currentText = _messenger.Send(new CurrentTextRequestMessage());
            if (string.IsNullOrWhiteSpace(CustomPrompt) || string.IsNullOrWhiteSpace(currentText)) return;

            IsProcessing = true;
            StatusMessage = "Processing custom prompt with " + ActiveProviderName + "...";

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            _cancellationTokenSource = new CancellationTokenSource();

            // The template essentially just prepends the custom prompt to the text
            var prompt = $"{CustomPrompt}\n\n<text>\n{currentText}\n</text>";
            var options = new AIOptions 
            { 
                Temperature = 0.5,
                Model = ActiveModel,
                SystemPrompt = "You are a highly capable AI assistant embedded in a text editor. Execute the user's instructions on the provided text. Return ONLY the modified text without conversational filler, pleasantries, or markdown formatting, unless specifically requested."
            };

            try
            {
                StatusMessage = "Streaming proposed changes...";
                _streamingBuffer.Clear();
                _diffThrottle.Restart();
                IsReviewing = true;
                _currentPromptOrTemplate = CustomPrompt;

                await foreach (var chunk in _router.StreamAsync(ActiveProviderName, prompt, options, _cancellationTokenSource.Token))
                {
                    if (chunk.StartsWith("[Error] "))
                    {
                        StatusMessage = "Error occurred: " + chunk.Substring(8);
                        break;
                    }
                    
                    _streamingBuffer.Append(chunk);

                    // Throttle: only recompute diff at most once per DiffThrottleMs
                    if (_diffThrottle.ElapsedMilliseconds >= DiffThrottleMs)
                    {
                        _proposedText = _streamingBuffer.ToString();
                        await GenerateDiffAsync(currentText, _proposedText);
                        _diffThrottle.Restart();
                    }
                }

                // Final flush — always render the complete diff
                _proposedText = _streamingBuffer.ToString();
                await GenerateDiffAsync(currentText, _proposedText);

                if (!StatusMessage.StartsWith("Error"))
                {
                    StatusMessage = "Reviewing proposed changes...";
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

        /// <summary>
        /// Computes the inline diff on a threadpool thread, then marshals the
        /// result back to the UI thread via the captured SynchronizationContext
        /// (the await resumes on the caller's context — the UI thread).
        /// </summary>
        private async Task GenerateDiffAsync(string oldText, string newText)
        {
            var newChunks = await Task.Run(() =>
            {
                var diffBuilder = new InlineDiffBuilder(new Differ());
                var diff = diffBuilder.BuildDiffModel(oldText ?? string.Empty, newText ?? string.Empty);

                var chunks = new System.Collections.Generic.List<DiffChunk>();
                foreach (var line in diff.Lines)
                {
                    var chunkType = line.Type switch
                    {
                        ChangeType.Inserted => DiffChunkType.Inserted,
                        ChangeType.Deleted => DiffChunkType.Deleted,
                        _ => DiffChunkType.Unchanged
                    };

                    chunks.Add(new DiffChunk
                    {
                        Text = line.Text + "\n",
                        Type = chunkType
                    });
                }
                return chunks;
            }).ConfigureAwait(true); // Explicitly resume on UI thread for collection assignment
            
            // This assignment fires PropertyChanged on the UI thread — safe.
            DiffChunks = new ObservableCollection<DiffChunk>(newChunks);
        }

        [RelayCommand]
        public void AcceptChanges()
        {
            _messenger.Send(new TextReplacementRequestedMessage(_proposedText));

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
            var newHistory = new System.Collections.Generic.List<AIHistoryItem>();
            foreach (var item in items)
            {
                newHistory.Add(new AIHistoryItem(RestoreHistoryItem, DeleteHistoryItem)
                {
                    Prompt = item.Prompt ?? "",
                    GeneratedText = item.GeneratedText ?? "",
                    Timestamp = item.Timestamp
                });
            }
            History = new ObservableCollection<AIHistoryItem>(newHistory);
        }

        private void RestoreHistoryItem(string text)
        {
            _messenger.Send(new TextReplacementRequestedMessage(text));
        }
    }
}

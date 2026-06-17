using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TidyText.Core.Security;

namespace TidyText.App.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SecureKeyVault _keyVault;

        [ObservableProperty]
        private string _geminiApiKey = string.Empty;

        [ObservableProperty]
        private string _deepSeekApiKey = string.Empty;

        [ObservableProperty]
        private string _openAIApiKey = string.Empty;

        [ObservableProperty]
        private string _anthropicApiKey = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public SettingsViewModel(SecureKeyVault keyVault)
        {
            _keyVault = keyVault;
            LoadKeys();
        }

        private void LoadKeys()
        {
            GeminiApiKey = _keyVault.GetKey("Gemini");
            DeepSeekApiKey = _keyVault.GetKey("DeepSeek");
            OpenAIApiKey = _keyVault.GetKey("OpenAI");
            AnthropicApiKey = _keyVault.GetKey("Anthropic");
        }

        [RelayCommand]
        public void SaveKeys()
        {
            _keyVault.SetKey("Gemini", GeminiApiKey);
            _keyVault.SetKey("DeepSeek", DeepSeekApiKey);
            _keyVault.SetKey("OpenAI", OpenAIApiKey);
            _keyVault.SetKey("Anthropic", AnthropicApiKey);
            
            StatusMessage = "API Keys saved securely to Windows Vault.";
        }
    }
}

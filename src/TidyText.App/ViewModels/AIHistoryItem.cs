using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TidyText.App.ViewModels
{
    public partial class AIHistoryItem : ObservableObject
    {
        private readonly Action<string>? _restoreAction;

        [ObservableProperty]
        private string _prompt = string.Empty;

        [ObservableProperty]
        private string _generatedText = string.Empty;

        [ObservableProperty]
        private DateTime _timestamp = DateTime.Now;
        
        [ObservableProperty]
        private bool _isExpanded = false;

        private readonly Action<AIHistoryItem>? _deleteAction;

        public AIHistoryItem(Action<string>? restoreAction, Action<AIHistoryItem>? deleteAction = null)
        {
            _restoreAction = restoreAction;
            _deleteAction = deleteAction;
        }

        [RelayCommand]
        public void Delete()
        {
            _deleteAction?.Invoke(this);
        }

        [RelayCommand]
        public void ToggleExpand()
        {
            IsExpanded = !IsExpanded;
        }

        [RelayCommand]
        public void Restore()
        {
            if (_restoreAction != null && !string.IsNullOrEmpty(GeneratedText))
            {
                _restoreAction(GeneratedText);
            }
        }
    }
}

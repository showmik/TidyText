using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TidyText.App.ViewModels
{
    public partial class AIHistoryItem : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;

        [ObservableProperty]
        private string _prompt = string.Empty;

        [ObservableProperty]
        private string _generatedText = string.Empty;

        [ObservableProperty]
        private DateTime _timestamp = DateTime.Now;
        
        [ObservableProperty]
        private bool _isExpanded = false;

        public AIHistoryItem(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        [RelayCommand]
        public void ToggleExpand()
        {
            IsExpanded = !IsExpanded;
        }

        [RelayCommand]
        public void Restore()
        {
            if (_mainViewModel != null && !string.IsNullOrEmpty(GeneratedText))
            {
                _mainViewModel.MainText = GeneratedText;
            }
        }
    }
}

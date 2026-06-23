using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace TidyText.App.ViewModels
{
    public partial class AIHistoryItem : ObservableObject
    {
        private readonly CommunityToolkit.Mvvm.Messaging.IMessenger _messenger;

        [ObservableProperty]
        private string _prompt = string.Empty;

        [ObservableProperty]
        private string _generatedText = string.Empty;

        [ObservableProperty]
        private DateTime _timestamp = DateTime.Now;
        
        [ObservableProperty]
        private bool _isExpanded = false;

        public AIHistoryItem(CommunityToolkit.Mvvm.Messaging.IMessenger messenger)
        {
            _messenger = messenger;
        }

        [RelayCommand]
        public void Delete()
        {
            _messenger.Send(new TidyText.App.Messages.DeleteHistoryItemMessage(this));
        }

        [RelayCommand]
        public void ToggleExpand()
        {
            IsExpanded = !IsExpanded;
        }

        [RelayCommand]
        public void Restore()
        {
            if (!string.IsNullOrEmpty(GeneratedText))
            {
                _messenger.Send(new TidyText.App.Messages.RestoreHistoryItemMessage(GeneratedText));
            }
        }
    }
}

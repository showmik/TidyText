namespace TidyText.App.Messages
{
    public record DeleteHistoryItemMessage(ViewModels.AIHistoryItem Item);
    public record RestoreHistoryItemMessage(string Text);
}

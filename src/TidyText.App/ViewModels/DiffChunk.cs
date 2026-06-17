using CommunityToolkit.Mvvm.ComponentModel;

namespace TidyText.App.ViewModels
{
    public enum DiffChunkType
    {
        Unchanged,
        Inserted,
        Deleted
    }

    public partial class DiffChunk : ObservableObject
    {
        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        private DiffChunkType _type = DiffChunkType.Unchanged;
    }
}

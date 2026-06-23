namespace TidyText.Domain.Services
{
    /// <summary>
    /// Decouples text-editing consumers (like the AI panel) from
    /// the concrete ViewModel that owns the editor buffer.
    /// </summary>
    public interface ITextEditorMediator
    {
        string CurrentText { get; }
        void ReplaceText(string newText);
    }
}

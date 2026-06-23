namespace TidyText.Domain.Services
{
    public interface IUndoRedoService
    {
        void Push(string state);
        string? Undo(string currentState);
        string? Redo(string currentState);
    }
}

namespace TidyText.Domain.Services
{
    public class UndoRedoService : IUndoRedoService
    {
        private readonly System.Collections.Generic.List<string> _history = new();
        private readonly System.Collections.Generic.List<string> _redoHistory = new();
        private const int MaxHistorySize = 50;

        public void Push(string state)
        {
            if (_history.Count > 0 && _history[^1] == state) return;
            _history.Add(state);
            _redoHistory.Clear();
            if (_history.Count > MaxHistorySize)
                _history.RemoveAt(0);
        }

        public string? Undo(string currentState)
        {
            if (_history.Count == 0) return null;
            _redoHistory.Add(currentState);
            var previous = _history[^1];
            _history.RemoveAt(_history.Count - 1);
            return previous;
        }

        public string? Redo(string currentState)
        {
            if (_redoHistory.Count == 0) return null;
            _history.Add(currentState);
            var next = _redoHistory[^1];
            _redoHistory.RemoveAt(_redoHistory.Count - 1);
            return next;
        }
    }
}

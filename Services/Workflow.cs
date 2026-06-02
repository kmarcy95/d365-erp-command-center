namespace D365CommandCenter.Services;

/// <summary>
/// Performs governed state transitions (submit / approve / reject / lock), writes them to
/// the activity & audit log, persists, and supports a single-step undo of the last action.
/// </summary>
public class Workflow
{
    private readonly AppData _data;
    private readonly UiState _ui;

    public Workflow(AppData data, UiState ui) { _data = data; _ui = ui; }

    public event Action? Changed;

    /// <summary>Label of the most recent undoable action (null if nothing to undo).</summary>
    public string? LastLabel { get; private set; }
    private Func<Task>? _undo;

    /// <summary>
    /// Apply a transition: mutate via <paramref name="apply"/>, log it, persist, and register
    /// the inverse (<paramref name="revert"/>) as the undoable last action.
    /// </summary>
    public async Task ApplyAsync(string module, string action, string detail, Action apply, Action revert)
    {
        apply();
        AddAudit(module, action, detail);
        LastLabel = $"{action} — {detail}";
        _undo = async () =>
        {
            revert();
            AddAudit(module, $"Undo {action}", $"Reverted: {detail}");
            LastLabel = null;
            _undo = null;
            await _data.SaveAsync();
            Changed?.Invoke();
        };
        await _data.SaveAsync();
        Changed?.Invoke();
    }

    public async Task UndoAsync()
    {
        if (_undo is not null) await _undo();
    }

    public void ClearUndo() { LastLabel = null; _undo = null; Changed?.Invoke(); }

    private void AddAudit(string module, string action, string detail)
    {
        _data.Data.AuditLog.Insert(0, new Models.AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            User = _ui.UserName,
            Role = _ui.UserRole,
            Module = module,
            Action = action,
            Detail = detail
        });
    }
}

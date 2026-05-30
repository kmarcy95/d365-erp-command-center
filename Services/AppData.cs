using Blazored.LocalStorage;

namespace D365CommandCenter.Services;

/// <summary>
/// Central in-memory data store for the demo. Phase 2 fills this with the
/// coherent Alamo Foods Co. dataset plus localStorage load/save. For the
/// foundation build it exposes the lifecycle the UI calls into.
/// </summary>
public class AppData
{
    private readonly ILocalStorageService _localStorage;

    public AppData(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public bool IsLoaded { get; private set; }

    /// <summary>Load persisted state or fall back to the seed (Phase 2).</summary>
    public Task InitializeAsync()
    {
        IsLoaded = true;
        return Task.CompletedTask;
    }

    /// <summary>Restore the seeded demo dataset (Phase 2).</summary>
    public Task ResetDemoDataAsync() => Task.CompletedTask;
}

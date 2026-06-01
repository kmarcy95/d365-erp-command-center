namespace D365CommandCenter.Services;

/// <summary>
/// Central data store for the demo. Loads the persisted dataset from localStorage
/// or falls back to the deterministic <see cref="SeedData"/> seed, and saves edits back.
/// Inject app-wide and call <see cref="InitializeAsync"/> once at startup.
/// </summary>
public class AppData
{
    private const string StorageKey = "dataset";
    private readonly BrowserStorage _storage;

    public AppData(BrowserStorage storage) => _storage = storage;

    public DemoData Data { get; private set; } = new();
    public bool IsLoaded { get; private set; }

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (IsLoaded) return;
        try
        {
            var saved = await _storage.GetAsync<DemoData>(StorageKey);
            Data = saved is { Version: >= 4 } ? saved : SeedData.Build();
        }
        catch
        {
            Data = SeedData.Build();
        }
        IsLoaded = true;
        await SaveAsync();
        Changed?.Invoke();
    }

    public async Task SaveAsync()
    {
        await _storage.SetAsync(StorageKey, Data);
        Changed?.Invoke();
    }

    /// <summary>Discard edits and restore the seeded demo dataset.</summary>
    public async Task ResetDemoDataAsync()
    {
        Data = SeedData.Build();
        await SaveAsync();
        Changed?.Invoke();
    }
}

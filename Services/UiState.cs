namespace D365CommandCenter.Services;

public enum AppEnvironment { Production, Sandbox, Development }

/// <summary>
/// App-wide UI state that isn't part of the business dataset: the selected
/// environment (Prod/Sandbox/Dev) and the signed-in user identity. Environment
/// selection persists to localStorage so it survives reloads.
/// </summary>
public class UiState
{
    private const string EnvKey = "ui-environment";
    private readonly BrowserStorage _storage;
    private bool _loaded;

    public UiState(BrowserStorage storage) => _storage = storage;

    public AppEnvironment Environment { get; private set; } = AppEnvironment.Sandbox;

    // Signed-in user (demo identity).
    public string UserName => "Keith Marcy";
    public string UserRole => "Financial Controller";
    public string UserInitials => "KM";

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (_loaded) return;
        try
        {
            var saved = await _storage.GetAsync<string>(EnvKey);
            if (!string.IsNullOrEmpty(saved) && Enum.TryParse<AppEnvironment>(saved, out var e))
                Environment = e;
        }
        catch { /* ignore — fall back to default */ }
        _loaded = true;
    }

    public async Task SetEnvironmentAsync(AppEnvironment env)
    {
        if (env == Environment) return;
        Environment = env;
        await _storage.SetAsync(EnvKey, env.ToString());
        Changed?.Invoke();
    }

    /// <summary>Short pill label, e.g. "PROD".</summary>
    public string EnvShort => Environment switch
    {
        AppEnvironment.Production => "PROD",
        AppEnvironment.Sandbox => "UAT",
        _ => "DEV"
    };

    /// <summary>Full label, e.g. "Sandbox · UAT".</summary>
    public string EnvName => Environment switch
    {
        AppEnvironment.Production => "Production",
        AppEnvironment.Sandbox => "Sandbox · UAT",
        _ => "Development"
    };

    /// <summary>CSS modifier suffix used for the env pill color.</summary>
    public string EnvCss => Environment switch
    {
        AppEnvironment.Production => "prod",
        AppEnvironment.Sandbox => "uat",
        _ => "dev"
    };
}

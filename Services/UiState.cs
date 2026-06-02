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
    private const string DarkKey = "ui-dark";
    private const string DensityKey = "ui-density";
    private const string LandingKey = "ui-landing";
    private readonly BrowserStorage _storage;
    private bool _loaded;

    public UiState(BrowserStorage storage) => _storage = storage;

    public AppEnvironment Environment { get; private set; } = AppEnvironment.Sandbox;

    /// <summary>Dark appearance toggle (persisted).</summary>
    public bool DarkMode { get; private set; }

    /// <summary>UI density: "comfortable" (default) or "compact" (persisted).</summary>
    public string Density { get; private set; } = "comfortable";

    /// <summary>Preferred start-page route ("" = Overview); the app opens here once per page load.</summary>
    public string DefaultLanding { get; private set; } = "";

    // Signed-in user (demo identity).
    public string UserName => "Keith Marcy";
    public string UserRole => "Financial Controller";
    public string UserInitials => "KM";

    /// <summary>Whether the signed-in role may approve/submit/lock records. Approvals are
    /// disabled in Production (change-controlled) to demonstrate environment-aware gating.</summary>
    public bool CanApprove => Environment != AppEnvironment.Production;

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (_loaded) return;
        try
        {
            var saved = await _storage.GetAsync<string>(EnvKey);
            if (!string.IsNullOrEmpty(saved) && Enum.TryParse<AppEnvironment>(saved, out var e))
                Environment = e;

            var dark = await _storage.GetAsync<string>(DarkKey);
            if (!string.IsNullOrEmpty(dark)) DarkMode = dark == "1" || dark.ToLower() == "true";

            var density = await _storage.GetAsync<string>(DensityKey);
            if (density == "compact" || density == "comfortable") Density = density;

            var landing = await _storage.GetAsync<string>(LandingKey);
            if (landing is not null) DefaultLanding = landing;
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

    public async Task SetDarkModeAsync(bool dark)
    {
        if (dark == DarkMode) return;
        DarkMode = dark;
        await _storage.SetAsync(DarkKey, dark ? "1" : "0");
        Changed?.Invoke();
    }

    public async Task SetDensityAsync(string density)
    {
        if (density != "compact" && density != "comfortable") return;
        if (density == Density) return;
        Density = density;
        await _storage.SetAsync(DensityKey, density);
        Changed?.Invoke();
    }

    public async Task SetDefaultLandingAsync(string route)
    {
        DefaultLanding = route ?? "";
        await _storage.SetAsync(LandingKey, DefaultLanding);
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

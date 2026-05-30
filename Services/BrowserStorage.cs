using System.Text.Json;
using Microsoft.JSInterop;

namespace D365CommandCenter.Services;

/// <summary>
/// Tiny wrapper over the browser's localStorage via IJSRuntime — no external
/// package needed. Stores values as JSON under a namespaced key.
/// </summary>
public class BrowserStorage
{
    private const string Prefix = "afc:";
    private readonly IJSRuntime _js;

    public BrowserStorage(IJSRuntime js) => _js = js;

    public async Task<T?> GetAsync<T>(string key)
    {
        var raw = await _js.InvokeAsync<string?>("localStorage.getItem", Prefix + key);
        return string.IsNullOrEmpty(raw) ? default : JsonSerializer.Deserialize<T>(raw);
    }

    public async Task SetAsync<T>(string key, T value)
    {
        var raw = JsonSerializer.Serialize(value);
        await _js.InvokeVoidAsync("localStorage.setItem", Prefix + key, raw);
    }

    public ValueTask RemoveAsync(string key)
        => _js.InvokeVoidAsync("localStorage.removeItem", Prefix + key);
}

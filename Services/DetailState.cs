using Microsoft.AspNetCore.Components;

namespace D365CommandCenter.Services;

/// <summary>
/// Holds the content for the full-page detail view (<c>/detail</c>). A page stashes a
/// rendered fragment + breadcrumb here, then navigates to <c>/detail</c>, which renders it.
/// Replaces all modal dialogs with full-screen navigation.
/// </summary>
public class DetailState
{
    public RenderFragment? Content { get; private set; }
    public string Title { get; private set; } = "";
    public string BackLabel { get; private set; } = "Back";
    public string BackUrl { get; private set; } = "";

    public void Open(string title, string backLabel, string backUrl, RenderFragment content)
    {
        Title = title;
        BackLabel = backLabel;
        BackUrl = backUrl;
        Content = content;
    }

    public void Clear() => Content = null;
}

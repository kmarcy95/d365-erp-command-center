// Chart.js interop for the D365 ERP Command Center.
window.afcCharts = (function () {
    const registry = {};
    function render(canvasId, config) {
        const el = document.getElementById(canvasId);
        if (!el || typeof Chart === 'undefined') return;
        if (registry[canvasId]) { registry[canvasId].destroy(); }
        registry[canvasId] = new Chart(el.getContext('2d'), config);
    }
    function destroy(canvasId) {
        if (registry[canvasId]) { registry[canvasId].destroy(); delete registry[canvasId]; }
    }
    return { render, destroy };
})();

// Command palette: Ctrl/Cmd+K toggles, Escape closes. Routes to a registered
// .NET callback so the Blazor CommandPalette component can show/hide itself.
window.afcPalette = (function () {
    let dotnet = null;
    function register(ref) {
        dotnet = ref;
        document.addEventListener('keydown', onKey);
    }
    function onKey(e) {
        if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
            e.preventDefault();
            if (dotnet) dotnet.invokeMethodAsync('Toggle');
        } else if (e.key === 'Escape' && dotnet) {
            dotnet.invokeMethodAsync('Close');
        }
    }
    function focus(id) {
        const el = document.getElementById(id);
        if (el) el.focus();
    }
    return { register, focus };
})();

// CSV download helper used by the data-grid pages.
window.afcDownload = function (filename, text) {
    const blob = new Blob([text], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

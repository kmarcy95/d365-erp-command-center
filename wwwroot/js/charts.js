// Chart.js interop for the D365 ERP Command Center.
window.afcCharts = (function () {
    const registry = {};
    function render(canvasId, config, dotnetRef) {
        const el = document.getElementById(canvasId);
        if (!el || typeof Chart === 'undefined') return;
        if (registry[canvasId]) { registry[canvasId].destroy(); }
        // Optional click-to-filter: report the clicked label back to the .NET component.
        if (dotnetRef) {
            config.options = config.options || {};
            config.options.onClick = function (evt, els, chart) {
                if (els && els.length) {
                    var label = chart.data.labels[els[0].index];
                    dotnetRef.invokeMethodAsync('OnPointClick', String(label));
                }
            };
            config.options.onHover = function (evt, els) {
                if (evt && evt.native && evt.native.target) {
                    evt.native.target.style.cursor = (els && els.length) ? 'pointer' : 'default';
                }
            };
        }
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

// "?" opens the keyboard-shortcuts help (ignored while typing in a field).
window.afcHelp = (function () {
    let dotnet = null;
    function register(ref) {
        dotnet = ref;
        document.addEventListener('keydown', onKey);
    }
    function isEditable(el) {
        if (!el) return false;
        const tag = el.tagName ? el.tagName.toLowerCase() : '';
        // Native fields, contentEditable, or any custom element (e.g. <fluent-search>)
        // whose real input lives in shadow DOM — a hyphen in the tag marks a web component.
        return tag === 'input' || tag === 'textarea' || tag === 'select'
            || el.isContentEditable === true || tag.indexOf('-') > -1;
    }
    function onKey(e) {
        if (e.key !== '?' || e.ctrlKey || e.metaKey || e.altKey) return;
        // composedPath() pierces shadow roots, so a Fluent field reports its inner input.
        const path = typeof e.composedPath === 'function' ? e.composedPath() : [e.target];
        if (path.some(isEditable)) return;
        e.preventDefault();
        if (dotnet) dotnet.invokeMethodAsync('ShowHelp');
    }
    return { register };
})();

// Print / "Save as PDF" — the browser print dialog renders the print stylesheet
// (report header shown, app chrome hidden). Optional body class scopes which
// report header is visible during the print.
window.afcPrint = function (reportClass) {
    var added = false;
    if (reportClass) { document.body.classList.add(reportClass); added = true; }
    window.print();
    if (added) {
        // remove on the next tick so the print engine has captured the layout
        setTimeout(function () { document.body.classList.remove(reportClass); }, 500);
    }
};

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

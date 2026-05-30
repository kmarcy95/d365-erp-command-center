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

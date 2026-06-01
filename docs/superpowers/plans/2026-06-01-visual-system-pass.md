# Visual System Pass Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the D365 ERP Command Center a cohesive Fluent/D365 design-token system, a fully-themed dark mode (OS-aware + persisting toggle), and tasteful motion — so it reads like a genuine modern Dynamics module.

**Architecture:** All color/elevation/motion values become semantic CSS custom properties in `wwwroot/css/app.css` `:root`; a `body.theme-dark` block redefines them for dark. Every custom class is refactored to consume tokens (light values unchanged), so dark mode applies everywhere automatically. A tiny `afcTheme` JS module (in `wwwroot/js/charts.js`) reads OS preference + toggles a `body` class + persists to localStorage; `MainLayout` drives it and keeps Fluent's `DesignThemeModes` in sync. Motion is CSS-driven plus a JS `afcCountUp` tween for KPI cards, all gated behind `prefers-reduced-motion`.

**Tech Stack:** Blazor WebAssembly (.NET 8), Fluent UI Blazor, plain CSS custom properties, vanilla JS interop (no new dependencies).

**Verification model:** This project has **no unit-test harness** (per spec + project history). "Tests" here are: (a) the mandated clean-Release build gate `rm -rf obj bin && dotnet build -c Release`, and (b) headless-Chrome CDP assertions against a running `dotnet run`. Follow the CDP driver pattern already used this project (Node `WebSocket` to `http://localhost:9222`, `Runtime.evaluate`).

---

## File Structure

- **Modify** `wwwroot/css/app.css` — add token `:root`, add `body.theme-dark` block, add motion tokens/keyframes, refactor all literals to `var(--token)`. (Single CSS file; this is the backbone.)
- **Modify** `wwwroot/js/charts.js` — add `window.afcTheme` (OS pref + body class + persist) and `window.afcCountUp` (number tween).
- **Modify** `Layout/MainLayout.razor` — theme init (OS-aware), toggle wiring to `afcTheme` + `body.theme-dark`, persist.
- **Modify** `Components/KpiCard.razor` — optional `Raw`/`Prefix`/`Suffix`/`Decimals` params + count-up call in `OnAfterRenderAsync`.
- **Modify (opt-in callers)** `Pages/Budget.razor`, `Pages/ExecutiveDashboard.razor`, `Pages/AccountsPayable.razor`, `Pages/AccountsReceivable.razor` — pass `Raw` to headline KPI cards.

No new files. No new components. No dependency changes.

---

## Task 1: Add the semantic token `:root`

**Files:**
- Modify: `wwwroot/css/app.css:1-5` (the current `:root`)

- [ ] **Step 1: Replace the 4-line `:root` with the full token set**

Replace lines 1-5 (`:root { --afc-accent … --afc-maxw … }`) with:

```css
:root {
    /* Accent (D365 blue) */
    --accent: #0f6cbd;
    --accent-strong: #115ea3;
    --accent-hover: #2899f5;
    --accent-subtle: #eef5fb;          /* selected-row / active tint */
    /* Back-compat aliases (existing files reference these) */
    --afc-accent: var(--accent);
    --afc-accent-strong: var(--accent-strong);

    /* Surfaces (elevation ladder) */
    --bg: #faf9f8;                     /* app content background */
    --surface: #ffffff;                /* cards, nav, footer */
    --surface-2: #fafafa;              /* subtle raised strips (page-meta, table head) */
    --surface-raised: #ffffff;         /* dialogs, menus */
    --surface-hover: #f3f3f3;          /* menu/option hover */
    --row-hover: #f3f9fd;              /* grid row hover */

    /* Borders */
    --border: #edebe9;
    --border-strong: #d9d9d9;
    --border-soft: #eeeeee;            /* table cell separators */

    /* Text */
    --text: #242424;
    --text-secondary: #605e5c;
    --text-muted: #8a8a8a;

    /* Status (fg + bg pairs) */
    --ok: #0e700e;        --ok-bg: #dff6dd;
    --warn: #8a6d00;      --warn-bg: #fff4ce;
    --bad: #b10e1c;       --bad-bg: #fde7e9;
    --info: #0f548c;      --info-bg: #e5f1fb;
    --neutral-fg: #555555; --neutral-bg: #f0f0f0;
    /* Status accents used on charts/meters/env dots */
    --ok-solid: #107c10;
    --warn-solid: #d9a400;
    --bad-solid: #d13438;
    --prod: #c50f1f;

    /* Row tints (variance) */
    --row-warn-bg: #fff9e6;
    --row-bad-bg: #fdeef0;

    /* Shadows / radius */
    --shadow-sm: 0 1px 3px rgba(0,0,0,.08);
    --shadow-md: 0 4px 14px rgba(0,0,0,.12);
    --shadow-lg: 0 14px 40px rgba(0,0,0,.28);
    --radius: 8px;

    /* Motion */
    --ease: cubic-bezier(.2,.8,.2,1);
    --motion-fast: .12s;
    --motion-base: .28s;

    --afc-maxw: 1320px;
}
```

- [ ] **Step 2: Clean Release build**

Run: `rm -rf obj bin && dotnet build -c Release`
Expected: `Build succeeded` (CSS isn't compiled, but this confirms nothing else broke).

- [ ] **Step 3: Commit**

```bash
git add wwwroot/css/app.css
git commit -m "css: add semantic Fluent/D365 design-token :root (light values)"
```

---

## Task 2: Refactor `app.css` literals to consume tokens (light unchanged)

**Files:**
- Modify: `wwwroot/css/app.css` (all rules below `:root`)

**Goal:** Every hardcoded color literal becomes `var(--token)`. Light-mode rendering is byte-identical in effect. This is the prerequisite for dark mode.

- [ ] **Step 1: Apply this exact find → replace mapping across the whole file**

Replace each literal (as it appears in property values) with the token. Where a literal already has a `var(--neutral-…, #fallback)` Fluent fallback, swap the fallback to our token, e.g. `var(--neutral-layer-1, #fff)` → `var(--neutral-layer-1, var(--surface))`.

| Literal (in value) | Replace with |
|---|---|
| `#faf9f8` (backgrounds) | `var(--bg)` |
| `#fff` / `#ffffff` (card/nav/footer/table bg, menu bg) | `var(--surface)` |
| `#fafafa` (page-meta bg) | `var(--surface-2)` |
| `#faf9f8` (table head bg, line 110) | `var(--surface-2)` |
| `#605e5c` (secondary text) | `var(--text-secondary)` |
| `#8a8a8a` (muted text) | `var(--text-muted)` |
| `#555` / `#555555` | `var(--neutral-fg)` |
| `#242424` (strong text) | `var(--text)` |
| `#edebe9` (borders) | `var(--border)` |
| `#d9d9d9` (strong borders, inputs) | `var(--border-strong)` |
| `#eee` / `#ededed` (cell separators, scale divider) | `var(--border-soft)` |
| `#e6e6e6` (page-meta/gov border) | `var(--border)` |
| `#ddd` (table total top border) | `var(--border-strong)` |
| `#0f6cbd` / `#0078d4` (accent, incl. focus outline, drill text, cmdp active fallback, print header) | `var(--accent)` |
| `#f3f3f3` (env-opt hover) | `var(--surface-hover)` |
| `#f3f9fd` (row hover) | `var(--row-hover)` |
| `#eef5fb` (env-opt active) | `var(--accent-subtle)` |
| `#dff6dd` (good bg: sb-good, gc-approved, sw-ok) | `var(--ok-bg)` |
| `#0e700e` (good fg: sb-good, gc/dot, var-pos, gov current) | `var(--ok)` |
| `#fff4ce` (warn bg) | `var(--warn-bg)` |
| `#8a6d00` (warn fg, env-uat, pm-closed) | `var(--warn)` |
| `#fde7e9` (bad bg: sb-bad, error-banner, sw-bad) | `var(--bad-bg)` |
| `#b10e1c` (bad fg: sb-bad, var-neg) | `var(--bad)` |
| `#e5f1fb` (info bg: sb-info, gc-locked) | `var(--info-bg)` |
| `#0f548c` (info fg) | `var(--info)` |
| `#f0f0f0` (neutral bg: sb-neutral, gc-draft, gs) | `var(--neutral-bg)` |
| `#107c10` (ok solid: pm-live, util-fill, kpi-delta.up) | `var(--ok-solid)` |
| `#d9a400` (warn solid: dot-uat, util u-warn) | `var(--warn-solid)` |
| `#d13438` (bad solid: kpi-delta.down, util u-bad) | `var(--bad-solid)` |
| `#c50f1f` (env-prod, dot-prod) | `var(--prod)` |
| `#fff9e6` (var-row-warn) | `var(--row-warn-bg)` |
| `#fdeef0` (var-row-bad) | `var(--row-bad-bg)` |
| `0 4px 14px rgba(0,0,0,.12)` (kpi hover shadow, line 74) | `var(--shadow-md)` |
| `0 14px 40px rgba(0,0,0,.28)` (env-menu shadow, line 182) | `var(--shadow-lg)` |

Leave alone (intentionally not tokenized): the header/hero/loading **gradients** and their `rgba(15,108,189,…)` glows (brand gradient stays fixed across themes); `rgba(255,255,255,.18)` on header pills (they sit on the fixed blue header); the audit `.au-mod` chip `#eef2f6`/`#41607a`; pure `#fff` text on solid accent fills (e.g. `.scale-toggle button.is-active { … color:#fff }`, `.cmdp-item.active`); print-block colors inside `@media print`.

- [ ] **Step 2: Clean Release build**

Run: `rm -rf obj bin && dotnet build -c Release`
Expected: `Build succeeded`.

- [ ] **Step 3: Visual sanity (light mode unchanged) via headless CDP**

Start app: `dotnet run -c Debug --urls http://localhost:5099` (background), launch headless Chrome with `--remote-debugging-port=9222`.
Driver assertion: navigate `/budget`, read computed `background-color` of `.kpi-card` and `color` of `.var-neg`; confirm they are non-empty and the page renders (`.gov-banner` present). Expected: renders identically to before (values resolve through tokens).

- [ ] **Step 4: Commit**

```bash
git add wwwroot/css/app.css
git commit -m "css: refactor all literals to consume design tokens (light unchanged)"
```

---

## Task 3: Add `afcTheme` JS helper

**Files:**
- Modify: `wwwroot/js/charts.js` (append a new IIFE near the other `window.afc*` modules, before `window.afcDownload`)

- [ ] **Step 1: Add the theme helper**

```javascript
// Theme: OS-aware dark mode + persistence. Toggles `theme-dark` on <body>.
window.afcTheme = (function () {
    const KEY = 'afc:theme';
    function osPrefersDark() {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    }
    function stored() { return localStorage.getItem(KEY); }   // "Dark" | "Light" | null
    // Resolve the mode to apply on load: stored pref wins, else OS.
    function initial() {
        const s = stored();
        return s === 'Dark' || s === 'Light' ? s : (osPrefersDark() ? 'Dark' : 'Light');
    }
    function apply(mode) {
        document.body.classList.toggle('theme-dark', mode === 'Dark');
    }
    function set(mode) { localStorage.setItem(KEY, mode); apply(mode); }
    return { initial, apply, set };
})();
```

- [ ] **Step 2: Clean Release build**

Run: `rm -rf obj bin && dotnet build -c Release`
Expected: `Build succeeded`.

- [ ] **Step 3: Commit**

```bash
git add wwwroot/js/charts.js
git commit -m "js: add afcTheme helper (OS-aware dark mode + persistence)"
```

---

## Task 4: Wire theme init + toggle in MainLayout

**Files:**
- Modify: `Layout/MainLayout.razor:7` (`<FluentDesignTheme>`), `:112` (`_mode` field), `:132-139` (`OnAfterRenderAsync`), `:166-167` (`ToggleTheme`)

- [ ] **Step 1: Make `OnAfterRenderAsync` resolve and apply the initial theme**

Replace the current `OnAfterRenderAsync` body (lines 132-139) with:

```csharp
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _selfRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("afcHelp.register", _selfRef);

            // Resolve initial theme: stored pref wins, else OS preference.
            var initial = await JS.InvokeAsync<string>("afcTheme.initial");
            await JS.InvokeVoidAsync("afcTheme.apply", initial);
            var mode = initial == "Dark" ? DesignThemeModes.Dark : DesignThemeModes.Light;
            if (mode != _mode) { _mode = mode; StateHasChanged(); }
        }
    }
```

- [ ] **Step 2: Make `ToggleTheme` persist + drive the body class**

Replace `ToggleTheme` (lines 166-167) with:

```csharp
    private async Task ToggleTheme()
    {
        _mode = _mode == DesignThemeModes.Light ? DesignThemeModes.Dark : DesignThemeModes.Light;
        await JS.InvokeVoidAsync("afcTheme.set", _mode == DesignThemeModes.Dark ? "Dark" : "Light");
    }
```

(The toggle button at the header already calls `OnClick="ToggleTheme"`; an async Task handler is fine.)

- [ ] **Step 3: Clean Release build**

Run: `rm -rf obj bin && dotnet build -c Release`
Expected: `Build succeeded`. (If RZ/CS error about `ToggleTheme` return type, confirm the button uses `OnClick` not a sync delegate — it does.)

- [ ] **Step 4: Commit**

```bash
git add Layout/MainLayout.razor
git commit -m "layout: OS-aware theme init + persist toggle via afcTheme"
```

---

## Task 5: Add the dark theme token block

**Files:**
- Modify: `wwwroot/css/app.css` (append at end of file, after the `@media print` block)

- [ ] **Step 1: Append the dark override block**

```css
/* ============================================================
   Dark theme — redefines tokens; applies app-wide because every
   custom class consumes them. Triggered by body.theme-dark.
   ============================================================ */
body.theme-dark {
    --accent: #4aa3e8;          /* lifted for contrast on dark */
    --accent-strong: #2899f5;
    --accent-hover: #6cb6ef;
    --accent-subtle: #16324a;

    --bg: #1b1a19;
    --surface: #252423;
    --surface-2: #2d2c2b;
    --surface-raised: #2d2c2b;
    --surface-hover: #3b3a39;
    --row-hover: #2f3a44;

    --border: #3b3a39;
    --border-strong: #4a4948;
    --border-soft: #3b3a39;

    --text: #f3f2f1;
    --text-secondary: #c8c6c4;
    --text-muted: #979593;

    --ok: #6ccb6c;    --ok-bg: #143614;
    --warn: #e6c34a;  --warn-bg: #3a2f0a;
    --bad: #f1707a;   --bad-bg: #3a1418;
    --info: #6cb6ef;  --info-bg: #14283a;
    --neutral-fg: #c8c6c4; --neutral-bg: #3b3a39;
    --ok-solid: #2ea043;
    --warn-solid: #d9a400;
    --bad-solid: #e06c75;
    --prod: #e06c75;

    --row-warn-bg: #332b12;
    --row-bad-bg: #3a1a1e;

    --shadow-sm: 0 1px 3px rgba(0,0,0,.5);
    --shadow-md: 0 4px 14px rgba(0,0,0,.55);
    --shadow-lg: 0 14px 40px rgba(0,0,0,.65);
}

/* The app content + nav use Fluent layer vars with token fallbacks; in dark,
   pin them to our tokens so non-token Fluent fallbacks don't leak light. */
body.theme-dark .nav-shell,
body.theme-dark .grid-wrap,
body.theme-dark .cmdp-panel { background: var(--surface); }
body.theme-dark .app-content { background: var(--bg); }
```

- [ ] **Step 2: Clean Release build**

Run: `rm -rf obj bin && dotnet build -c Release`
Expected: `Build succeeded`.

- [ ] **Step 3: Verify dark mode re-themes custom chrome via CDP**

Driver: navigate `/budget`; record computed `background-color` of `.kpi-card` (light). Click the header theme-toggle button (the one showing 🌙/☀️). After ~300ms, re-read `.kpi-card` background and `.env-pill`/`.page-meta`/`.afc-table th` backgrounds. **Assert each changed** vs the light reading (proves tokens drive custom chrome, not just Fluent). Then assert `document.body.classList.contains('theme-dark')` is true; reload; assert it is **still** true (persisted) and `localStorage['afc:theme']` === `"Dark"`. Reset to light for cleanliness.

- [ ] **Step 4: Commit**

```bash
git add wwwroot/css/app.css
git commit -m "css: add designed dark theme (token overrides + layer pinning)"
```

---

## Task 6: Motion — hover-lift, chart fade-in, eased transitions

**Files:**
- Modify: `wwwroot/css/app.css` (update `.kpi-card--clickable` line 73-74; `.chart-card` line 65; `.grid-row-clickable` line 80-81; append a motion block + reduced-motion guard)

- [ ] **Step 1: Standardize hover-lift + transitions on cards/rows**

Replace `.kpi-card--clickable` rules (lines 73-74) with:

```css
.kpi-card { transition: transform var(--motion-fast) var(--ease), box-shadow var(--motion-fast) var(--ease); }
.kpi-card--clickable { cursor: pointer; }
.kpi-card--clickable:hover { transform: translateY(-2px); box-shadow: var(--shadow-md); }
```

Replace `.grid-row-clickable:hover` (line 81) with:

```css
.grid-row-clickable:hover { background: var(--row-hover); }
```

- [ ] **Step 2: Append chart fade-in + content-fade + reduced-motion guard**

Append to end of file:

```css
/* ---------- Motion ---------- */
.chart-card { animation: afc-rise var(--motion-base) var(--ease) both; }
.content-inner { animation: afc-fade var(--motion-base) var(--ease) both; }
@keyframes afc-rise { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: none; } }
@keyframes afc-fade { from { opacity: 0; } to { opacity: 1; } }

@media (prefers-reduced-motion: reduce) {
    .chart-card, .content-inner { animation: none !important; }
    .kpi-card, .kpi-card--clickable:hover { transition: none !important; transform: none !important; }
    .app-loading-mark, .page-meta .pm-live, .sk { animation: none !important; }
}
```

- [ ] **Step 3: Clean Release build**

Run: `rm -rf obj bin && dotnet build -c Release`
Expected: `Build succeeded`.

- [ ] **Step 4: Verify motion + reduced-motion via CDP**

Driver: navigate `/executive-dashboard`; assert `.chart-card` has a non-`none` computed `animation-name`. Then set CDP emulated media `prefers-reduced-motion: reduce` (`Emulation.setEmulatedMedia` with features `[{name:'prefers-reduced-motion',value:'reduce'}]`), reload, assert `.chart-card` computed `animation-name` is `none`. Clear emulation.

- [ ] **Step 5: Commit**

```bash
git add wwwroot/css/app.css
git commit -m "css: tasteful motion (hover-lift, chart fade-in, content fade) + reduced-motion guard"
```

---

## Task 7: KPI count-up

**Files:**
- Modify: `wwwroot/js/charts.js` (append `window.afcCountUp` before `window.afcDownload`)
- Modify: `Components/KpiCard.razor` (add params + `OnAfterRenderAsync`)

- [ ] **Step 1: Add the count-up JS tween**

```javascript
// Animates an element's number 0 -> target (~600ms), then writes the exact
// preformatted final string so currency/percent/comma formatting is correct.
window.afcCountUp = function (elementId, target, finalText, decimals) {
    const el = document.getElementById(elementId);
    if (!el) return;
    const reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduce || typeof target !== 'number' || !isFinite(target)) { el.textContent = finalText; return; }
    const dur = 600, t0 = performance.now(), d = decimals || 0;
    function frame(now) {
        const p = Math.min((now - t0) / dur, 1);
        const eased = 1 - Math.pow(1 - p, 3);
        if (p < 1) {
            el.textContent = (target * eased).toLocaleString('en-US', { minimumFractionDigits: d, maximumFractionDigits: d });
            requestAnimationFrame(frame);
        } else {
            el.textContent = finalText;   // exact formatting as the final frame
        }
    }
    requestAnimationFrame(frame);
};
```

- [ ] **Step 2: Add params + count-up call to `KpiCard.razor`**

Replace the whole file with:

```razor
<FluentCard Class="@CardClass" role="@(Clickable ? "button" : null)" tabindex="@(Clickable ? "0" : null)"
            @onclick="HandleClick" @onkeydown="HandleKey">
    <div class="kpi-label">@Label</div>
    <div class="kpi-value" id="@_valueId">@Value</div>
    @if (!string.IsNullOrEmpty(Delta))
    {
        <div class="kpi-delta @(Positive ? "up" : "down")">@(Positive ? "▲" : "▼") @Delta</div>
    }
    @if (!string.IsNullOrEmpty(Sub))
    {
        <div class="kpi-sub">@Sub</div>
    }
    @if (Clickable)
    {
        <div class="kpi-drill">View details →</div>
    }
</FluentCard>

@code {
    [Parameter] public string Label { get; set; } = "";
    [Parameter] public string Value { get; set; } = "";
    [Parameter] public string? Delta { get; set; }
    [Parameter] public bool Positive { get; set; } = true;
    [Parameter] public string? Sub { get; set; }
    [Parameter] public bool Clickable { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>When set, the value animates 0→Raw on first render, then settles on the exact Value string.</summary>
    [Parameter] public decimal? Raw { get; set; }
    /// <summary>Decimal places to show mid-tween (final frame always uses Value).</summary>
    [Parameter] public int Decimals { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private readonly string _valueId = "kpi-" + Guid.NewGuid().ToString("N");

    private string CardClass => Clickable ? "kpi-card kpi-card--clickable" : "kpi-card";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Raw is { } raw)
        {
            await JS.InvokeVoidAsync("afcCountUp", _valueId, (double)raw, Value, Decimals);
        }
    }

    private async Task HandleClick()
    {
        if (Clickable) await OnClick.InvokeAsync();
    }

    private async Task HandleKey(KeyboardEventArgs e)
    {
        if (Clickable && (e.Key == "Enter" || e.Key == " " || e.Key == "Spacebar"))
            await OnClick.InvokeAsync();
    }
}
```

- [ ] **Step 3: Opt headline KPIs into count-up**

Add `Raw="@(...)"` to the money/number KPI cards on these pages (match the existing `Value` expression). Examples — apply the same pattern to each page's KPI cards:

- `Pages/ExecutiveDashboard.razor`: on the Revenue card add `Raw="@_revenue"`; Open Receivable `Raw="@_openAr"`; Open Payable `Raw="@_openAp"`; Inventory Value `Raw="@_inventoryValue"`; Open Sales Orders `Raw="@_openSoCount"`. (Gross Margin % card: add `Raw="@_grossMarginPct" Decimals="1"`.)
- `Pages/Budget.razor`: Total Budget `Raw="@_totalBudget"`; Total Actual `Raw="@_totalActual"`; Variance `Raw="@_totalVariance"`; Over Budget `Raw="@_overCount"`.
- `Pages/AccountsPayable.razor`: Outstanding `Raw="@_outstanding"`; Overdue `Raw="@_overdueValue"`; Open Invoices `Raw="@_openCount"`; Vendors `Raw="@_vendorCount"`.
- `Pages/AccountsReceivable.razor`: Outstanding `Raw="@_outstanding"`; Overdue `Raw="@_overdueValue"`; In Collections `Raw="@_inCollections"`; Customers `Raw="@_customerCount"`.

Do NOT add `Raw` to the scale-dependent Budget/AP/AR cards if it conflicts with the `_scale` formatting — those `Value`s use `Fmt.Money(x, _scale)`; the final frame still writes the exact `Value`, so the tween is cosmetic and correct. Leave the General Ledger "Trial Balance" text card (`Value="Balanced"`) static (no `Raw`).

- [ ] **Step 4: Clean Release build**

Run: `rm -rf obj bin && dotnet build -c Release`
Expected: `Build succeeded`.

- [ ] **Step 5: Verify count-up via CDP**

Driver: navigate `/executive-dashboard`; immediately poll `.kpi-value` textContent a few times over ~500ms; assert it CHANGES across samples (mid-tween) and the final value matches the static expected (e.g. contains `$`). Then emulate `prefers-reduced-motion: reduce`, reload, assert the first read of `.kpi-value` already equals the final formatted value (no tween). Assert zero console errors.

- [ ] **Step 6: Commit**

```bash
git add wwwroot/js/charts.js Components/KpiCard.razor Pages/ExecutiveDashboard.razor Pages/Budget.razor Pages/AccountsPayable.razor Pages/AccountsReceivable.razor
git commit -m "feat: animated KPI count-up (opt-in via Raw; reduced-motion safe)"
```

---

## Task 8: Full verification, push, deploy

**Files:** none (verification + release)

- [ ] **Step 1: Clean Release build (authoritative gate)**

Run: `rm -rf obj bin && dotnet build -c Release`
Expected: `Build succeeded`, 0 warnings.

- [ ] **Step 2: Full CDP regression pass**

With `dotnet run` + headless Chrome, run one driver covering: (a) light→dark toggle re-themes `.kpi-card`/`.env-pill`/`.page-meta`/`.afc-table th`; (b) dark persists across reload; (c) chart fade-in animation-name present; (d) reduced-motion disables chart animation AND count-up; (e) KPI count-up animates in normal mode; (f) existing features still work — Ctrl+K palette opens, a KPI drill-down dialog opens with a chart, a grid row opens a detail dialog; (g) **zero console errors** on `/`, `/budget`, `/executive-dashboard`, `/accounts-payable`. Write results to a file and read it.

- [ ] **Step 3: Kill test processes**

```bash
taskkill //F //IM chrome.exe; taskkill //F //IM dotnet.exe
```

- [ ] **Step 4: Push**

```bash
git push origin main
```

- [ ] **Step 5: Confirm green deploy for HEAD**

Poll `gh run list --limit 1` until `completed`; assert `conclusion == success` AND `headSha` matches `git rev-parse --short HEAD`. (Per the project pre-push gate: never infer deploy success from a live 200.)

- [ ] **Step 6: Sync docs**

Update `CLAUDE.md` (repo-local) + `project_d365_command_center.md` memory with: the token system + dark mode + motion, the `afcTheme`/`afcCountUp` JS modules, and the `KpiCard.Raw` opt-in pattern. Commit `CLAUDE.md`, push, confirm green.

---

## Notes for the implementer

- **The clean-Release build is the only authoritative build.** Incremental `dotnet build -c Debug` has masked CI failures on this project (markup referencing symbols whose code-behind wasn't recompiled). Always `rm -rf obj bin` first.
- **Fluent layer vars:** some classes use `var(--neutral-layer-1, #fff)`. Fluent sets those vars at runtime, so in light they may resolve to Fluent's value, not our fallback — that's fine. The `body.theme-dark` pins in Task 5 Step 1 cover the spots where a light Fluent value would leak into dark.
- **No `.razor.css` isolation here** — all styles live in the global `app.css`; the `::deep` on `.var-row-*` (line 288) is pre-existing and stays.
- **CDP driver pattern:** Node 22+ global `WebSocket` to the page target's `webSocketDebuggerUrl` (use `PUT /json/new?<url>` or the existing page target); `Runtime.evaluate` with `returnByValue`. Reuse the harness from prior tasks this session.

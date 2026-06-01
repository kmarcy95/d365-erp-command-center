# Visual System Pass — D365/Fluent identity, designed dark mode, motion

**Date:** 2026-06-01
**Scope:** App-wide visual refresh (`wwwroot/css/app.css`, `Layout/MainLayout.razor`, `Components/KpiCard.razor`, plus inline-hex cleanup in a few components/pages)
**Status:** Approved design

## Goal

Make the Command Center read like an authentic, modern Dynamics 365 module rather than
near-stock Fluent. Three coordinated changes, done in order because each depends on the prior:

1. A **semantic design-token system** grounded in the Fluent/D365 palette.
2. A **designed dark theme** that re-themes *all* custom chrome (today's toggle only re-skins
   Fluent's own components — env switcher, KPI cards, tables, dialogs, page-meta strips, etc.
   stay light because they use hardcoded hex).
3. **Tasteful, subtle motion** (count-up, hover-lift, fade-in, eased transitions), all gated
   behind `prefers-reduced-motion`.

Decisions locked in brainstorming: **neutral D365 look** (no Alamo Foods branding beyond the
existing "AF" mark); **OS-aware dark mode** (first load follows `prefers-color-scheme`, header
toggle overrides + persists); **tasteful/subtle motion**.

The app is already D365 blue (`--afc-accent: #0f6cbd`), so this is *systematizing and deepening*
an existing identity, not a re-color. Net visual change in **light** mode is near-zero; the
payoff is a real dark mode + consistency + polish.

## 1. Token system (`app.css` `:root`)

Replace the 4-variable `:root` with a full semantic set (names indicative):

- **Accent:** `--accent` `#0f6cbd`, `--accent-strong` `#115ea3`, `--accent-hover`,
  `--accent-subtle` (selected-row / active-state tint).
- **Surfaces (elevation ladder):** `--bg`, `--surface`, `--surface-2`, `--surface-raised`.
- **Borders:** `--border`, `--border-strong`.
- **Text:** `--text`, `--text-secondary`, `--text-muted`.
- **Status (fg + bg pairs):** `--ok`/`--ok-bg`, `--warn`/`--warn-bg`, `--bad`/`--bad-bg`,
  `--info`/`--info-bg` — replacing scattered literals (`#dff6dd`, `#fde7e9`, `#fff4ce`,
  `#e5f1fb`, status-badge colors, var-row tints, util-fill colors, etc.).
- **Shadows/radii/motion:** `--shadow-sm/md/lg`, `--radius`, `--ease`, `--motion-fast`,
  `--motion-base`.
- Keep `--afc-accent`/`--afc-accent-strong` as aliases of `--accent`/`--accent-strong` so the
  ~11 files already referencing them keep working without a mass rename.

## 2. Dark theme (`body.theme-dark { … }`)

A single override block redefines every token for dark: **designed** elevated dark surfaces
(`#1b1a19` bg / `#252423` surface / `#2d2c2b` raised — not pure black), recalibrated status
tints (darker bg, brighter fg for AA contrast on dark), softer borders, adjusted shadows.
Because all custom classes consume tokens (§3), dark mode applies everywhere automatically.

**Trigger logic (`MainLayout`):**
- On first load (no stored pref), read `prefers-color-scheme` via JS interop and set the
  initial mode.
- The existing header toggle flips mode, sets `body.theme-dark` (toggle a class on
  `document.body` via a tiny JS helper), persists to `localStorage` `afc:theme`, and keeps
  Fluent's `DesignThemeModes` in sync so Fluent components + our chrome match.
- A small `afcTheme` JS module in `charts.js` handles: read OS pref, get/set the body class,
  read/write the stored value.

## 3. Token refactor (the bulk of the work)

Sweep `app.css` replacing raw hex with `var(--token)`. Also fix the inline-hex spots in
components/pages (e.g. `MainLayout` menu `color:#242424`, `UiState`-driven pills already use
classes, `.user-avatar color:var(--afc-accent-strong)` already tokenized). Light-mode values
stay equal to today's (named, not changed). This is the prerequisite that makes dark mode work.

## 4. Motion

Shared tokens `--ease: cubic-bezier(.2,.8,.2,1)`, `--motion-fast: .12s`, `--motion-base: .28s`.
All animation wrapped so `@media (prefers-reduced-motion: reduce)` disables it.

- **KPI count-up:** `KpiCard` gains an optional `[Parameter] decimal? Raw` plus a format hint
  (`Prefix`/`Suffix`/`Decimals`, or reuse the existing `Value` string for the final render).
  When `Raw` is supplied, `OnAfterRenderAsync` calls a JS helper
  `afcCountUp(elementId, target, prefix, suffix, decimals)` that tweens the displayed number
  0→target (~600ms ease-out) then sets the exact preformatted `Value` as the final frame so
  formatting (`$`/`%`/`,`) is always correct. Cards **without** `Raw` render statically — no
  behavior change for existing usages. KPI callers opt in by passing `Raw`.
- **Card hover-lift:** standardize `.kpi-card`/`.chart-card`/`.grid-row-clickable` hover to
  `translateY(-2px)` + `--shadow-md`, transition `--motion-fast`.
- **Chart fade-in:** `.chart-card` fades/rises in on first paint (CSS animation).
- **Dialog/route easing:** consistent ease-out on dialog content and a subtle content fade on
  route change (CSS on `.content-inner`).

## Verification

- `rm -rf obj bin && dotnet build -c Release` → "Build succeeded" (the mandated clean-Release
  pre-push gate).
- Headless-CDP: (a) toggle dark mode, assert a custom-chrome element's computed background
  actually changes (env pill / KPI card / table / dialog / page-meta); (b) reload with
  `afc:theme=Dark` persists; (c) KPI count-up animates (value differs mid-animation, settles
  at target); (d) emulate `prefers-reduced-motion: reduce` and assert no animation;
  (e) zero console errors.
- Commit in logical chunks (tokens+refactor / dark theme + trigger / motion), push, confirm the
  latest `gh run list` is `success` AND `headSha == HEAD`.

## Out of scope (YAGNI)

No new pages or layout restructure; no dependency changes; no Chart.js live theme-reactivity
beyond fade-in (charts re-render on navigation). Overview dashboard + toasts, global data
search, and mobile/density polish are **separate later specs** (this is sub-project 1 of 4).

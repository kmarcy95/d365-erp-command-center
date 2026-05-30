# D365 ERP Command Center — Design Spec

**Date:** 2026-05-30
**Owner:** Keystone Marcy (kmarcy95)
**Status:** Approved (design) — proceeding to plan + build

## Purpose

Replace the thin Blazor **Server** "D365 Web" stub with a **robust, self-contained Blazor WebAssembly application** that runs **free on GitHub Pages** and serves as a credible portfolio piece evidencing the owner's skills as a **Business Analyst, Dynamics 365 ERP Analyst, Data Analyst, and Project Manager**. A hiring manager or recruiter can open one URL and explore a populated, interactive ERP + implementation toolkit with no login and nothing to install.

## Success criteria

- Live, free, no-login URL: `https://kmarcy95.github.io/d365-erp-command-center/`.
- Looks and feels like a real Microsoft/Dynamics 365 product (Fluent UI).
- Every one of the four target roles is clearly demonstrated by named modules.
- Demo data is **internally consistent** (e.g., AP aging totals reconcile to the GL AP control account) — the detail that signals genuine analyst rigor.
- Each data view supports sort / filter / search / CSV export; edits persist to localStorage; a "Reset demo data" action restores the seed.
- Builds clean (`dotnet build`), deploys via GitHub Actions, and works on desktop + mobile.

## Users

- **Primary:** recruiters and hiring managers for finance-systems / ERP / BA / data / PM roles.
- **Secondary:** the owner, as a live talking-point in interviews and on the Business Landing Page.

## Scope (in)

Self-contained demo only. Fictional company **Alamo Foods Co.** (San Antonio food/CPG manufacturer-distributor — a canonical ERP domain matching the owner's H-E-B / C.H. Guenther background).

Modules, grouped, mapped to roles:

- **Executive** — Executive Dashboard (KPI scorecards + charts), Financial Analytics (Trial Balance, P&L, Balance Sheet) → *Data Analyst*
- **Finance** — General Ledger, Accounts Payable, Accounts Receivable, Budget vs Actual → *D365 ERP Analyst*
- **Supply Chain** — Procurement, Inventory, Sales Orders → *D365 ERP Analyst*
- **Implementation** — Roadmap/Gantt (Success-by-Design phases), RAID log + risk heatmap, Fit-Gap Analysis, Requirements Traceability Matrix, Test/UAT Management, Status Reports → *Business Analyst + Project Manager*
- **Process** — Business Process Catalog (P2P / O2C / R2R) → *Business Analyst*
- **About** — what each module demonstrates + links to LinkedIn / résumé / landing pages

## Scope (out / YAGNI)

- No real backend, accounts, multi-user, or cloud database (would break $0 + adds no portfolio value).
- No AI/LLM calls (cost; not needed for the demo).
- No real D365 integration.
- Job Tracker app migration is a **separate** spec/plan.

## Architecture

- **Blazor WebAssembly, .NET 8 (LTS)** — static output, free on GitHub Pages.
- **Fluent UI Blazor** (`Microsoft.FluentUI.AspNetCore.Components`) for authentic Microsoft look. Fallback: MudBlazor if Fluent fights WASM+Pages.
- **`Blazored.LocalStorage`** for persistence.
- **Chart.js** via a thin JS-interop component for analytics visuals.
- Data layer: `Models/` records → `SeedData` generator → `AppData` service (in-memory repos hydrated/saved to localStorage, `ResetDemoData()`).
- Shared UI: app shell (`MainLayout` + grouped `NavMenu` + theme toggle + command palette), `DataGridView<T>` (sort/filter/search/page/CSV), `KpiCard`, `Chart`, `StatusBadge`, `PageHeader`.

## Hosting / deployment

- New repo `kmarcy95/d365-erp-command-center`; old `D365---WEB` + local `D365ERPManager.Web` untouched (non-destructive).
- GitHub Actions: `dotnet publish -c Release` → rewrite `<base href>` to `/d365-erp-command-center/` → add `.nojekyll` → copy `index.html`→`404.html` (SPA deep links) → `upload-pages-artifact` → `deploy-pages`. Pages source = GitHub Actions (`build_type=workflow`).

## Research basis

- **D365 F&O modules** (Microsoft Learn): GL, AP, AR, Budgeting, Cash & Bank; Procurement & Sourcing, Inventory, Product Information, Sales, Warehouse; financial dimensions (dept/cost center), workflow approvals.
- **Implementation (Success by Design / FastTrack):** phases Initiate → Implement → Prepare → Operate; BA artifacts = process catalog, fit-gap, FRD/FDD, RTM, config workbooks, data-migration mapping, UAT scripts; **RAID**; KPIs = budget vs actual, schedule variance, open risks/issues by severity, test pass rate, requirements coverage, go-live readiness.

## Risks / mitigations

- Fluent UI on WASM+Pages finicky → fall back to MudBlazor (changes look, not architecture).
- WASM first-load size is inherently larger → acceptable for a demo; enable Release trimming if needed.
- Base-href must exactly match the repo path or assets 404 → handled in the deploy workflow.

## Build phasing

1. Foundation (shell/theme/deploy live) → 2. Model + seed → 3. Executive dashboard → 4. Finance → 5. Supply chain → 6. Implementation → 7. Process catalog + About + polish + link from Business Landing Page. Phases 4–6 parallelizable via subagents.

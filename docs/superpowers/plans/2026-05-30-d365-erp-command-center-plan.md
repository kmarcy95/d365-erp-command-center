# D365 ERP Command Center — Implementation Plan

Spec: `docs/superpowers/specs/2026-05-30-d365-erp-command-center-design.md`

> Execute phase-by-phase. Each phase ends in a working, committed (and where noted, deployed) state. Verify `dotnet build` after every phase; verify the live URL after deploy checkpoints. Phases 4–6 are independent module groups → parallelizable via subagents once Phases 1–3 land the shared foundation.

## Commands
```
dotnet build                          # compile
dotnet run                            # local dev
dotnet publish -c Release -o publish  # wwwroot for Pages
gh api -X POST repos/kmarcy95/d365-erp-command-center/pages -f build_type=workflow
git push origin main                  # triggers deploy workflow
```

## Phase 1 — Foundation (live, empty-but-branded)
- [ ] `dotnet new blazorwasm --framework net8.0`; `git init`; `.gitignore` (bin/obj/*.db)
- [ ] Add `Microsoft.FluentUI.AspNetCore.Components` + `Blazored.LocalStorage`; register in `Program.cs`; `dotnet build` green
- [ ] `index.html` (title/meta, base href, app.css, Chart.js CDN, charts.js), `favicon.svg`
- [ ] App shell: `MainLayout` (Fluent header + body + footer), grouped `NavMenu`, light/dark theme toggle, brand "Alamo Foods Co. · D365 ERP Command Center"; Fluent providers in `App.razor`
- [ ] Brand theme in `app.css`; rich `Home` (Overview) page; `Placeholder` component + stub pages for every route so nav fully works
- [ ] `.github/workflows/pages.yml` (setup-dotnet 8 → publish → base-href rewrite → .nojekyll → 404.html → upload/deploy)
- [ ] Create repo, push, `gh api ... pages -f build_type=workflow`, confirm live
- [ ] **Checkpoint:** site loads at live URL; nav + theme toggle work; commit the spec/plan

## Phase 2 — Domain model + Alamo Foods seed
- [ ] `Models/`: GlAccount, JournalEntry, Vendor, Customer, ApInvoice, ArInvoice, BudgetLine, PurchaseOrder, Requisition, InventoryItem, Warehouse, SalesOrder, ProjectPhase, Milestone, RaidEntry, Requirement, FitGapItem, TestCase, StatusReport, ProcessFlow
- [ ] `Services/SeedData.cs`: coherent dataset — chart of accounts w/ balances; ~30 vendors/~30 customers; ~120 AP + ~120 AR invoices with aging that **ties to GL**; ~60 POs; ~90 items across 3 warehouses w/ valuation + reorder; ~50 sales orders; full project (4 phases, ~18 milestones, ~25 RAID, ~30 requirements + fit-gap + RTM, ~25 test cases)
- [ ] `Services/AppData.cs`: in-memory repos, localStorage load/save, `ResetDemoData()`; wire "Reset demo data" in header
- [ ] **Checkpoint:** data loads, persists across refresh, resets

## Phase 3 — Executive analytics [Data Analyst]
- [ ] `Chart` interop component (line/bar/doughnut/stacked) + `KpiCard`
- [ ] Executive Dashboard: KPI cards (Revenue YTD, Gross Margin %, AP/AR balances, DSO/DPO, Inventory turns, Cash, Go-live readiness) + charts (revenue trend, AP/AR aging, budget vs actual, top customers/vendors, inventory by category) + period filter
- [ ] Financial Analytics: Trial Balance, P&L, Balance Sheet from GL; CSV export
- [ ] **Checkpoint:** dashboard renders real charts; deploy

## Phase 4 — Finance [D365 ERP Analyst]
- [ ] `DataGridView<T>` shared component (sort/filter/search/page/CSV/status badges)
- [ ] General Ledger; Accounts Payable; Accounts Receivable; Budget vs Actual
- [ ] **Checkpoint:** grids work; edits persist; deploy

## Phase 5 — Supply Chain [D365 ERP Analyst]
- [ ] Procurement; Inventory; Sales Orders
- [ ] **Checkpoint:** deploy

## Phase 6 — Implementation [BA + PM]
- [ ] Roadmap/Gantt; RAID + risk heatmap; Fit-Gap; RTM; Test/UAT; Status Reports
- [ ] **Checkpoint:** deploy

## Phase 7 — Process catalog, About, polish
- [ ] Process Catalog (P2P/O2C/R2R); About page (persona mapping + contact links)
- [ ] Responsive pass, empty/loading states, command palette
- [ ] Screenshot + link the live app from Business Landing Page `/work/d365.html`
- [ ] **Checkpoint:** final deploy + cross-browser sanity

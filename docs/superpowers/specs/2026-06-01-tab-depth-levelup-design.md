# Tab-depth level-up — bring every page to gold-standard depth

**Date:** 2026-06-01
**Status:** Approved → in implementation
**Author:** brainstormed with user

## Problem

The finance / supply-chain pages (GL, AP, AR, Budget, Procurement, Inventory, Sales) are
deeply built — clickable KPI drill-downs, Chart.js with chart→table filtering, filter bars,
`PageMeta`/`Skeleton`, conditional row tints. The **Implementation / BA-PM pages** that the
portfolio is built to showcase (Roadmap, RAID, Fit-Gap, RTM, Test/UAT, Status Reports,
Process Catalog) plus Home/About are visibly thinner — most still use `FluentProgressRing`,
have no drill-downs, no filters, and no `PageMeta`. The user asked to "add more functionality
and fill up each tab with valuable information pertaining to the topic of the tab."

## Goal

Bring every thin page to the same depth standard as `GeneralLedger.razor` (the gold-standard
page), expand the seed data where richer content needs it, and tie the BA pages together with
a real **traceability spine** (Requirement ↔ Test case ↔ Process step). Conservative,
at-most-one-widget touch-ups on the already-rich pages so nothing working regresses.

## Reused building blocks (no new infrastructure beyond small typed dialogs)

- `KpiCard` with `Clickable` + `OnClick`.
- `Chart` with `OnLabelClick` (chart→table filter, toggle off on re-click).
- `PageMeta`, `Skeleton`, `EmptyState`, `ErrorBanner`, `StatusBadge`.
- **Generic** `ExecDrilldownDialog.Args` (Title/Subtitle/Kind/Labels/Data/Headers/Rows) for
  ALL KPI drill-downs — opened via `IDialogService.ShowDialogAsync<ExecDrilldownDialog>(args, params)`
  with a `"Page ▸ Title"` breadcrumb, `Width`, `PrimaryAction="Close"`, `SecondaryAction=null`.
- `Csv.Build` + `afcDownload` (CSV export), `afcPrint` (Print/PDF) + `.print-header`.
- New per-page CSS appended to the existing modernization block in `app.css`.

**Drill-down rule:** KPI drill-downs reuse the generic dialog. New small typed row-detail
dialogs ONLY where a row has rich sub-data — **4 new dialogs**: `MilestoneDetailDialog`,
`RaidEntryDetailDialog`, `RequirementDetailDialog` (→ linked tests), `TestCaseDetailDialog`
(→ run history). Mirror `GlAccountDetailDialog` (`IDialogContentComponent<T>`, `.dlg-content`,
`.dlg-facts`, `.dlg-section`, `.afc-table.dlg-table`).

## Seed-data expansions (Models.cs + SeedData.cs) — **Data Version 4 → 5**

Bump `DemoData.Version` to 5 and the `AppData` load guard to `>= 5` so every visitor auto-reseeds.

- **Milestone:** `+ List<string> Deliverables`, `+ int PercentComplete`, `+ string Health`.
- **RaidEntry:** `+ DateOnly Raised`, `+ DateOnly? TargetDate`, `+ string LastUpdate`.
- **Requirement:** `+ string Owner`, `+ string Status` (Approved/In Review/Draft), `+ string Rationale`.
- **TestCase:** `+ List<string> Steps`, `+ List<TestRun> Runs` (Date/Result/Tester), `+ string? Defect`.
  New `TestRun` model. The latest run must match `Result` and `LastRun` (tie-out).
- **ProcessStep:** `+ string Description`, `+ string Control`, `+ double CycleTimeHrs`,
  `+ string[] RequirementCodes` (reference real requirement codes). **ProcessFlow:** `+ string Owner`.

All deterministic via the existing `Rng`/`Pick` helpers; no change to finance tie-outs.

## Per-page work

### Phase 2 — traceability spine
- **Fit-Gap:** Skeleton/PageMeta/ErrorBanner; clickable KPIs (+ "Total effort (days)") → generic
  drill-down; existing doughnut made clickable + effort-by-area bar; area/priority/fit-gap/process
  filter bar; requirement row → `RequirementDetailDialog` (rationale + linked test cases & status).
- **RTM:** clickable KPIs; coverage-by-area bar + test-result doughnut (clickable→filter);
  area/process/test-status filters; requirement row → `RequirementDetailDialog` (core traceability).
- **Test/UAT:** clickable KPIs (+ Execution %, Defects); results doughnut (clickable) +
  by-process-area bar + execution-trend line (from run history); process/result/tester filters;
  case row → `TestCaseDetailDialog` (steps, run history, linked requirement, defect).

### Phase 3 — PM pages
- **Roadmap:** clickable KPIs; `% complete by phase` bar (click→filter milestones) +
  milestone-status doughnut; phase/status filter bar; milestone row → `MilestoneDetailDialog`
  (deliverables, % complete, owner, due, health).
- **RAID:** clickable KPIs; **clickable heatmap cells** → filter to that prob×impact bucket;
  by-type doughnut + by-status bar (click→filter); type/severity/status/owner filters;
  entry row → `RaidEntryDetailDialog` (response, target date, aging, owner).
- **Status Reports:** Skeleton/PageMeta; **Print/PDF** + print header; milestone-status &
  phase-% charts; auto "Accomplishments since last report" (recently completed milestones) +
  "Next-period focus" (upcoming milestones / open high-risks); richer Copy-report text.

### Phase 4 — Process Catalog + Home + About
- **Process Catalog:** Skeleton/PageMeta; KPIs (processes, total steps, systems touched, roles,
  avg cycle time); each step card enriched (description/system/role/control/cycle-time);
  flow summary (owner, total cycle time, #controls, linked-requirements count); process filter.
- **Home:** Project Health band — overall RAG, % complete, open high-risks, test pass-rate,
  go-live countdown — as KPI tiles linking to relevant pages; persona cards become links.
- **About:** skills/competency matrix (BA/PM/Data/D365 chips), tech-stack row, "what this demo
  proves" capability→page map; keep LinkedIn / Business Landing Page links.

### Phase 5 — conservative rich-page touch-ups
Audit Exec / Financial Analytics / GL / AP / AR / Budget / Procurement / Inventory / Sales.
Add **at most one** high-value widget where there's a clear gap (e.g., Exec dashboard gets the
go-live countdown / project-health tie-in). Otherwise leave intact — do not regress working pages.

## Build / rollout (honors the pre-push gate)

Phased; each phase ends green and commits markup + `@code` together:
1. Seed/model expansion + Version bump → 2. Fit-Gap/RTM/Test → 3. Roadmap/RAID/Status →
4. Process Catalog/Home/About → 5. rich-page touch-ups + final verify.

Each phase: `rm -rf obj bin && dotnet build -c Release` MUST report "Build succeeded" before
commit (incremental Debug can be green while CI's clean Release fails). Headless click-through
before declaring done. **Push/deploy held until the user approves** (public, outward-facing).
After any push: confirm latest `gh run` is `success` AND `headSha == HEAD`.

## Gotchas to honor (from CLAUDE.md)

- `SortBy="@(GridSort<T>.ByAscending(...))"` MUST be paren-wrapped (RZ9986).
- Cast mismatched ternary branches to `(object)` (CS0173/CS8654) in Chart.js anon configs.
- Dialog content is a plain `<div class="dlg-content">` — the service supplies header/Close.
- Commit each page's markup + `@code` together (a split commit broke a Release deploy).
- A page file named like a model type shadows it — keep page/dialog names distinct from models.

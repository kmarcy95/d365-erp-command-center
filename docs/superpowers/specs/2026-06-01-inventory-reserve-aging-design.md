# Inventory → Reserve & Aging sub-tab

**Date:** 2026-06-01
**Status:** Approved → implementing (WS1 of 4)
**AI approach:** rule-based Smart Insights (no backend; static GitHub Pages can't hold an API key)

## Goal
Add a detailed, methodical **Reserve & Aging** sub-tab under Inventory that computes an
**excess-&-obsolete (E&O) inventory reserve** from stock aging, shows reserve/aging/turns
**trends**, and surfaces **Smart Insights** (deterministic, plain-English recommendations on
how to better manage inventory). It must read like a worksheet a real controller/planner uses.

## Sub-tab navigation
Inventory becomes a two-tab area via a shared segmented control (`Components/InventorySubnav.razor`):
- **Overview** → `/inventory` (existing page, add the subnav at top)
- **Reserve & Aging** → `/inventory/reserve` (new page)
Both render the subnav under the page header. Add `/inventory/reserve` to the command palette.

## Data model (Models.cs) — **Data Version 5 → 6** (AppData guard `>=6`)
- `InventoryItem` gains `DateOnly LastReceiptDate` (the date the on-hand stock was last
  replenished — drives aging). Age is computed against `AsOf` in code, not stored.
- New `ReserveBand { string Label; int MinDays; int MaxDays; int RatePct; }`.
- New `ReservePolicy { List<ReserveBand> Bands; }` — the editable E&O provisioning matrix.
  Default bands: `0–30 = 0%`, `31–60 = 5%`, `61–90 = 25%`, `91–180 = 50%`, `181+ = 100%`.
- New `InventorySnapshot { DateOnly AsOf; decimal OnHandValue; decimal ReserveValue; double Turns; decimal[] BucketValues; }`.
- `DemoData` gains `ReservePolicy ReservePolicy` and `List<InventorySnapshot> InventorySnapshots`.

## Seed (SeedData.cs)
- `BuildInventory`: set `LastReceiptDate = AsOf - ageDays`, where `ageDays` correlates with stock
  state so aging is realistic (Excess → 120–400d, Healthy → 10–90d, Low/Critical → 5–45d, + jitter).
- New `BuildReserve(d)`: set the default `ReservePolicy`; generate **12 monthly snapshots** walking
  back from `AsOf`. The latest snapshot equals the value computed from current inventory + default
  policy (ties out to the page); prior months vary on a slight trend so the lines are meaningful.

## Reserve math (Services/ReserveCalc.cs — pure, deterministic)
- `int Age(item, asOf)` and `ReserveBand Band(policy, ageDays)` (first band whose `[Min,Max]` contains age).
- Per item: `ageDays`, `band`, `reserveRate = band.RatePct`, `reserveAmount = Value * rate/100`,
  `netValue = Value − reserveAmount`. Roll-ups: total on-hand value, total reserve, **E&O % = reserve ÷ on-hand**,
  181+ value, total NRV.
- `List<Insight> Insights(items, policy, snapshots, asOf)` — rules below.

## Page: `Pages/InventoryReserve.razor` (`/inventory/reserve`)
- Subnav + header (scale toggle `$/$K/$M`, Print/PDF, Export CSV), PageMeta, Skeleton/Error.
- **KPIs (clickable → `InventoryDrilldownDialog`):** On-hand value · Reserve $ · E&O % · 181+ value · Net realizable value.
- **Reserve policy editor:** one editable rate input (0–100) per band + "Reset to default"; recompute
  live on change and persist via `AppData.SaveAsync()` (the policy lives in the dataset).
- **Charts:** (1) value by age bucket — stacked Net vs Reserve, click bucket → filter table;
  (2) Reserve $ trend (line, 12 months); (3) Aging-mix migration (stacked bar over the 12 snapshots).
- **Filters:** category · warehouse · age bucket · ABC · search. Conditional row tint: 91–180 warn, 181+ bad.
- **Aging table:** SKU · Name · Category · ABC · Age (days) · Bucket · On-hand value · Reserve % · Reserve $ · Net value.
  Row → existing `InventoryItemDetailDialog` (consistent with Overview).
- **Smart Insights panel:** prioritized cards (severity High/Med/Low, title, detail, $ impact, suggested action).

### Smart Insights rules (deterministic, sorted by $ impact, cap ~6)
1. **Obsolete (181+):** total value + top SKUs → recommend write-off/liquidation.
2. **Migrating (91–180):** value approaching full reserve → forecast next-period reserve increase.
3. **Excess capital:** high days-of-supply / low turns value tied up → slow purchasing.
4. **Concentration:** category (or SKU) holding the largest share of total reserve.
5. **Reserve trend:** current reserve vs prior snapshot (↑/↓ $ and %).
6. **Below-cost / NRV watch:** SKUs with `UnitPrice ≤ UnitCost` → markdown/NRV write-down risk.
7. **Coverage:** E&O % vs an 8% policy benchmark (over/under-reserved signal).

## Build / rollout
Phased, each ends green and commits markup+`@code` together; clean Release gate before any push:
1. Model + seed + Version bump + ReserveCalc → 2. Subnav + wire into Inventory → 3. Reserve page
(KPIs/policy/charts/table) → 4. Smart Insights → 5. palette + CSS + verify. Push held until user approves deploy.

## Gotchas honored
`SortBy="@(GridSort<T>...)"` paren-wrapped; cast mismatched ternary branches `(object)`; dialog content
in `.dlg-content`; commit markup + `@code` together; clean Release build (not incremental Debug) before push.

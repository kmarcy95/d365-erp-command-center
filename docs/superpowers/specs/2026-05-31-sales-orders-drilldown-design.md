# Sales Orders — KPI drill-down, row detail & charts

**Date:** 2026-05-31
**Page:** `/sales-orders` (`Pages/SalesOrders.razor`)
**Status:** Approved design

## Goal

Make the Sales Orders page interactive for portfolio demonstration:

1. **Clickable KPI cards** — each of the 4 KPI cards opens an in-page dialog showing a
   Chart.js visualization plus the filtered orders behind that metric.
2. **Clickable order rows** — each row opens an in-page dialog with the order header facts
   and a synthesized line-item breakdown.
3. **Sorting & charts** — columns sort (already enabled); the page lands sorted by Order Date
   descending; charts live inside the KPI drill-down dialogs.

There is no KPI *bug* — the existing `OnInitializedAsync` computes values correctly. This is
purely additive functionality.

## Mechanism

In-page dialogs via Fluent UI's `IDialogService` (already available — `AddFluentUIComponents()`
in `Program.cs`, `<FluentDialogProvider />` in `App.razor`). No new routes, no persistence
changes, no editing. Line items are display-only.

## Data model changes (`Models/Models.cs`)

Add a line-item type and attach a list to `SalesOrder`:

```csharp
public class SalesLine
{
    public string Product { get; set; } = "";
    public string Category { get; set; } = "";
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Math.Round(Qty * UnitPrice, 2);
}
```

`SalesOrder` gains `public List<SalesLine> Lines { get; set; } = new();`. The existing `Total`
property stays and now equals `Lines.Sum(l => l.LineTotal)` — semantics are unchanged, so the
GL/trial-balance derivation is unaffected.

## Seed changes (`Services/SeedData.cs`)

In `BuildSalesOrders`, replace the single random `Total` with 2–5 generated lines drawn from
an Alamo-Foods product catalog (product + category + qty + unit price), all via the existing
deterministic `Rng`/`Money`/`Pick` helpers. Set `Total = order.Lines.Sum(l => l.LineTotal)`.
Catalog products are food-manufacturer SKUs (e.g. "Mesquite BBQ Sauce", "Corn Tortilla Flour",
"Jalapeño Brine") with sensible categories ("Sauces & Marinades", "Flour & Grains", etc.).

Because `Random` is seeded (`new(42)`) and call order is deterministic, the dataset stays
reproducible. Note: adding line-generation calls shifts later `Rng` draws, so downstream
seeded values (budget, etc.) will change numerically but remain internally consistent.

## Component changes

### `Components/KpiCard.razor`
Add optional click support, off by default (existing non-clickable usages unaffected):
- `[Parameter] public bool Clickable { get; set; }`
- `[Parameter] public EventCallback OnClick { get; set; }`
- When `Clickable`, add class `kpi-card--clickable`, `role="button"`, `tabindex="0"`,
  `@onclick`, and `@onkeydown` handling Enter/Space.

### `Components/KpiDrilldownDialog.razor` (new)
Implements `IDialogContentComponent<KpiDrilldownDialog.Args>` and `IAsyncDisposable`.
`Args { string Title; string Subtitle; List<SalesOrder> Orders; ChartSpec Chart; }` where
`ChartSpec` describes the chart kind + labels + data the page computed. Renders:
- a one-line summary,
- a `<div class="chart-canvas-wrap"><canvas id="..."></canvas></div>`,
- a compact `afc-table` of the contributing orders.

Charts render in `OnAfterRenderAsync(firstRender)` via `JS.InvokeVoidAsync("afcCharts.render", id, config)`
and are destroyed in `DisposeAsync` via `afcCharts.destroy`. Canvas id is unique per dialog
instance (Guid suffix).

### `Components/OrderDetailDialog.razor` (new)
Implements `IDialogContentComponent<SalesOrder>`. Renders header facts (number, customer,
segment, order/ship dates, status badge) and an `afc-table` of `Lines`
(product / category / qty / unit price / line total) with a footer total.

## Page changes (`Pages/SalesOrders.razor`)

- `@inject IDialogService DialogService`.
- KPI cards: `Clickable="true"` + `OnClick` handlers that build the relevant
  `KpiDrilldownDialog.Args` and call `DialogService.ShowDialogAsync<KpiDrilldownDialog>(args, params)`.
  - **Open Order Value** → `Status != Invoiced`; bar chart: open value by status.
  - **Sales Orders** → all; doughnut: count by status.
  - **Shipped** → Shipped + Invoiced; bar: count by month (order date).
  - **Customers** → distinct customers; bar: top-10 customers by total value.
- Rows: open `OrderDetailDialog` for the clicked `SalesOrder` (via `OnRowClick` on
  `FluentDataGrid`, or a template action). Add a row hover affordance.
- Default sort: Order Date descending on load.

## CSS (`wwwroot/css/app.css`)

Add `.kpi-card--clickable { cursor: pointer; transition: transform .08s ease, box-shadow .12s ease; }`
and a hover/focus lift. Add a clickable-row affordance for the grid. Reuse existing
`.chart-canvas-wrap`, `.afc-table`, `.kpi-grid`, `.grid-wrap`.

## Verification

- `dotnet build` (Debug) is green.
- `dotnet run`, open `/sales-orders`: click each KPI → dialog with correct chart + order list;
  click a row → order detail with line items summing to the order total; columns sort; page
  loads sorted by Order Date desc.
- Push to `main`; GitHub Actions does the Release publish + Pages deploy.

## Out of scope (YAGNI)

No new routes/deep links, no order editing, no extra persistence, no changes to other pages.

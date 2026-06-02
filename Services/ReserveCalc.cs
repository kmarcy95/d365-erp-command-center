using D365CommandCenter.Models;

namespace D365CommandCenter.Services;

/// <summary>Severity of a Smart Insight recommendation.</summary>
public enum InsightLevel { High, Medium, Low }

/// <summary>A single rule-based inventory recommendation.</summary>
public class Insight
{
    public InsightLevel Level { get; set; }
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public decimal Impact { get; set; }            // $ at stake — used for ranking
    public string Action { get; set; } = "";
}

/// <summary>
/// Pure, deterministic excess-&-obsolete (E&O) reserve math + a rule-based
/// "Smart Insights" engine. No backend / LLM — runs entirely client-side.
/// </summary>
public static class ReserveCalc
{
    public static int Age(InventoryItem it, DateOnly asOf) => asOf.DayNumber - it.LastReceiptDate.DayNumber;

    public static ReserveBand Band(ReservePolicy policy, int ageDays)
        => policy.Bands.FirstOrDefault(b => b.Contains(ageDays)) ?? policy.Bands[^1];

    public static int RatePct(ReservePolicy policy, int ageDays) => Band(policy, ageDays).RatePct;

    /// <summary>Reserve amount for one item at the given policy.</summary>
    public static decimal Reserve(InventoryItem it, ReservePolicy policy, DateOnly asOf)
        => Math.Round(it.Value * RatePct(policy, Age(it, asOf)) / 100m, 2);

    /// <summary>Net realizable value = on-hand value less reserve.</summary>
    public static decimal NetValue(InventoryItem it, ReservePolicy policy, DateOnly asOf)
        => it.Value - Reserve(it, policy, asOf);

    /// <summary>Generates prioritized recommendations from the current book + history.</summary>
    public static List<Insight> Insights(
        IReadOnlyList<InventoryItem> items,
        ReservePolicy policy,
        IReadOnlyList<InventorySnapshot> snaps,
        DateOnly asOf)
    {
        var list = new List<Insight>();
        if (items.Count == 0) return list;

        decimal onHand = items.Sum(i => i.Value);
        decimal totalReserve = items.Sum(i => Reserve(i, policy, asOf));
        decimal eandoPct = onHand == 0 ? 0 : Math.Round(totalReserve / onHand * 100, 1);

        // 1. Obsolete (top band, 181+)
        var topBand = policy.Bands[^1];
        var obsolete = items.Where(i => Age(i, asOf) >= topBand.MinDays).ToList();
        if (obsolete.Count > 0)
        {
            decimal v = obsolete.Sum(i => i.Value);
            var top = obsolete.OrderByDescending(i => i.Value).Take(3).Select(i => i.Sku);
            list.Add(new Insight
            {
                Level = InsightLevel.High,
                Title = $"Write off or liquidate {obsolete.Count} obsolete SKUs",
                Detail = $"{Fmt.Money(v)} of stock is {topBand.MinDays}+ days old and fully reserved (e.g. {string.Join(", ", top)}). It is consuming warehouse space and carrying cost with little chance of sale.",
                Impact = v,
                Action = "Dispose, donate, or run a clearance markdown; remove from active replenishment."
            });
        }

        // 2. Migrating into the 91-180 band (about to attract a higher reserve)
        var migrating = items.Where(i => { int a = Age(i, asOf); return a >= 91 && a <= 180; }).ToList();
        if (migrating.Count > 0)
        {
            decimal v = migrating.Sum(i => i.Value);
            decimal nowRes = migrating.Sum(i => Reserve(i, policy, asOf));
            decimal fullRes = v; // becomes 100% reserved if it ages into the top band
            decimal delta = Math.Max(0, fullRes - nowRes);
            list.Add(new Insight
            {
                Level = InsightLevel.Medium,
                Title = $"{migrating.Count} SKUs aging toward full reserve",
                Detail = $"{Fmt.Money(v)} sits in the 91–180 day band. If it isn't sold, the reserve on it rises by about {Fmt.Money(delta)} as it crosses 180 days.",
                Impact = delta,
                Action = "Prioritize these for promotion, bundling, or transfer before they obsolete."
            });
        }

        // 3. Excess capital tied up in slow stock
        var excess = items.Where(i => i.State == StockState.Excess).ToList();
        if (excess.Count > 0)
        {
            decimal v = excess.Sum(i => i.Value);
            list.Add(new Insight
            {
                Level = InsightLevel.Medium,
                Title = $"{Fmt.Money(v)} of working capital tied up in excess stock",
                Detail = $"{excess.Count} SKUs hold more than 120 days of supply. This is cash on the shelf that could fund faster-moving lines.",
                Impact = v,
                Action = "Cut or pause purchase orders on these items and let demand draw stock down."
            });
        }

        // 4. Reserve concentration by category
        var byCat = items.GroupBy(i => i.Category)
            .Select(g => new { Cat = g.Key, Res = g.Sum(i => Reserve(i, policy, asOf)) })
            .Where(x => x.Res > 0)
            .OrderByDescending(x => x.Res).ToList();
        if (byCat.Count > 0 && totalReserve > 0)
        {
            var lead = byCat[0];
            decimal share = Math.Round(lead.Res / totalReserve * 100, 0);
            if (share >= 35)
                list.Add(new Insight
                {
                    Level = InsightLevel.Low,
                    Title = $"{lead.Cat} drives {share}% of the total reserve",
                    Detail = $"{Fmt.Money(lead.Res)} of the {Fmt.Money(totalReserve)} reserve comes from one category. Concentrated risk is easier to manage with a targeted plan.",
                    Impact = lead.Res,
                    Action = $"Build a category-specific sell-through plan for {lead.Cat}."
                });
        }

        // 5. Reserve trend vs the prior month snapshot
        if (snaps.Count >= 2)
        {
            var prev = snaps[^2];
            decimal delta = totalReserve - prev.ReserveValue;
            if (Math.Abs(delta) >= onHand * 0.005m)
            {
                bool up = delta > 0;
                list.Add(new Insight
                {
                    Level = up ? InsightLevel.Medium : InsightLevel.Low,
                    Title = up ? $"Reserve grew {Fmt.Money(Math.Abs(delta))} month-over-month"
                               : $"Reserve fell {Fmt.Money(Math.Abs(delta))} month-over-month",
                    Detail = up
                        ? $"E&O provision rose from {Fmt.Money(prev.ReserveValue)} to {Fmt.Money(totalReserve)} — stock is aging faster than it is selling."
                        : $"E&O provision improved from {Fmt.Money(prev.ReserveValue)} to {Fmt.Money(totalReserve)} — recent sell-through is working.",
                    Impact = Math.Abs(delta),
                    Action = up ? "Investigate which categories are aging and tighten buying."
                                : "Keep the current sell-through tactics in place."
                });
            }
        }

        // 6. Below-cost / NRV markdown risk
        var belowCost = items.Where(i => i.UnitPrice <= i.UnitCost).ToList();
        if (belowCost.Count > 0)
        {
            decimal v = belowCost.Sum(i => i.Value);
            list.Add(new Insight
            {
                Level = InsightLevel.Medium,
                Title = $"{belowCost.Count} SKUs priced at or below cost",
                Detail = $"{Fmt.Money(v)} of on-hand value is priced at or under unit cost — net realizable value may be below carrying value, requiring an additional write-down.",
                Impact = v,
                Action = "Re-price or renegotiate cost; assess a lower-of-cost-or-market adjustment."
            });
        }

        // 7. Coverage vs an 8% benchmark
        const decimal benchmark = 8m;
        if (eandoPct >= benchmark * 1.25m)
            list.Add(new Insight
            {
                Level = InsightLevel.High,
                Title = $"E&O reserve is {eandoPct}% of inventory — above the {benchmark}% benchmark",
                Detail = $"A reserve this high signals an aging, slow-moving book. Healthy operations typically run near {benchmark}%.",
                Impact = totalReserve,
                Action = "Launch a coordinated clearance program and tighten purchasing limits."
            });
        else if (eandoPct <= benchmark * 0.4m)
            list.Add(new Insight
            {
                Level = InsightLevel.Low,
                Title = $"E&O reserve is a healthy {eandoPct}% of inventory",
                Detail = $"The book is fresh and turning well, comfortably under the {benchmark}% benchmark.",
                Impact = 0,
                Action = "Maintain current planning discipline."
            });

        return list.OrderByDescending(i => i.Level == InsightLevel.High)
                   .ThenByDescending(i => i.Impact)
                   .Take(6).ToList();
    }
}

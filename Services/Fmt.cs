using System.Globalization;

namespace D365CommandCenter;

/// <summary>Display scale for money figures (raw $, thousands, millions).</summary>
public enum MoneyScale { Units, Thousands, Millions }

/// <summary>Culture-stable formatting helpers (WASM default culture varies).</summary>
public static class Fmt
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Money(decimal v) => "$" + v.ToString("#,##0", Inv);
    public static string Money1(decimal v) => "$" + v.ToString("#,##0.00", Inv);
    public static string Num(decimal v) => v.ToString("#,##0", Inv);
    public static string Pct(decimal v) => v.ToString("0.0", Inv) + "%";

    /// <summary>Signed money with accounting-style parentheses for negatives, e.g. ($12,450).</summary>
    public static string Signed(decimal v)
        => v < 0 ? "($" + (-v).ToString("#,##0", Inv) + ")" : "$" + v.ToString("#,##0", Inv);

    /// <summary>Signed percent to one decimal, e.g. +4.2% / -8.1%.</summary>
    public static string SignedPct(decimal v)
        => (v > 0 ? "+" : "") + v.ToString("0.0", Inv) + "%";

    /// <summary>Short unit suffix for a scale ("" / "K" / "M").</summary>
    public static string ScaleSuffix(MoneyScale s) => s switch
    {
        MoneyScale.Thousands => "K",
        MoneyScale.Millions => "M",
        _ => ""
    };

    /// <summary>Money rendered at the chosen scale, accounting-signed. e.g. ($1.2M), $980K, $1,284.</summary>
    public static string Money(decimal v, MoneyScale scale)
    {
        bool neg = v < 0;
        decimal a = Math.Abs(v);
        string body = scale switch
        {
            MoneyScale.Thousands => "$" + (a / 1_000m).ToString("#,##0.#", Inv) + "K",
            MoneyScale.Millions => "$" + (a / 1_000_000m).ToString("#,##0.##", Inv) + "M",
            _ => "$" + a.ToString("#,##0", Inv)
        };
        return neg ? "(" + body + ")" : body;
    }
}

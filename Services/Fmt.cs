using System.Globalization;

namespace D365CommandCenter;

/// <summary>Culture-stable formatting helpers (WASM default culture varies).</summary>
public static class Fmt
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Money(decimal v) => "$" + v.ToString("#,##0", Inv);
    public static string Money1(decimal v) => "$" + v.ToString("#,##0.00", Inv);
    public static string Num(decimal v) => v.ToString("#,##0", Inv);
    public static string Pct(decimal v) => v.ToString("0.0", Inv) + "%";
}

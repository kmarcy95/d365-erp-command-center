namespace D365CommandCenter.Services;

/// <summary>Static app/release metadata surfaced in the footer and meta strips.</summary>
public static class AppInfo
{
    public const string Version = "3.1.0";
    public const string Build = "2026.06.01";
    public const string BaseCurrency = "USD";
    public const string FiscalCalendar = "Calendar FY (Jan–Dec)";
    public const string Platform = "Dynamics 365 Finance & Operations";
}

/// <summary>Derives plausible fiscal-period labels from the dataset "as of" date.</summary>
public static class Fiscal
{
    /// <summary>e.g. "May FY26".</summary>
    public static string Period(DateOnly asOf)
        => $"{asOf:MMM} FY{asOf.Year % 100:00}";

    /// <summary>The reporting period is treated as open; prior periods are closed.</summary>
    public static string PeriodStatus(DateOnly asOf) => "Open";

    /// <summary>"May FY26 (Open)".</summary>
    public static string PeriodLabel(DateOnly asOf) => $"{Period(asOf)} ({PeriodStatus(asOf)})";

    /// <summary>Human "x hours/minutes ago" relative to now, from a UTC timestamp.</summary>
    public static string Relative(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span < TimeSpan.Zero) span = TimeSpan.Zero;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} hr ago";
        return $"{(int)span.TotalDays} d ago";
    }
}

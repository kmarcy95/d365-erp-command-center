namespace D365CommandCenter.Components;

/// <summary>One labelled data series for the <see cref="Chart"/> component.</summary>
public record ChartSeries(string Label, double[] Data, string? Color = null);

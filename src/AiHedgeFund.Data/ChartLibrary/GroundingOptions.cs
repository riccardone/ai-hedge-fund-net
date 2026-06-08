namespace AiHedgeFund.Data.ChartLibrary;

/// <summary>
/// Configuration for optional historical base-rate grounding. Off by default; the
/// provider is only <see cref="Active"/> when grounding is explicitly enabled AND an
/// API key is present. Bound from the "Grounding" configuration section.
/// </summary>
public sealed class GroundingOptions
{
    public const string SectionName = "Grounding";

    public bool Enabled { get; set; } = false;
    public string BaseUrl { get; set; } = "https://chartlibrary.io";
    public string? ApiKey { get; set; }
    public string Timeframe { get; set; } = "rth";
    public int[] Horizons { get; set; } = { 1, 5, 10 };
    public int OverfitHorizon { get; set; } = 5;
    public double TimeoutSeconds { get; set; } = 8.0;
    public bool ExcludeSameSymbolDays { get; set; } = true;

    public bool HasKey => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>Grounding only does anything when both enabled and keyed.</summary>
    public bool Active => Enabled && HasKey;
}

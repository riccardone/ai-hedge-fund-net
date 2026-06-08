using System.Globalization;

namespace AiHedgeFund.Contracts.Model;

/// <summary>
/// A historical base rate for a (symbol, date, horizon): the calibrated forward-return
/// distribution of the cohort of historical analogs. Purely advisory context — it never
/// changes an agent's decision, it only grounds the agent's reasoning in what analogous
/// setups actually did next.
/// </summary>
public sealed class BaseRate
{
    public required string Asset { get; init; }
    public required string AsOf { get; init; }
    public int Horizon { get; init; }

    public double MedianPct { get; init; }
    public double P10Pct { get; init; }
    public double P90Pct { get; init; }

    public int? N { get; init; }
    public double? Coverage { get; init; }
    public int? CoverageN { get; init; }
    public bool Calibrated { get; init; }
    public string Source { get; init; } = "chartlibrary:cohort_analyze";

    private static string Pct(double value) =>
        value.ToString("+0.0;-0.0", CultureInfo.InvariantCulture) + "%";

    private static string Count(int value) =>
        value.ToString("N0", CultureInfo.InvariantCulture);

    /// <summary>
    /// One plain-English line suitable for injecting into an LLM prompt as grounding context.
    /// </summary>
    public string ToContextLine()
    {
        var kind = Calibrated ? "calibrated" : "raw";
        var line = $"Historical base rate for {Asset} as of {AsOf} ({Horizon}-day horizon): " +
                   $"median {Pct(MedianPct)}, {kind} 80% band [{Pct(P10Pct)}, {Pct(P90Pct)}]";

        if (N is > 0)
            line += $" from {Count(N.Value)} analogous setups";

        if (Coverage is { } cov && CoverageN is > 0)
        {
            var covPct = (cov * 100).ToString("0.0", CultureInfo.InvariantCulture);
            line += $"; calibration held {covPct}% across {Count(CoverageN.Value)} cases";
        }

        return line + ". Context only, not a forecast.";
    }

    /// <summary>
    /// Returns an advisory note when a directional signal runs counter to a clearly-signed
    /// base rate, otherwise null. This is a reality-check the caller may append to its
    /// reasoning — it is informational and must not change the signal itself.
    /// </summary>
    public string? AssessConflict(string? signal)
    {
        const double flat = 0.10; // |median| below this is treated as no directional lean

        var s = signal?.Trim().ToLowerInvariant();
        var up = s is "bullish" or "buy" or "long";
        var down = s is "bearish" or "sell" or "short";
        if (!up && !down)
            return null;

        if (Math.Abs(MedianPct) < flat)
            return null;

        var against = (up && MedianPct < 0) || (down && MedianPct > 0);
        if (!against)
            return null;

        var cohort = N is > 0 ? $" across {Count(N.Value)} analogous setups" : string.Empty;
        return $"Base-rate reality-check: this {s} call runs counter to the historical base rate " +
               $"(median {Pct(MedianPct)} over {Horizon} days{cohort}). Historical context only, " +
               "not a directional forecast — the signal above is unchanged.";
    }
}

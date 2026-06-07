using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AiHedgeFund.Contracts.Model;

namespace AiHedgeFund.Data.ChartLibrary;

/// <summary>
/// Parses a Chart Library cohort_analyze response into a <see cref="BaseRate"/>.
/// Deliberately defensive: the response envelope shape is not contractually fixed,
/// so it walks the whole tree, prefers the conformally calibrated band over the raw
/// one, and matches the requested horizon on a digit boundary. Any malformed or
/// missing data yields null rather than throwing.
/// </summary>
public static class BaseRateParser
{
    private static readonly string[] CountNames =
        { "cohort_size", "n_matches", "n_cohort", "n", "count", "sample_size" };

    public static BaseRate? Parse(string json, string asset, string asOf, int horizon)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return ParseRoot(doc.RootElement, asset, asOf, horizon);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static BaseRate? ParseRoot(JsonElement root, string asset, string asOf, int horizon)
    {
        var found = FindDistribution(root, horizon);
        if (found is null)
            return null;

        var (path, dist) = found.Value;
        var center = Center(dist);
        if (center is null)
            return null;
        if (!TryGetNum(dist, "p10", out var p10) || !TryGetNum(dist, "p90", out var p90))
            return null;

        double? coverage = null;
        int? coverageN = null;
        var calBlock = FindCalibration(root);
        if (calBlock is { } cal)
        {
            if (TryGetNum(cal, "empirical_coverage", out var cov) ||
                TryGetNum(cal, "observed_coverage", out cov))
                coverage = cov;
            if (TryGetInt(cal, "sample_size", out var cn) ||
                TryGetInt(cal, "n", out cn) ||
                TryGetInt(cal, "coverage_n", out cn))
                coverageN = cn;
        }

        return new BaseRate
        {
            Asset = asset,
            AsOf = asOf,
            Horizon = horizon,
            MedianPct = center.Value,
            P10Pct = p10,
            P90Pct = p90,
            N = FindInt(root, CountNames),
            Coverage = coverage,
            CoverageN = coverageN,
            Calibrated = path.ToLowerInvariant().Contains("calibrated_return_pct"),
        };
    }

    private static (string Path, JsonElement Dist)? FindDistribution(JsonElement root, int horizon)
    {
        (string, JsonElement)? best = null;
        var bestScore = -1;
        var htok = horizon.ToString(CultureInfo.InvariantCulture);
        // Digit-boundary match so horizon 1 doesn't also match a "/10" path segment.
        var horizonRe = new Regex($@"(?<!\d){Regex.Escape(htok)}(?!\d)");

        foreach (var (p, d) in Walk(root, ""))
        {
            if (!IsDistribution(d))
                continue;

            var lp = p.ToLowerInvariant();
            var score = 0;
            if (lp.Contains("calibrated_return_pct"))
                score += 4;
            else if (lp.Contains("return_pct"))
                score += 2;
            if (horizonRe.IsMatch(lp))
                score += 3;

            if (score > bestScore)
            {
                best = (p, d);
                bestScore = score;
            }
        }

        return best;
    }

    private static IEnumerable<(string Path, JsonElement Obj)> Walk(JsonElement el, string path)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                yield return (path, el);
                foreach (var prop in el.EnumerateObject())
                    foreach (var item in Walk(prop.Value, $"{path}/{prop.Name}"))
                        yield return item;
                break;
            case JsonValueKind.Array:
                var i = 0;
                foreach (var v in el.EnumerateArray())
                {
                    foreach (var item in Walk(v, $"{path}/{i}"))
                        yield return item;
                    i++;
                }
                break;
        }
    }

    private static bool IsDistribution(JsonElement d) =>
        Has(d, "p10") && Has(d, "p90") &&
        (Has(d, "p50") || Has(d, "median") || Has(d, "mean"));

    private static double? Center(JsonElement d)
    {
        if (TryGetNum(d, "p50", out var v)) return v;
        if (TryGetNum(d, "median", out v)) return v;
        if (TryGetNum(d, "mean", out v)) return v;
        return null;
    }

    private static int? FindInt(JsonElement root, string[] names)
    {
        foreach (var (_, d) in Walk(root, ""))
            foreach (var nm in names)
                if (TryGetInt(d, nm, out var v) && v > 0)
                    return v;
        return null;
    }

    private static JsonElement? FindCalibration(JsonElement root)
    {
        foreach (var (p, d) in Walk(root, ""))
            if (p.ToLowerInvariant().EndsWith("/calibration") &&
                (Has(d, "empirical_coverage") || Has(d, "calibrated_coverage") || Has(d, "observed_coverage")))
                return d;
        return null;
    }

    private static bool Has(JsonElement d, string name) =>
        d.ValueKind == JsonValueKind.Object && d.TryGetProperty(name, out _);

    private static bool TryGetNum(JsonElement d, string name, out double value)
    {
        value = 0;
        return d.ValueKind == JsonValueKind.Object &&
               d.TryGetProperty(name, out var p) &&
               p.ValueKind == JsonValueKind.Number &&
               p.TryGetDouble(out value);
    }

    private static bool TryGetInt(JsonElement d, string name, out int value)
    {
        value = 0;
        return d.ValueKind == JsonValueKind.Object &&
               d.TryGetProperty(name, out var p) &&
               p.ValueKind == JsonValueKind.Number &&
               p.TryGetInt32(out value);
    }
}

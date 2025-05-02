namespace AiHedgeFund.Contracts.Model;

internal static class FinancialCalculations
{
    public static decimal? ComputeRevenueGrowth(IEnumerable<FinancialLineItem> items)
    {
        var revenues = items
            .OrderBy(x => x.ReportPeriod)
            .Where(x => x.Extras.TryGetValue("TotalRevenue", out var val) && val is decimal)
            .Select(x => (decimal)x.Extras["TotalRevenue"])
            .ToList();

        if (revenues.Count < 2)
            return null;

        var initial = revenues.First();
        var final = revenues.Last();

        return initial != 0 ? (final - initial) / Math.Abs(initial) : null;
    }

    public static decimal? ComputeTtmRevenue(IEnumerable<FinancialLineItem> items)
    {
        var revenues = items
            .OrderByDescending(x => x.ReportPeriod)
            .Where(x => x.Extras.TryGetValue("TotalRevenue", out var val) && val is decimal)
            .Take(4)
            .Select(x => (decimal)x.Extras["TotalRevenue"])
            .ToList();

        return revenues.Count == 4 ? revenues.Sum() : null;
    }

    public static decimal? ComputeEpsGrowth(IEnumerable<FinancialMetrics> metrics)
    {
        var epsValues = metrics
            .OrderBy(m => m.EndDate)
            .Where(m => m.EarningsPerShare.HasValue)
            .Select(m => m.EarningsPerShare.Value)
            .ToList();

        if (epsValues.Count < 2)
            return null;

        var initial = epsValues.First();
        var final = epsValues.Last();

        return initial != 0 ? (final - initial) / Math.Abs(initial) : null;
    }

    public static decimal? ComputeTtmEps(IEnumerable<FinancialMetrics> metrics)
    {
        return metrics
            .OrderByDescending(x => x.EndDate)
            .Where(x => x.EarningsPerShare.HasValue)
            .Take(4)
            .Select(x => x.EarningsPerShare.Value)
            .ToList() is var epsValues && epsValues.Count == 4
            ? epsValues.Sum()
            : (decimal?)null;
    }

    public static decimal? ComputeBookValuePerShare(IEnumerable<FinancialMetrics> metrics)
    {
        var latest = metrics.OrderByDescending(m => m.EndDate).FirstOrDefault();
        if (latest?.TotalShareholderEquity is null || latest.CommonStockSharesOutstanding is null)
            return null;

        var equity = latest.TotalShareholderEquity.Value;
        var shares = latest.CommonStockSharesOutstanding.Value;

        return shares > 0 ? equity / shares : null;
    }
}
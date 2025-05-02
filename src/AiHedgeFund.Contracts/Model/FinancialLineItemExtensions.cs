namespace AiHedgeFund.Contracts.Model;

public static class FinancialLineItemExtensions
{
    public static decimal? RevenueGrowth(this IEnumerable<FinancialLineItem> items) =>
        FinancialCalculations.ComputeRevenueGrowth(items);

    public static decimal? TtmRevenue(this IEnumerable<FinancialLineItem> items) =>
        FinancialCalculations.ComputeTtmRevenue(items);

    public static decimal? ComputeEpsGrowth(this IEnumerable<FinancialMetrics> metrics) =>
        FinancialCalculations.ComputeEpsGrowth(metrics);

    public static decimal? ComputeTtmEps(this IEnumerable<FinancialMetrics> metrics) =>
        FinancialCalculations.ComputeTtmEps(metrics);

    public static decimal? ComputeBookValuePerShare(this IEnumerable<FinancialMetrics> metrics) =>
        FinancialCalculations.ComputeBookValuePerShare(metrics);
}
namespace AiHedgeFund.Data.AlphaVantage;

public static class IncomeStatementMapper
{
    public static IncomeStatement Map(IncomeStatementRaw raw)
    {
        return new IncomeStatement
        {
            Symbol = raw.Symbol,
            AnnualReports = raw.AnnualReports
                .Select(MapReport)
                .ToList(),
            QuarterlyReports = raw.QuarterlyReports
                .Select(MapReport)
                .ToList()
        };
    }

    private static IncomeStatementReport MapReport(Dictionary<string, string> rawReport)
    {
        var report = new IncomeStatementReport();
        var extras = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in rawReport)
        {
            // Normalize keys to match class properties
            switch (key.ToLowerInvariant())
            {
                case "totalrevenue":
                    var totalRevenue = ParseDecimal(rawReport["totalRevenue"]);
                    var costOfRevenue = ParseDecimal(rawReport["costOfRevenue"]);
                    if (totalRevenue.HasValue && costOfRevenue.HasValue)
                    {
                        report.GrossMargin = (totalRevenue.Value - costOfRevenue.Value) / totalRevenue.Value;
                    }
                    break;
                case "fiscaldateending":
                    report.FiscalDateEnding = DateTime.Parse(value);
                    break;
                case "reportedcurrency":
                    report.ReportedCurrency = value;
                    break;
                case "netincome":
                    report.NetIncome = ParseDecimal(value);
                    break;
                case "eps":
                    report.EarningsPerShare = ParseDecimal(value);
                    extras["EarningsPerShare"] = report.EarningsPerShare;
                    break;
                default:
                    if (decimal.TryParse(value, out var numeric))
                        extras[key] = numeric;
                    break;
            }
        }

        // Always assign extras
        report.Extras = extras;
        return report;
    }

    private static decimal? ParseDecimal(string value)
    {
        return decimal.TryParse(value, out var result) ? result : null;
    }
}
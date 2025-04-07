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
            var normalizedKey = key.ToLowerInvariant();
            var parsedValue = ParseDecimal(value);

            switch (normalizedKey)
            {
                case "fiscaldateending":
                    report.FiscalDateEnding = DateTime.Parse(value);
                    break;

                case "reportedcurrency":
                    report.ReportedCurrency = value;
                    break;

                case "netincome":
                    report.NetIncome = parsedValue;
                    break;

                case "eps":
                case "earningspershare":
                    report.EarningsPerShare = parsedValue;
                    break;

                case "totalrevenue":
                    report.TotalRevenue = parsedValue;
                    break;

                case "grossprofit":
                    report.GrossProfit = parsedValue;
                    break;

                case "operatingincome":
                    report.OperatingIncome = parsedValue;
                    break;

                case "researchanddevelopment":
                    report.ResearchAndDevelopment = parsedValue;
                    break;

                case "sellinggeneralandadministrative":
                    report.SellingGeneralAndAdministrative = parsedValue;
                    break;

                case "interestexpense":
                    report.InterestExpense = parsedValue;
                    break;

                case "incomebeforetax":
                    report.IncomeBeforeTax = parsedValue;
                    break;

                case "costofrevenue":
                    extras["costOfRevenue"] = parsedValue; // needed to compute gross margin
                    break;

                case "incometaxexpense":
                    report.IncomeTaxExpense = parsedValue;
                    break;

                default:
                    if (parsedValue.HasValue)
                        extras[key] = parsedValue;
                    break;
            }
        }

        // Compute GrossMargin if not already computed
        if (report.TotalRevenue.HasValue && extras.TryGetValue("costOfRevenue", out var costOfRevenue) && costOfRevenue.HasValue)
        {
            report.GrossMargin = (report.TotalRevenue.Value - costOfRevenue.Value) / report.TotalRevenue.Value;
        }

        report.Extras = extras;
        return report;
    }

    private static decimal? ParseDecimal(string value)
    {
        return decimal.TryParse(value, out var result) ? result : null;
    }
}
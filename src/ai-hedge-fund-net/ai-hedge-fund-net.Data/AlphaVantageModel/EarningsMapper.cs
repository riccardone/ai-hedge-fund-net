namespace ai_hedge_fund_net.Data.AlphaVantageModel;

public static class EarningsMapper
{
    public static Earnings Map(EarningsRaw raw)
    {
        return new Earnings
        {
            Symbol = raw.Symbol,
            AnnualEarnings = raw.AnnualEarnings?.Select(MapAnnualReport).ToList(),
            QuarterlyEarnings = raw.QuarterlyEarnings?.Select(MapQuarterlyReport).ToList()
        };
    }

    private static EarningsReport MapAnnualReport(Dictionary<string, string> raw)
    {
        return new EarningsReport
        {
            FiscalDateEnding = ParseDate(raw.GetValueOrDefault("fiscalDateEnding")),
            ReportedEPS = ParseNullableDecimal(raw.GetValueOrDefault("reportedEPS"))
        };
    }

    private static EarningsReport MapQuarterlyReport(Dictionary<string, string> raw)
    {
        return new EarningsReport
        {
            FiscalDateEnding = ParseDate(raw.GetValueOrDefault("fiscalDateEnding")),
            ReportedDate = ParseNullableDate(raw.GetValueOrDefault("reportedDate")),
            ReportedEPS = ParseNullableDecimal(raw.GetValueOrDefault("reportedEPS")),
            EstimatedEPS = ParseNullableDecimal(raw.GetValueOrDefault("estimatedEPS")),
            Surprise = ParseNullableDecimal(raw.GetValueOrDefault("surprise")),
            SurprisePercentage = ParseNullableDecimal(raw.GetValueOrDefault("surprisePercentage"))
        };
    }

    private static decimal? ParseNullableDecimal(string value)
    {
        return decimal.TryParse(value, out var result) ? result : (decimal?)null;
    }

    private static DateTime ParseDate(string value)
    {
        return DateTime.TryParse(value, out var date) ? date : default;
    }

    private static DateTime? ParseNullableDate(string value)
    {
        return DateTime.TryParse(value, out var date) ? (DateTime?)date : null;
    }
}
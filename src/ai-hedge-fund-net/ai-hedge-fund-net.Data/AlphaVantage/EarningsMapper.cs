namespace ai_hedge_fund_net.Data.AlphaVantage;

public static class EarningsMapper
{
    public static Earnings Map(EarningsRaw raw)
    {
        return new Earnings
        {
            Symbol = raw.Symbol,
            AnnualEarnings = raw.AnnualEarnings?.Select(MapAnnual).ToList() ?? new(),
            QuarterlyEarnings = raw.QuarterlyEarnings?.Select(MapQuarterly).ToList() ?? new()
        };
    }

    private static AnnualEarning MapAnnual(AnnualEarningRaw raw)
    {
        return new AnnualEarning
        {
            FiscalDateEnding = DateTime.TryParse(raw.FiscalDateEnding, out var date) ? date : default,
            ReportedEPS = ParseDecimal(raw.ReportedEPS)
        };
    }

    private static QuarterlyEarning MapQuarterly(QuarterlyEarningRaw raw)
    {
        return new QuarterlyEarning
        {
            FiscalDateEnding = DateTime.TryParse(raw.FiscalDateEnding, out var fde) ? fde : default,
            ReportedDate = DateTime.TryParse(raw.ReportedDate, out var rd) ? rd : default,
            ReportedEPS = ParseDecimal(raw.ReportedEPS),
            EstimatedEPS = ParseDecimal(raw.EstimatedEPS),
            Surprise = ParseDecimal(raw.Surprise),
            SurprisePercentage = ParseDecimal(raw.SurprisePercentage)
        };
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.TryParse(value, out var result) ? result : 0;
    }
}
using System.Globalization;

namespace AiHedgeFund.Data.AlphaVantage;

public static class BalanceSheetMapper
{
    public static BalanceSheet Map(BalanceSheetRaw raw)
    {
        return new BalanceSheet
        {
            Symbol = raw.Symbol,
            AnnualReports = raw.AnnualReports?.Select(MapReportFromRaw).ToList() ?? new List<BalanceSheetReport>(),
            QuarterlyReports = raw.QuarterlyReports?.Select(MapReportFromRaw).ToList() ?? new List<BalanceSheetReport>()
        };
    }

    private static BalanceSheetReport MapReportFromRaw(BalanceSheetReportRaw raw)
    {
        return new BalanceSheetReport
        {
            FiscalDateEnding = ParseDate(raw.FiscalDateEnding),
            TotalAssets = ParseLong(raw.TotalAssets),
            TotalCurrentAssets = ParseLong(raw.TotalCurrentAssets),
            TotalLiabilities = ParseLong(raw.TotalLiabilities),
            TotalCurrentLiabilities = ParseLong(raw.TotalCurrentLiabilities),
            TotalShareholderEquity = ParseLong(raw.TotalShareholderEquity),
            CommonStockSharesOutstanding = ParseLong(raw.CommonStockSharesOutstanding),
            ShortTermDebt = ParseLong(raw.ShortTermDebt),
            LongTermDebt = ParseLong(raw.LongTermDebt),
            Goodwill = ParseLong(raw.Goodwill),
            IntangibleAssets = ParseLong(raw.IntangibleAssets),
            CashAndCashEquivalentsAtCarryingValue = ParseLong(raw.CashAndCashEquivalentsAtCarryingValue)
        };
    }

    private static DateTime ParseDate(string? input)
    {
        return DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result)
            ? result
            : default;
    }

    private static long? ParseLong(string? input)
    {
        return long.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }
}
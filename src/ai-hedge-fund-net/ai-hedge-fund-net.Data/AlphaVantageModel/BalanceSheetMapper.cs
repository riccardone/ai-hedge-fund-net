using System.Globalization;

namespace ai_hedge_fund_net.Data.AlphaVantageModel;

public static class BalanceSheetMapper
{
    public static BalanceSheet Map(BalanceSheetRaw raw)
    {
        //return new BalanceSheet
        //{
        //    Symbol = raw.Symbol,
        //    AnnualReports = raw.AnnualReports?.Select(MapReportFromRaw).ToList() ?? new List<BalanceSheetReport>(),
        //    QuarterlyReports = raw.QuarterlyReports?.Select(MapReportFromRaw).ToList() ?? new List<BalanceSheetReport>()
        //};
        var results = new BalanceSheet
        {
            Symbol = raw.Symbol
        };
        results.QuarterlyReports = new List<BalanceSheetReport>();
        foreach (var balanceSheetReportRaw in raw.QuarterlyReports)
        {
            var ciccio = MapReportFromRaw(balanceSheetReportRaw);
            if (ciccio == null)
            {
                
            }
            results.QuarterlyReports.Add(ciccio);
        }
        results.AnnualReports = new List<BalanceSheetReport>();
        foreach (var balanceSheetReportRaw in raw.AnnualReports)
        {
            var ciccio = MapReportFromRaw(balanceSheetReportRaw);
            if (ciccio == null)
            {

            }
            results.AnnualReports.Add(ciccio);
        }
        return results;
    }

    private static BalanceSheetReport MapReportFromRaw(BalanceSheetReportRaw raw)
    {
        return new BalanceSheetReport
        {
            FiscalDateEnding = ParseDate(raw.FiscalDateEnding),
            ReportedCurrency = raw.ReportedCurrency,
            TotalAssets = ParseLong(raw.TotalAssets),
            TotalCurrentAssets = ParseLong(raw.TotalCurrentAssets),
            CashAndCashEquivalentsAtCarryingValue = ParseLong(raw.CashAndCashEquivalentsAtCarryingValue),
            CashAndShortTermInvestments = ParseLong(raw.CashAndShortTermInvestments),
            Inventory = ParseLong(raw.Inventory),
            CurrentNetReceivables = ParseLong(raw.CurrentNetReceivables),
            TotalNonCurrentAssets = ParseLong(raw.TotalNonCurrentAssets),
            PropertyPlantEquipment = ParseLong(raw.PropertyPlantEquipment),
            AccumulatedDepreciationAmortizationPPE = ParseLong(raw.AccumulatedDepreciationAmortizationPPE),
            IntangibleAssets = ParseLong(raw.IntangibleAssets),
            IntangibleAssetsExcludingGoodwill = ParseLong(raw.IntangibleAssetsExcludingGoodwill),
            Goodwill = ParseLong(raw.Goodwill),
            Investments = ParseLong(raw.Investments),
            LongTermInvestments = ParseLong(raw.LongTermInvestments),
            ShortTermInvestments = ParseLong(raw.ShortTermInvestments),
            OtherCurrentAssets = ParseLong(raw.OtherCurrentAssets),
            OtherNonCurrentAssets = ParseLong(raw.OtherNonCurrentAssets),
            TotalLiabilities = ParseLong(raw.TotalLiabilities),
            TotalCurrentLiabilities = ParseLong(raw.TotalCurrentLiabilities),
            CurrentAccountsPayable = ParseLong(raw.CurrentAccountsPayable),
            DeferredRevenue = ParseLong(raw.DeferredRevenue),
            CurrentDebt = ParseLong(raw.CurrentDebt),
            ShortTermDebt = ParseLong(raw.ShortTermDebt),
            TotalNonCurrentLiabilities = ParseLong(raw.TotalNonCurrentLiabilities),
            CapitalLeaseObligations = ParseLong(raw.CapitalLeaseObligations),
            LongTermDebt = ParseLong(raw.LongTermDebt),
            CurrentLongTermDebt = ParseLong(raw.CurrentLongTermDebt),
            LongTermDebtNoncurrent = ParseLong(raw.LongTermDebtNoncurrent),
            ShortLongTermDebtTotal = ParseLong(raw.ShortLongTermDebtTotal),
            OtherCurrentLiabilities = ParseLong(raw.OtherCurrentLiabilities),
            OtherNonCurrentLiabilities = ParseLong(raw.OtherNonCurrentLiabilities),
            TotalShareholderEquity = ParseLong(raw.TotalShareholderEquity),
            TreasuryStock = ParseLong(raw.TreasuryStock),
            RetainedEarnings = ParseLong(raw.RetainedEarnings),
            CommonStock = ParseLong(raw.CommonStock),
            CommonStockSharesOutstanding = ParseLong(raw.CommonStockSharesOutstanding)
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
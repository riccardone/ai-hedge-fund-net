using System.Globalization;

namespace ai_hedge_fund_net.Data.AlphaVantageModel;

public static class CompanyOverviewMapper
{
    public static CompanyOverview Map(CompanyOverviewRaw raw)
    {
        return new CompanyOverview
        {
            Symbol = raw.Symbol,
            AssetType = raw.AssetType,
            Name = raw.Name,
            Description = raw.Description,
            CIK = raw.CIK,
            Exchange = raw.Exchange,
            Currency = raw.Currency,
            Country = raw.Country,
            Sector = raw.Sector,
            Industry = raw.Industry,
            Address = raw.Address,
            FiscalYearEnd = raw.FiscalYearEnd,
            LatestQuarter = ParseDate(raw.LatestQuarter),
            MarketCapitalization = ParseLong(raw.MarketCapitalization),
            EBITDA = ParseLong(raw.EBITDA),
            PERatio = ParseDecimal(raw.PERatio),
            PEGRatio = ParseDecimal(raw.PEGRatio),
            BookValue = ParseDecimal(raw.BookValue),
            DividendPerShare = ParseDecimal(raw.DividendPerShare),
            DividendYield = ParseDecimal(raw.DividendYield),
            EPS = ParseDecimal(raw.EPS),
            RevenuePerShareTTM = ParseDecimal(raw.RevenuePerShareTTM),
            ProfitMargin = ParseDecimal(raw.ProfitMargin),
            OperatingMarginTTM = ParseDecimal(raw.OperatingMarginTTM),
            ReturnOnAssetsTTM = ParseDecimal(raw.ReturnOnAssetsTTM),
            ReturnOnEquityTTM = ParseDecimal(raw.ReturnOnEquityTTM),
            RevenueTTM = ParseLong(raw.RevenueTTM),
            GrossProfitTTM = ParseLong(raw.GrossProfitTTM),
            DilutedEPSTTM = ParseDecimal(raw.DilutedEPSTTM),
            QuarterlyEarningsGrowthYOY = ParseDecimal(raw.QuarterlyEarningsGrowthYOY),
            QuarterlyRevenueGrowthYOY = ParseDecimal(raw.QuarterlyRevenueGrowthYOY),
            AnalystTargetPrice = ParseDecimal(raw.AnalystTargetPrice),
            TrailingPE = ParseDecimal(raw.TrailingPE),
            ForwardPE = ParseDecimal(raw.ForwardPE),
            PriceToSalesRatioTTM = ParseDecimal(raw.PriceToSalesRatioTTM),
            PriceToBookRatio = ParseDecimal(raw.PriceToBookRatio),
            EVToRevenue = ParseDecimal(raw.EVToRevenue),
            EVToEBITDA = ParseDecimal(raw.EVToEBITDA),
            Beta = ParseDecimal(raw.Beta),
            _52WeekHigh = ParseDecimal(raw._52WeekHigh),
            _52WeekLow = ParseDecimal(raw._52WeekLow),
            _50DayMovingAverage = ParseDecimal(raw._50DayMovingAverage),
            _200DayMovingAverage = ParseDecimal(raw._200DayMovingAverage),
            SharesOutstanding = ParseLong(raw.SharesOutstanding),
            DividendDate = ParseDate(raw.DividendDate),
            ExDividendDate = ParseDate(raw.ExDividendDate),
        };
    }

    private static decimal? ParseDecimal(string? input)
    {
        return decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static long? ParseLong(string? input)
    {
        return long.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static DateTime? ParseDate(string? input)
    {
        return DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result)
            ? result
            : null;
    }
}
﻿using System.Data;
using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using NLog;
using IDataReader = AiHedgeFund.Contracts.IDataReader;

namespace AiHedgeFund.Data.AlphaVantage;

public class AlphaVantageDataReader : IDataReader
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly DataFetcher _dataFetcher;
    private readonly IPriceVolumeProvider _priceVolumeProvider;

    public AlphaVantageDataReader(IHttpClientFactory clientFactory, IPriceVolumeProvider priceVolumeProvider)
    {
        _dataFetcher = new DataFetcher(clientFactory);
        _priceVolumeProvider = priceVolumeProvider;
    }

    public bool TryGetPrices(string ticker, DateTime startDate, DateTime endDate, out IEnumerable<Price>? prices)
    {
        var key = $"daily_{ticker}";
        var query = $"query?function=TIME_SERIES_DAILY&symbol={ticker}&outputsize=compact";

        if (_dataFetcher.TryLoadOrFetch<TimeSeriesDailyResponse, List<Price>>(key, query, raw =>
            {
                var result = new List<Price>();

                foreach (var kvp in raw.TimeSeries)
                {
                    if (!DateTime.TryParse(kvp.Key, out var date)) continue;
                    if (date < startDate || date > endDate) continue;

                    var data = kvp.Value;

                    result.Add(new Price
                    {
                        Date = date,
                        Open = decimal.Parse(data["1. open"]),
                        High = decimal.Parse(data["2. high"]),
                        Low = decimal.Parse(data["3. low"]),
                        Close = decimal.Parse(data["4. close"]),
                        Volume = data.TryGetValue("6. volume", out var volumeRaw) && decimal.TryParse(volumeRaw, out var volume)
                            ? volume
                            : _priceVolumeProvider.GetVolume(ticker, date) ?? 0
                    });
                }

                return result.OrderBy(p => p.Date).ToList();
            }, out var results))
        {
            prices = results ?? Enumerable.Empty<Price>();
            return true;
        }

        prices = default;
        return false;
    }

    public bool TryGetFinancialMetrics(string ticker, DateTime endDate, string period, int limit, out IEnumerable<FinancialMetrics>? metrics)
    {
        var internalMetrics = new List<FinancialMetrics>();

        try
        {
            if (!_dataFetcher.TryLoadOrFetch<CompanyOverviewRaw, CompanyOverview>($"OVERVIEW:{ticker}",
                    $"query?function=OVERVIEW&symbol={ticker}", CompanyOverviewMapper.Map, out var overviewData))
            {
                Logger.Error($"I can't retrieve {nameof(overviewData)}");
                metrics = default;
                return false;
            }

            if (!_dataFetcher.TryLoadOrFetch<BalanceSheetRaw, BalanceSheet>($"BALANCE_SHEET:{ticker}",
                    $"query?function=BALANCE_SHEET&symbol={ticker}", BalanceSheetMapper.Map, out var balanceSheetData))
            {
                Logger.Error($"I can't retrieve {nameof(balanceSheetData)}");
                metrics = default;
                return false;
            }

            if (!_dataFetcher.TryLoadOrFetch<IncomeStatementRaw, IncomeStatement>($"INCOME_STATEMENT:{ticker}",
                    $"query?function=INCOME_STATEMENT&symbol={ticker}", IncomeStatementMapper.Map,
                    out var incomeStatementData))
            {
                Logger.Error($"I can't retrieve {nameof(incomeStatementData)}");
                metrics = default;
                return false;
            }

            if (!_dataFetcher.TryLoadOrFetch<CashFlowRaw, CashFlow>($"CASH_FLOW:{ticker}",
                    $"query?function=CASH_FLOW&symbol={ticker}", CashFlowMapper.Map, out var cashFlowData))
            {
                Logger.Error($"I can't retrieve {nameof(cashFlowData)}");
                metrics = default;
                return false;
            }

            if (!_dataFetcher.TryLoadOrFetch<EarningsRaw, Earnings>($"EARNINGS:{ticker}",
                    $"query?function=EARNINGS&symbol={ticker}", EarningsMapper.Map, out var earningsData))
            {
                Logger.Error($"I can't retrieve {nameof(earningsData)}");
                metrics = default;
                return false;
            }
            
            var balanceReports = GetFilteredReports(balanceSheetData.QuarterlyReports, endDate, limit, r => r.FiscalDateEnding);
            var incomeReports = GetFilteredReports(incomeStatementData.QuarterlyReports, endDate, 2, r => r.FiscalDateEnding);
            var cashFlowReports = GetFilteredReports(cashFlowData.QuarterlyReports, endDate, limit, r => r.FiscalDateEnding);
            var earningsReports = GetFilteredReports(earningsData.QuarterlyEarnings, endDate, limit, r => r.FiscalDateEnding);

            for (int i = 0; i < balanceReports.Count; i++)
            {
                var tmpMetrics = new FinancialMetrics { Ticker = ticker };

                if (overviewData != null)
                {
                    tmpMetrics.MarketCap = overviewData.MarketCapitalization; // TODO Parse overview if available
                    tmpMetrics.PriceToEarningsRatio = overviewData.PERatio;
                    tmpMetrics.PriceToBookRatio = overviewData.PriceToBookRatio;
                    tmpMetrics.ReturnOnEquity = overviewData.ReturnOnEquityTTM;
                    tmpMetrics.ReturnOnAssets = overviewData.ReturnOnAssetsTTM;
                    tmpMetrics.EarningsPerShare = overviewData.EPS;
                    tmpMetrics.RevenueGrowth = overviewData.QuarterlyRevenueGrowthYOY;
                }

                if (i < balanceReports.Count)
                {
                    var bs = balanceReports[i];
                    tmpMetrics.DebtToEquity = bs.TotalLiabilities / bs.TotalShareholderEquity;
                    tmpMetrics.DebtToAssets = bs.TotalLiabilities / bs.TotalAssets;
                    tmpMetrics.BookValuePerShare = bs.TotalShareholderEquity / bs.CommonStockSharesOutstanding;
                    tmpMetrics.CommonStockSharesOutstanding = bs.CommonStockSharesOutstanding;
                }

                if (i < incomeReports.Count)
                {
                    var inc = incomeReports[i];
                    tmpMetrics.NetMargin = inc.NetIncome / inc.TotalRevenue;
                    tmpMetrics.OperatingIncomeGrowth = inc.OperatingIncome / inc.TotalRevenue;
                    tmpMetrics.GrossMargin = incomeReports[i].GrossMargin;
                    tmpMetrics.TotalRevenue = inc.TotalRevenue;
                    tmpMetrics.OperatingMargin = inc.OperatingMargin;
                }

                if (i < cashFlowReports.Count)
                {
                    var cf = cashFlowReports[i];
                    var sharesOutstanding = balanceReports[i].CommonStockSharesOutstanding;
                    tmpMetrics.FreeCashFlowPerShare = cf.OperatingCashflow / sharesOutstanding;
                    if(!TryCalculatePayoutRatio(cf, i < incomeReports.Count ? incomeReports[i] : null, out decimal payoutRatio))
                        Logger.Warn($"Can't calculate payoutRatio for {ticker}");
                    tmpMetrics.PayoutRatio = payoutRatio;
                    tmpMetrics.OperatingCashFlow = cf.OperatingCashflow;
                    tmpMetrics.CapitalExpenditure = cf.CapitalExpenditures;
                    tmpMetrics.DividendsAndOtherCashDistributions =
                        cf.DividendPayoutCommonStock + cf.DividendPayoutPreferredStock;
                }

                if (i < earningsReports.Count)
                {
                    var er = earningsReports[i];
                    tmpMetrics.EarningsPerShareGrowth = er.ReportedEPS;
                }

                internalMetrics.Add(tmpMetrics);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{nameof(AlphaVantageDataReader)}/{nameof(TryGetFinancialMetrics)} for {ticker}: {ex.GetBaseException().Message}");
            metrics = internalMetrics;
            return false;
        }

        metrics = internalMetrics;
        return true;
    }

    private static bool TryCalculatePayoutRatio(CashFlowReport? cashFlow, IncomeStatementReport? incomeStatement, out decimal payoutRatio)
    {
        payoutRatio = 0;

        if (cashFlow?.DividendPayout is null)
            return false;

        // Prefer NetIncome from income statement if available
        if (incomeStatement?.NetIncome is decimal netIncome && netIncome != 0)
        {
            payoutRatio = cashFlow.DividendPayout.Value / netIncome;
            return true;
        }

        // Fallback: use NetIncome from cash flow, if available
        if (cashFlow.NetIncome is decimal fallbackNetIncome && fallbackNetIncome != 0)
        {
            payoutRatio = cashFlow.DividendPayout.Value / fallbackNetIncome;
            return true;
        }

        return false;
    }

    public bool TryGetFinancialLineItems(string ticker, DateTime endDate, string period, int limit, out IEnumerable<FinancialLineItem>? financialLineItems)
    {
        if (!TrySearchLineItems(ticker, endDate, period, limit, out var internalResults))
        {
            financialLineItems = default;
            return false;
        }

        financialLineItems = internalResults;
        return true;
    }

    private List<T> GetFilteredReports<T>(
        List<T> reports,
        DateTime endDate,
        int limit,
        Func<T, DateTime> dateSelector)
    {
        return reports
            .Where(r => dateSelector(r) <= endDate)
            .OrderByDescending(dateSelector)
            .Take(limit)
            .ToList();
    }

    private bool TrySearchLineItems(string ticker, DateTime endDate, string? period, int? limit, out IEnumerable<FinancialLineItem>? results)
    {
        period ??= "ttm";
        limit ??= 10;

        if (!_dataFetcher.TryLoadOrFetch<BalanceSheetRaw, BalanceSheet>($"BALANCE_SHEET:{ticker}",
                $"query?function=BALANCE_SHEET&symbol={ticker}", BalanceSheetMapper.Map, out var balanceSheetData))
        {
            Logger.Error($"I can't retrieve {nameof(balanceSheetData)}");
            results = default;
            return false;
        }

        if (!_dataFetcher.TryLoadOrFetch<IncomeStatementRaw, IncomeStatement>($"INCOME_STATEMENT:{ticker}",
                $"query?function=INCOME_STATEMENT&symbol={ticker}", IncomeStatementMapper.Map,
                out var incomeStatementData))
        {
            Logger.Error($"I can't retrieve {nameof(incomeStatementData)}");
            results = default;
            return false;
        }

        if (!_dataFetcher.TryLoadOrFetch<CashFlowRaw, CashFlow>($"CASH_FLOW:{ticker}",
                $"query?function=CASH_FLOW&symbol={ticker}", CashFlowMapper.Map, out var cashFlowData))
        {
            Logger.Error($"I can't retrieve {nameof(cashFlowData)}");
            results = default;
            return false;
        }

        var balanceReports = GetFilteredReports(balanceSheetData.QuarterlyReports, endDate, limit.Value, r => r.FiscalDateEnding);
        var incomeReports = GetFilteredReports(incomeStatementData.QuarterlyReports, endDate, 2, r => r.FiscalDateEnding);
        var cashFlowReports = GetFilteredReports(cashFlowData.QuarterlyReports, endDate, limit.Value, r => r.FiscalDateEnding);

        var allReports = MergeReportsByDate(balanceReports, incomeReports, cashFlowReports);

        var internalResults = new List<FinancialLineItem>();
        foreach (var (fiscalDate, reportData) in allReports)
        {
            var extras = new Dictionary<string, dynamic>(StringComparer.OrdinalIgnoreCase);

            var keysToUse = reportData.Keys.ToArray(); // all keys in the merged report

            foreach (var item in keysToUse)
            {
                if (reportData.TryGetValue(item, out var value))
                    extras[item] = value;
                else
                    extras[item] = null;
            }

            internalResults.Add(new FinancialLineItem(
                ticker: ticker,
                reportPeriod: fiscalDate.ToString("yyyy-MM-dd"),
                period: period,
                currency: "USD",
                extras: extras
            ));
        }

        results = internalResults;
        return true;
    }

    private static Dictionary<DateTime, Dictionary<string, decimal?>> MergeReportsByDate(
        IEnumerable<BalanceSheetReport> balanceReports, IEnumerable<IncomeStatementReport> incomeReports,
        IEnumerable<CashFlowReport> cashFlowReports)
    {
        var merged = new Dictionary<DateTime, Dictionary<string, decimal?>>();

        void Merge<T>(IEnumerable<T> reports, Func<T, DateTime> getDate, Func<T, Dictionary<string, decimal?>> getItems)
        {
            foreach (var report in reports)
            {
                var date = getDate(report);

                if (!merged.TryGetValue(date, out var combined))
                {
                    combined = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
                    merged[date] = combined;
                }

                foreach (var kvp in getItems(report))
                {
                    combined[kvp.Key] = kvp.Value;
                }
            }
        }

        Merge(balanceReports, r => r.FiscalDateEnding, r => r.GetLineItems());
        Merge(incomeReports, r => r.FiscalDateEnding, r => r.GetLineItems());
        Merge(cashFlowReports, r => r.FiscalDateEnding, r => r.GetLineItems());

        return merged;
    }

    public DataTable PricesToDf(IEnumerable<Price> prices)
    {
        var table = new DataTable();

        // Define columns
        table.Columns.Add("Date", typeof(DateTime));
        table.Columns.Add("Open", typeof(decimal));
        table.Columns.Add("Close", typeof(decimal));
        table.Columns.Add("High", typeof(decimal));
        table.Columns.Add("Low", typeof(decimal));
        table.Columns.Add("Volume", typeof(decimal)); // changed from int to decimal

        foreach (var price in prices)
        {
            table.Rows.Add(
                price.Date,
                price.Open,
                price.Close,
                price.High,
                price.Low,
                price.Volume
            );
        }

        // Sort by Date (ascending)
        var sortedView = new DataView(table)
        {
            Sort = "Date ASC"
        };

        return sortedView.ToTable();
    }
}
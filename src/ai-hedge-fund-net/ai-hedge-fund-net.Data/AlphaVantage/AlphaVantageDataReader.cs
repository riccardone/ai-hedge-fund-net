using System.Data;
using System.Globalization;
using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;
using NLog;

namespace ai_hedge_fund_net.Data.AlphaVantage;

public class AlphaVantageDataReader : Contracts.IDataReader
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IDataFetcher _dataFetcher;
    private readonly IPriceVolumeProvider _priceVolumeProvider;

    public AlphaVantageDataReader(IDataFetcher dataFetcher, IPriceVolumeProvider priceVolumeProvider)
    {
        _dataFetcher = dataFetcher;
        _priceVolumeProvider = priceVolumeProvider;
    }

    public IEnumerable<Price> GetPrices(string ticker, DateTime startDate, DateTime endDate)
    {
        var key = $"daily_{ticker}";
        //var query = $"query?function=TIME_SERIES_DAILY_ADJUSTED&symbol={ticker}&outputsize=full";
        var query = $"query?function=TIME_SERIES_DAILY&symbol={ticker}&outputsize=compact";

        var prices = _dataFetcher.LoadOrFetch<TimeSeriesDailyResponse, List<Price>>(key, query, raw =>
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
        });

        return prices ?? Enumerable.Empty<Price>();
    }

    public bool TryGetFinancialMetricsAsync(string ticker, DateTime endDate, string period, int limit, out IList<FinancialMetrics> metrics)
    {
        metrics = new List<FinancialMetrics>();

        try
        {
            var overviewData = _dataFetcher.LoadOrFetch<CompanyOverviewRaw, CompanyOverview>($"OVERVIEW:{ticker}",
                $"query?function=OVERVIEW&symbol={ticker}", CompanyOverviewMapper.Map);

            var balanceSheetData = _dataFetcher.LoadOrFetch<BalanceSheetRaw, BalanceSheet>($"BALANCE_SHEET:{ticker}",
                $"query?function=BALANCE_SHEET&symbol={ticker}", BalanceSheetMapper.Map);

            var incomeStatementData = _dataFetcher.LoadOrFetch<IncomeStatementRaw, IncomeStatement>($"INCOME_STATEMENT:{ticker}",
                $"query?function=INCOME_STATEMENT&symbol={ticker}", IncomeStatementMapper.Map);

            var cashFlowData = _dataFetcher.LoadOrFetch<CashFlowRaw, CashFlow>($"CASH_FLOW:{ticker}",
                $"query?function=CASH_FLOW&symbol={ticker}", CashFlowMapper.Map);

            var earningsData = _dataFetcher.LoadOrFetch<EarningsRaw, Earnings>($"EARNINGS:{ticker}",
                $"query?function=EARNINGS&symbol={ticker}", EarningsMapper.Map);
            
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
                }

                if (i < incomeReports.Count)
                {
                    var inc = incomeReports[i];
                    tmpMetrics.NetMargin = inc.NetIncome / inc.TotalRevenue;
                    tmpMetrics.OperatingIncomeGrowth = inc.OperatingIncome / inc.TotalRevenue;
                }

                if (i < cashFlowReports.Count)
                {
                    var cf = cashFlowReports[i];
                    var sharesOutstanding = balanceReports[i].CommonStockSharesOutstanding;
                    tmpMetrics.FreeCashFlowPerShare = cf.OperatingCashflow / sharesOutstanding;
                    if(!TryCalculatePayoutRatio(cf, i < incomeReports.Count ? incomeReports[i] : null, out decimal payoutRatio))
                        Logger.Warn($"Can't calculate payoutRatio for {ticker}");
                    tmpMetrics.PayoutRatio = payoutRatio;
                }

                if (i < earningsReports.Count)
                {
                    var er = earningsReports[i];
                    tmpMetrics.EarningsPerShareGrowth = er.ReportedEPS;
                }

                metrics.Add(tmpMetrics);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{nameof(AlphaVantageDataReader)}/{nameof(TryGetFinancialMetricsAsync)} for {ticker}: {ex.Message}");
            return false;
        }

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

    public bool TryGetFinancialLineItemsAsync(string ticker, DateTime endDate, string period, int limit, out IList<FinancialLineItem> financialLineItems)
    {
        financialLineItems = new List<FinancialLineItem>();
        try
        {
            financialLineItems = SearchLineItemsAsync(ticker, endDate, period, limit).Result.ToList();
            return true;
        }
        catch
        {
            return false;
        }
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

    private async Task<IEnumerable<FinancialLineItem>> SearchLineItemsAsync(string ticker, DateTime endDate,string period = "ttm",int limit = 10)
    {
        var result = new List<FinancialLineItem>();

        var balanceSheetData = _dataFetcher.LoadOrFetch<BalanceSheetRaw, BalanceSheet>($"BALANCE_SHEET:{ticker}",
            $"query?function=BALANCE_SHEET&symbol={ticker}", BalanceSheetMapper.Map);

        var incomeStatementData = _dataFetcher.LoadOrFetch<IncomeStatementRaw, IncomeStatement>($"INCOME_STATEMENT:{ticker}",
            $"query?function=INCOME_STATEMENT&symbol={ticker}", IncomeStatementMapper.Map);

        var cashFlowData = _dataFetcher.LoadOrFetch<CashFlowRaw, CashFlow>($"CASH_FLOW:{ticker}",
            $"query?function=CASH_FLOW&symbol={ticker}", CashFlowMapper.Map);

        var balanceReports = GetFilteredReports(balanceSheetData.QuarterlyReports, endDate, limit, r => r.FiscalDateEnding);
        var incomeReports = GetFilteredReports(incomeStatementData.QuarterlyReports, endDate, 2, r => r.FiscalDateEnding);
        var cashFlowReports = GetFilteredReports(cashFlowData.QuarterlyReports, endDate, limit, r => r.FiscalDateEnding);

        var allReports = MergeReportsByDate(balanceReports, incomeReports, cashFlowReports);

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

            result.Add(new FinancialLineItem(
                ticker: ticker,
                reportPeriod: fiscalDate.ToString("yyyy-MM-dd"),
                period: period,
                currency: "USD",
                extras: extras
            ));
        }

        return result;
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

    public IEnumerable<InsiderTrade> GetInsiderTrades(string ticker, DateTime endDate, DateTime? startDate = null, int limit = 1000)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<CompanyNews> GetCompanyNews(string ticker, DateTime endDate, DateTime startDate, int limit = 1000)
    {
        throw new NotImplementedException();
    }

    public decimal? GetMarketCap(string ticker, DateTime endDate)
    {
        throw new NotImplementedException();
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

    public DataTable GetPriceData(string ticker, DateTime startDate, DateTime endDate)
    {
        var prices = GetPrices(ticker, startDate, endDate);
        return PricesToDf(prices);
    }
}
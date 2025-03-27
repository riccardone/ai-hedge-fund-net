using System.Data;
using System.Globalization;
using System.Text.Json;
using ai_hedge_fund_net.Contracts.Model;
using ai_hedge_fund_net.Data.AlphaVantageModel;
using NLog;

namespace ai_hedge_fund_net.Data;

public class AlphaVantageDataReader : Contracts.IDataReader
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly HttpClient _client;

    public AlphaVantageDataReader(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient("AlphaVantage");
    }

    private async Task<JsonElement?> FetchDataAsync(string endpoint)
    {
        var response = await _client.GetAsync(endpoint);
        if (response.IsSuccessStatusCode)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(jsonString);
            return doc.RootElement.Clone(); // Clone to avoid disposal issues
        }
        return null;
    }

    private bool TryFetchData<T>(string endpoint, out T result) where T : class 
    {
        //result = default;

        try
        {
            var response = _client.GetAsync(endpoint).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                Logger.Warn("API call to '{0}' failed with status code {1}", endpoint, response.StatusCode);
                result = null;
                return false;
            }

            var jsonString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            result = JsonSerializer.Deserialize<T>(jsonString, options);
            if (result != null) return true;
            Logger.Warn("Deserialization returned null for endpoint '{0}'", endpoint);
            return false;
        }
        catch (HttpRequestException ex)
        {
            Logger.Error(ex, "HTTP request failed for endpoint '{0}'", endpoint);
        }
        catch (JsonException ex)
        {
            Logger.Error(ex, "JSON deserialization failed for endpoint '{0}'", endpoint);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Unexpected error in TryFetchData for endpoint '{0}'", endpoint);
        }

        result = null;
        return false;
    }

    private double? ParseDouble(JsonElement data, string key)
    {
        if (data.TryGetProperty(key, out JsonElement value) && value.ValueKind == JsonValueKind.String)
        {
            if (double.TryParse(value.GetString(), out double result))
            {
                return result;
            }
        }
        return null;
    }

    public async Task<IEnumerable<Price>> GetPricesAsync(string ticker, DateTime startDate, DateTime endDate)
    {
        var prices = new List<Price>();

        try
        {
            var json = await FetchDataAsync($"query?function=TIME_SERIES_DAILY_ADJUSTED&symbol={ticker}&outputsize=full");

            if (!json.HasValue)
                return prices;

            if (!json.Value.TryGetProperty("Time Series (Daily)", out var timeSeries))
                return prices;

            foreach (var item in timeSeries.EnumerateObject())
            {
                if (!DateTime.TryParse(item.Name, out var date))
                    continue;

                if (date < startDate || date > endDate)
                    continue;

                var dailyData = item.Value;

                var price = new Price
                {
                    Time = date.ToString("yyyy-MM-dd"),
                    Open = ParseDecimal(dailyData, "1. open"),
                    High = ParseDecimal(dailyData, "2. high"),
                    Low = ParseDecimal(dailyData, "3. low"),
                    Close = ParseDecimal(dailyData, "4. close"),
                    Volume = ParseInt(dailyData, "6. volume")
                };

                prices.Add(price);
            }

            // Order ascending by date (optional)
            return prices.OrderBy(p => p.Time).ToList();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error fetching prices for {ticker}: {ex.Message}");
            return prices;
        }
    }

    private decimal ParseDecimal(JsonElement element, string key)
    {
        return element.TryGetProperty(key, out var prop) && decimal.TryParse(prop.GetString(), out var value)
            ? value
            : 0m;
    }

    private int ParseInt(JsonElement element, string key)
    {
        return element.TryGetProperty(key, out var prop) && int.TryParse(prop.GetString(), out var value)
            ? value
            : 0;
    }

    public bool TryGetFinancialMetricsAsync(string ticker, DateTime endDate, string period, int limit, out IList<FinancialMetrics> metrics)
    {
        metrics = new List<FinancialMetrics>();

        try
        {
            // Fetch CompanyOverview
            CompanyOverview? overviewData = default;
            if (TryFetchData<CompanyOverviewRaw>($"query?function=OVERVIEW&symbol={ticker}",
                    out var companyOverviewRaw))
                overviewData = CompanyOverviewMapper.Map(companyOverviewRaw);

            // Fetch BalanceSheet
            BalanceSheet? balanceSheetData = default;
            if (TryFetchData<BalanceSheetRaw>($"query?function=BALANCE_SHEET&symbol={ticker}",
                    out var balanceSheetRaw))
                balanceSheetData = BalanceSheetMapper.Map(balanceSheetRaw);

            // Fetch IncomeStatement
            IncomeStatement? incomeStatementData = default;
            if (TryFetchData<IncomeStatementRaw>($"query?function=BALANCE_SHEET&symbol={ticker}",
                    out var incomeStatementRaw))
                incomeStatementData = IncomeStatementMapper.Map(incomeStatementRaw);

            // Fetch IncomeStatement
            CashFlow? cashFlowData = default;
            if (TryFetchData<CashFlowRaw>($"query?function=BALANCE_SHEET&symbol={ticker}",
                    out var cashFlowRaw))
                cashFlowData = CashFlowMapper.Map(cashFlowRaw);

            // Fetch IncomeStatement
            Earnings? earningsData = default;
            if (TryFetchData<EarningsRaw>($"query?function=BALANCE_SHEET&symbol={ticker}",
                    out var earninFlowRaw))
                earningsData = EarningsMapper.Map(earninFlowRaw);

            var balanceReports = GetFilteredReports(balanceSheetData.QuarterlyReports, endDate, limit, r => r.FiscalDateEnding);
            var incomeReports = GetFilteredReports(incomeStatementData.AnnualReports, endDate, 2, r => r.FiscalDateEnding);
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
                    tmpMetrics.FreeCashFlowPerShare = cf.OperatingCashFlow / sharesOutstanding;
                    tmpMetrics.PayoutRatio = cf.DividendPayout / cf.NetIncome;
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

    public bool TryGetFinancialLineItemsAsync(string ticker, string[] lineItems, DateTime endDate, string period, int limit,
        out IList<FinancialLineItem> financialLineItems)
    {
        financialLineItems = new List<FinancialLineItem>();
        try
        {
            financialLineItems = SearchLineItemsAsync(ticker, lineItems, endDate, period, limit).Result.ToList();
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

    private List<JsonElement> GetFilteredReports(JsonElement? data, string key, DateTime endDate, int limit)
    {
        var reports = new List<JsonElement>();

        if (data.HasValue && data.Value.TryGetProperty(key, out JsonElement array))
        {
            reports = array.EnumerateArray()
                .Where(e => DateTime.TryParse(e.GetProperty("fiscalDateEnding").GetString(), out var date) && date <= endDate)
                .OrderByDescending(e => DateTime.Parse(e.GetProperty("fiscalDateEnding").GetString()))
                .Take(limit)
                .ToList();
        }

        return reports;
    }

    private async Task<IEnumerable<FinancialLineItem>> SearchLineItemsAsync(string ticker, string[] lineItems, DateTime endDate, string period = "ttm", int limit = 10)
    {
        var result = new List<FinancialLineItem>();

        // Load data from Alpha Vantage
        var balanceSheetData = await FetchDataAsync($"query?function=BALANCE_SHEET&symbol={ticker}");
        var incomeStatementData = await FetchDataAsync($"query?function=INCOME_STATEMENT&symbol={ticker}");
        var cashFlowData = await FetchDataAsync($"query?function=CASH_FLOW&symbol={ticker}");

        // Collect reports by period (e.g., annualReports or quarterlyReports)
        var balanceReports = GetFilteredReports(balanceSheetData, GetPeriodKey(period), endDate, limit);
        var incomeReports = GetFilteredReports(incomeStatementData, GetPeriodKey(period), endDate, limit);
        var cashFlowReports = GetFilteredReports(cashFlowData, GetPeriodKey(period), endDate, limit);

        // Merge reports by fiscal date
        var allReports = MergeReportsByDate(balanceReports, incomeReports, cashFlowReports);

        foreach (var (fiscalDate, reportData) in allReports)
        {
            var extras = new Dictionary<string, dynamic>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in lineItems)
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
                currency: "USD", // Alpha Vantage reports are usually in USD
                extras: extras
            ));
        }

        return result;
    }

    private static string GetPeriodKey(string period) => period.Equals("quarterly", StringComparison.OrdinalIgnoreCase) ? "quarterlyReports" : "annualReports";

    private Dictionary<DateTime, Dictionary<string, dynamic>> MergeReportsByDate(
        List<JsonElement> balance,
        List<JsonElement> income,
        List<JsonElement> cashflow)
    {
        var merged = new Dictionary<DateTime, Dictionary<string, dynamic>>();

        void AddToMerged(List<JsonElement> source)
        {
            foreach (var report in source)
            {
                if (!DateTime.TryParse(report.GetProperty("fiscalDateEnding").GetString(), out var date))
                    continue;

                if (!merged.TryGetValue(date, out var dict))
                {
                    dict = new Dictionary<string, dynamic>(StringComparer.OrdinalIgnoreCase);
                    merged[date] = dict;
                }

                foreach (var prop in report.EnumerateObject())
                {
                    if (!dict.ContainsKey(prop.Name))
                        dict[prop.Name] = ParseNullableDouble(prop.Value);
                }
            }
        }

        AddToMerged(balance);
        AddToMerged(income);
        AddToMerged(cashflow);

        return merged;
    }

    private double? ParseNullableDouble(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String && double.TryParse(element.GetString(), out var val))
            return val;
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var val2))
            return val2;
        return null;
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
        table.Columns.Add("Volume", typeof(int));

        foreach (var price in prices)
        {
            if (DateTime.TryParse(price.Time, null, DateTimeStyles.AdjustToUniversal, out DateTime parsedDate))
            {
                table.Rows.Add(parsedDate, price.Open, price.Close, price.High, price.Low, price.Volume);
            }
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
        var prices = GetPricesAsync(ticker, startDate, endDate).Result;
        return PricesToDf(prices);
    }
}
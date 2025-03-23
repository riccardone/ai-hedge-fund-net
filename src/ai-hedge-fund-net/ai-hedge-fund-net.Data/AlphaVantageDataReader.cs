using System.Data;
using System.Globalization;
using System.Text.Json;
using ai_hedge_fund_net.Contracts.Model;
using NLog;
using IDataReader = ai_hedge_fund_net.Contracts.IDataReader;

namespace ai_hedge_fund_net.Data;

public class AlphaVantageDataReader : IDataReader
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly HttpClient _client;
    private readonly string _apiKey;

    public AlphaVantageDataReader(HttpClient httpClient, string apiKey)
    {
        _client = httpClient;
        _apiKey = apiKey;
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
            var json = await FetchDataAsync($"query?function=TIME_SERIES_DAILY_ADJUSTED&symbol={ticker}&outputsize=full&apikey={_apiKey}");

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
            // Fetch Overview
            var overviewData = FetchDataAsync($"query?function=OVERVIEW&symbol={ticker}&apikey={_apiKey}").Result;

            // Fetch statements
            var balanceSheetData = FetchDataAsync($"query?function=BALANCE_SHEET&symbol={ticker}&apikey={_apiKey}").Result;
            var incomeStatementData = FetchDataAsync($"query?function=INCOME_STATEMENT&symbol={ticker}&apikey={_apiKey}").Result;
            var cashFlowData = FetchDataAsync($"query?function=CASH_FLOW&symbol={ticker}&apikey={_apiKey}").Result;
            var earningsData = FetchDataAsync($"query?function=EARNINGS&symbol={ticker}&apikey={_apiKey}").Result;

            var balanceReports = GetFilteredReports(balanceSheetData, period, endDate, limit);
            var incomeReports = GetFilteredReports(incomeStatementData, period, endDate, limit);
            var cashFlowReports = GetFilteredReports(cashFlowData, period, endDate, limit);
            var earningsReports = GetFilteredReports(earningsData, "annualEarnings", endDate, limit);

            for (int i = 0; i < balanceReports.Count; i++)
            {
                var tmpMetrics = new FinancialMetrics { Ticker = ticker };

                if (overviewData.HasValue)
                {
                    tmpMetrics.MarketCap = 1000; // TODO Parse overview if available
                    tmpMetrics.PriceToEarningsRatio = ParseDouble(overviewData.Value, "PERatio");
                    tmpMetrics.PriceToBookRatio = ParseDouble(overviewData.Value, "PriceToBookRatio");
                    tmpMetrics.ReturnOnEquity = ParseDouble(overviewData.Value, "ReturnOnEquityTTM");
                    tmpMetrics.ReturnOnAssets = ParseDouble(overviewData.Value, "ReturnOnAssetsTTM");
                    tmpMetrics.EarningsPerShare = ParseDouble(overviewData.Value, "EPS");
                    tmpMetrics.RevenueGrowth = ParseDouble(overviewData.Value, "RevenueGrowthTTM");
                }

                if (i < balanceReports.Count)
                {
                    var bs = balanceReports[i];
                    tmpMetrics.DebtToEquity = ParseDouble(bs, "totalLiabilities") / ParseDouble(bs, "totalShareholderEquity");
                    tmpMetrics.DebtToAssets = ParseDouble(bs, "totalLiabilities") / ParseDouble(bs, "totalAssets");
                    tmpMetrics.BookValuePerShare = ParseDouble(bs, "totalShareholderEquity") / ParseDouble(bs, "commonStockSharesOutstanding");
                }

                if (i < incomeReports.Count)
                {
                    var inc = incomeReports[i];
                    tmpMetrics.NetMargin = ParseDouble(inc, "netIncome") / ParseDouble(inc, "totalRevenue");
                    tmpMetrics.OperatingIncomeGrowth = ParseDouble(inc, "operatingIncome") / ParseDouble(inc, "totalRevenue");
                }

                if (i < cashFlowReports.Count)
                {
                    var cf = cashFlowReports[i];
                    var sharesOutstanding = ParseDouble(balanceReports[i], "commonStockSharesOutstanding");
                    tmpMetrics.FreeCashFlowPerShare = ParseDouble(cf, "operatingCashflow") / sharesOutstanding;
                    tmpMetrics.PayoutRatio = ParseDouble(cf, "dividendPayout") / ParseDouble(cf, "netIncome");
                }

                if (i < earningsReports.Count)
                {
                    var er = earningsReports[i];
                    tmpMetrics.EarningsPerShareGrowth = ParseDouble(er, "reportedEPS");
                }

                metrics.Add(tmpMetrics);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error fetching metrics for {ticker}: {ex.Message}");
            return false;
        }

        return true;
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

    public async Task<IEnumerable<FinancialLineItem>> SearchLineItemsAsync(string ticker, string[] lineItems, DateTime endDate, string period = "ttm", int limit = 10)
    {
        var result = new List<FinancialLineItem>();
        try
        {
            // Load data from Alpha Vantage
            var balanceSheetData = await FetchDataAsync($"query?function=BALANCE_SHEET&symbol={ticker}&apikey={_apiKey}");
            var incomeStatementData = await FetchDataAsync($"query?function=INCOME_STATEMENT&symbol={ticker}&apikey={_apiKey}");
            var cashFlowData = await FetchDataAsync($"query?function=CASH_FLOW&symbol={ticker}&apikey={_apiKey}");

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
        }
        catch (Exception ex)
        {
            Logger.Error($"SearchLineItemsAsync failed for {ticker}: {ex.Message}");
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
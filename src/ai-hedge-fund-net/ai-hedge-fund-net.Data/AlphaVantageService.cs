using System.Data;
using System.Text;
using System.Text.Json;
using ai_hedge_fund_net.Contracts.Model;
using IDataReader = ai_hedge_fund_net.Contracts.IDataReader;

namespace ai_hedge_fund_net.Data;

public class AlphaVantageService : IDataReader
{
    private readonly HttpClient _client;
    private readonly string _apiKey;

    public AlphaVantageService(HttpClient httpClient, string apiKey)
    {
        _client = httpClient;
        _apiKey = apiKey;
    }

    public async Task<FinancialMetrics> GetFinancialMetricsAsync(string ticker)
    {
        var metrics = new FinancialMetrics { Ticker = ticker };

        try
        {
            // Fetch Overview
            var overviewData = await FetchDataAsync($"query?function=OVERVIEW&symbol={ticker}&apikey={_apiKey}");
            if (overviewData.HasValue)
            {
                metrics.MarketCap = 1000; // TODO ParseDouble(overviewData.Value, "MarketCapitalization");
                metrics.PriceToEarningsRatio = ParseDouble(overviewData.Value, "PERatio");
                metrics.PriceToBookRatio = ParseDouble(overviewData.Value, "PriceToBookRatio");
                metrics.ReturnOnEquity = ParseDouble(overviewData.Value, "ReturnOnEquityTTM");
                metrics.ReturnOnAssets = ParseDouble(overviewData.Value, "ReturnOnAssetsTTM");
                metrics.EarningsPerShare = ParseDouble(overviewData.Value, "EPS");
                metrics.RevenueGrowth = ParseDouble(overviewData.Value, "RevenueGrowthTTM");
            }

            // Fetch Balance Sheet
            var balanceSheetData = await FetchDataAsync($"query?function=BALANCE_SHEET&symbol={ticker}&apikey={_apiKey}");
            if (balanceSheetData.HasValue && balanceSheetData.Value.TryGetProperty("annualReports", out JsonElement reports) && reports.GetArrayLength() > 0)
            {
                var latestBalanceSheet = reports[0];
                metrics.DebtToEquity = ParseDouble(latestBalanceSheet, "totalLiabilities") / ParseDouble(latestBalanceSheet, "totalShareholderEquity");
                metrics.DebtToAssets = ParseDouble(latestBalanceSheet, "totalLiabilities") / ParseDouble(latestBalanceSheet, "totalAssets");
                metrics.BookValuePerShare = ParseDouble(latestBalanceSheet, "totalShareholderEquity") / ParseDouble(latestBalanceSheet, "commonStockSharesOutstanding");
            }

            // Fetch Income Statement
            var incomeStatementData = await FetchDataAsync($"query?function=INCOME_STATEMENT&symbol={ticker}&apikey={_apiKey}");
            if (incomeStatementData.HasValue && incomeStatementData.Value.TryGetProperty("annualReports", out JsonElement incomeReports) && incomeReports.GetArrayLength() > 0)
            {
                var latestIncomeStatement = incomeReports[0];
                metrics.NetMargin = ParseDouble(latestIncomeStatement, "netIncome") / ParseDouble(latestIncomeStatement, "totalRevenue");
                metrics.OperatingIncomeGrowth = ParseDouble(latestIncomeStatement, "operatingIncome") / ParseDouble(latestIncomeStatement, "totalRevenue");
            }

            // Fetch Cash Flow Statement
            var cashFlowData = await FetchDataAsync($"query?function=CASH_FLOW&symbol={ticker}&apikey={_apiKey}");
            if (cashFlowData.HasValue && cashFlowData.Value.TryGetProperty("annualReports", out JsonElement cashFlowReports) && cashFlowReports.GetArrayLength() > 0)
            {
                var latestCashFlow = cashFlowReports[0];
                metrics.FreeCashFlowPerShare = ParseDouble(latestCashFlow, "operatingCashflow") / ParseDouble(balanceSheetData.Value.GetProperty("annualReports")[0], "commonStockSharesOutstanding");
                metrics.PayoutRatio = ParseDouble(latestCashFlow, "dividendPayout") / ParseDouble(latestCashFlow, "netIncome");
            }

            // Fetch Earnings
            var earningsData = await FetchDataAsync($"query?function=EARNINGS&symbol={ticker}&apikey={_apiKey}");
            if (earningsData.HasValue && earningsData.Value.TryGetProperty("annualEarnings", out JsonElement earningsReports) && earningsReports.GetArrayLength() > 0)
            {
                metrics.EarningsPerShareGrowth = ParseDouble(earningsReports[0], "reportedEPS");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching data for {ticker}: {ex.Message}");
        }

        return metrics;
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

    public IEnumerable<Price> GetPrices(string ticker, DateTime startDate, DateTime endDate)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<FinancialMetrics> GetFinancialMetrics(string ticker, DateTime endDate, string period = "ttm", int limit = 10)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<FinancialLineItem> SearchLineItems(string ticker, string[] lineItems, DateTime endDate, string period = "ttm", int limit = 10)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public DataTable GetPriceData(string ticker, DateTime startDate, DateTime endDate)
    {
        throw new NotImplementedException();
    }
}
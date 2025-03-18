using System.Data;
using ai_hedge_fund_net.Contracts.Model;

namespace ai_hedge_fund_net.Contracts;

public interface IDataReader
{
    IEnumerable<Price> GetPrices(string ticker, DateTime startDate, DateTime endDate);
    Task<FinancialMetrics> GetFinancialMetricsAsync(string ticker); // TODO find out which one do we need
    IEnumerable<FinancialMetrics> GetFinancialMetrics(string ticker, DateTime endDate, string period = "ttm", int limit = 10);
    IEnumerable<FinancialLineItem> SearchLineItems(string ticker, string[] lineItems, DateTime endDate, string period = "ttm", int limit = 10);
    IEnumerable<InsiderTrade> GetInsiderTrades(string ticker, DateTime endDate, DateTime? startDate = null, int limit = 1000);
    IEnumerable<CompanyNews> GetCompanyNews(string ticker, DateTime endDate, DateTime startDate, int limit = 1000);
    decimal? GetMarketCap(string ticker, DateTime endDate);
    DataTable PricesToDf(IEnumerable<Price> prices);
    DataTable GetPriceData(string ticker, DateTime startDate, DateTime endDate);
}
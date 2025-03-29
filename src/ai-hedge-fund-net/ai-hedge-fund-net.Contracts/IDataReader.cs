using System.Data;
using ai_hedge_fund_net.Contracts.Model;

namespace ai_hedge_fund_net.Contracts;

public interface IDataReader
{
    IEnumerable<Price> GetPrices(string ticker, DateTime startDate, DateTime endDate);
    bool TryGetFinancialMetricsAsync(string ticker, DateTime endDate, string period, int limit, out IList<FinancialMetrics> metrics);
    bool TryGetFinancialLineItemsAsync(string ticker, DateTime endDate, string period, int limit, out IList<FinancialLineItem> financialLineItems);
    IEnumerable<InsiderTrade> GetInsiderTrades(string ticker, DateTime endDate, DateTime? startDate = null, int limit = 1000);
    IEnumerable<CompanyNews> GetCompanyNews(string ticker, DateTime endDate, DateTime startDate, int limit = 1000);
    decimal? GetMarketCap(string ticker, DateTime endDate);
    DataTable PricesToDf(IEnumerable<Price> prices);
    DataTable GetPriceData(string ticker, DateTime startDate, DateTime endDate);
}
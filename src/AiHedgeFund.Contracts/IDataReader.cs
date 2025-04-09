using AiHedgeFund.Contracts.Model;

namespace AiHedgeFund.Contracts;

public interface IDataReader
{
    bool TryGetPrices(string ticker, DateTime startDate, DateTime endDate, out IEnumerable<Price>? prices);
    bool TryGetFinancialMetrics(string ticker, DateTime endDate, string period, int limit, out IEnumerable<FinancialMetrics>? metrics);
    bool TryGetFinancialLineItems(string ticker, DateTime endDate, string period, int limit, out IEnumerable<FinancialLineItem>? financialLineItems);
    bool TryGetCompanyNews(string ticker, out IEnumerable<NewsSentiment>? newsSentiments);
    //IEnumerable<InsiderTrade> GetInsiderTrades(string ticker, DateTime endDate, DateTime? startDate = null, int limit = 1000);
    //IEnumerable<CompanyNews> GetCompanyNews(string ticker, DateTime endDate, DateTime startDate, int limit = 1000);
    //decimal? GetMarketCap(string ticker, DateTime endDate);
    //DataTable PricesToDf(IEnumerable<Price> prices);
    //DataTable GetPriceData(string ticker, DateTime startDate, DateTime endDate);
}
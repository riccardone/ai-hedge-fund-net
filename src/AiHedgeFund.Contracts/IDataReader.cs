using AiHedgeFund.Contracts.Model;

namespace AiHedgeFund.Contracts;

public interface IDataReader
{
    bool TryGetPrices(string ticker, DateTime startDate, DateTime endDate, out IEnumerable<Price>? prices);
    bool TryGetFinancialMetrics(string ticker, DateTime endDate, string period, int limit, out IEnumerable<FinancialMetrics>? metrics);
    bool TryGetFinancialLineItems(string ticker, DateTime endDate, string period, int limit, out IEnumerable<FinancialLineItem>? financialLineItems);
    bool TryGetCompanyNews(string ticker, out IEnumerable<NewsSentiment>? newsSentiments);
}
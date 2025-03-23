﻿using System.Data;
using ai_hedge_fund_net.Contracts.Model;

namespace ai_hedge_fund_net.Contracts;

public interface IDataReader
{
    Task<IEnumerable<Price>> GetPricesAsync(string ticker, DateTime startDate, DateTime endDate);
    bool TryGetFinancialMetricsAsync(string ticker, DateTime endDate, string period, int limit, out IList<FinancialMetrics> metrics);
    Task<IEnumerable<FinancialLineItem>> SearchLineItemsAsync(string ticker, string[] lineItems, DateTime endDate,
        string period = "ttm", int limit = 10);
    IEnumerable<InsiderTrade> GetInsiderTrades(string ticker, DateTime endDate, DateTime? startDate = null, int limit = 1000);
    IEnumerable<CompanyNews> GetCompanyNews(string ticker, DateTime endDate, DateTime startDate, int limit = 1000);
    decimal? GetMarketCap(string ticker, DateTime endDate);
    DataTable PricesToDf(IEnumerable<Price> prices);
    DataTable GetPriceData(string ticker, DateTime startDate, DateTime endDate);
}
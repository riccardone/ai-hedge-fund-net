using System.Data;
using System.Globalization;
using ai_hedge_fund_net.Contracts.Model;
using IDataReader = ai_hedge_fund_net.Contracts.IDataReader;

namespace ai_hedge_fund_net.DataReader;

public class DataReader : IDataReader
{
    public IEnumerable<Price> GetPrices(string ticker, DateTime startDate, DateTime endDate)
    {
        throw new NotImplementedException();
    }

    public Task<FinancialMetrics> GetFinancialMetricsAsync(string ticker)
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
        var prices = GetPrices(ticker, startDate, endDate);
        return PricesToDf(prices);
    }
}
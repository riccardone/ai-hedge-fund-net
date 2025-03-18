namespace ai_hedge_fund_net.Contracts;

public interface ICaching
{
    List<Dictionary<string, object>>? GetPrices(string ticker);
    void SetPrices(string ticker, List<Dictionary<string, object>> data);
    List<Dictionary<string, object>>? GetFinancialMetrics(string ticker);
    void SetFinancialMetrics(string ticker, List<Dictionary<string, object>> data);
    List<Dictionary<string, object>>? GetLineItems(string ticker);
    void SetLineItems(string ticker, List<Dictionary<string, object>> data);
    List<Dictionary<string, object>>? GetInsiderTrades(string ticker);
    void SetInsiderTrades(string ticker, List<Dictionary<string, object>> data);
    List<Dictionary<string, object>>? GetCompanyNews(string ticker);
    void SetCompanyNews(string ticker, List<Dictionary<string, object>> data);
}
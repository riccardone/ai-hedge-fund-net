namespace AiHedgeFund.Contracts.Model;

/// <summary>
/// rely on FinancialLineItem for trend-based, time-series analysis (e.g., growth)
/// </summary>
public class FinancialLineItem
{
    public FinancialLineItem(string ticker, DateTime reportPeriod, string period, string currency, IReadOnlyDictionary<string, dynamic> extras)
    {
        Ticker = ticker;
        ReportPeriod = reportPeriod;
        Period = period;
        Currency = currency;
        Extras = extras;
    }

    public string Ticker { get; }
    public DateTime ReportPeriod { get; }
    public string Period { get; }
    public string Currency { get; }
    public IReadOnlyDictionary<string, dynamic> Extras { get; }
}
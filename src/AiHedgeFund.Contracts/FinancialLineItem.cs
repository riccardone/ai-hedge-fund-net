namespace AiHedgeFund.Contracts;

public class FinancialLineItem
{
    public FinancialLineItem(string ticker, string reportPeriod, string period, string currency, IReadOnlyDictionary<string, dynamic> extras)
    {
        Ticker = ticker;
        ReportPeriod = reportPeriod;
        Period = period;
        Currency = currency;
        Extras = extras;
    }

    public string Ticker { get; }
    public string ReportPeriod { get; }
    public string Period { get; }
    public string Currency { get; }
    public IReadOnlyDictionary<string, dynamic> Extras { get; }
}
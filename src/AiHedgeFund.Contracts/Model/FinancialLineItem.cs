namespace AiHedgeFund.Contracts.Model;

public class FinancialLineItem
{
    public FinancialLineItem(string ticker, string reportPeriod, string period, string currency, decimal grossMargin, IReadOnlyDictionary<string, dynamic> extras)
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
    public decimal GrossMargin { get; }
    public IReadOnlyDictionary<string, dynamic> Extras { get; }
}
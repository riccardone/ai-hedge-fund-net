namespace AiHedgeFund.Contracts;

public class TradeSignal
{
    public TradeSignal(string ticker, string signal, decimal confidence, string reasoning)
    {
        Ticker = ticker;
        Signal = signal;
        Confidence = confidence;
        Reasoning = reasoning;
    }

    public TradeSignal() { }

    public void SetTicker(string ticker)
    {
        Ticker = ticker;
    }

    public void SetRiskAssessment(RiskAssessment riskAssessment)
    {
        RiskAssessment = riskAssessment;
    }

    public string Ticker { get; set; }
    public string Signal { get; set; }
    public decimal Confidence { get; set; }
    public string Reasoning { get; set; }
    public RiskAssessment RiskAssessment { get; private set; }

    public override string ToString()
    {
        return $"{Ticker} Signal: {Signal} Confidence: {Confidence} Reasoning: {Reasoning}";
    }
}
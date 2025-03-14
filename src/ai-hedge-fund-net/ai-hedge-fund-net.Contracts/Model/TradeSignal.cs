namespace ai_hedge_fund_net.Contracts.Model;

public class TradeSignal
{
    public string Signal { get; set; }
    public decimal Confidence { get; set; }
    public string Reasoning { get; set; }
}
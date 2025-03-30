namespace ai_hedge_fund_net.Contracts.Model;

public class PortfolioDecision
{
    public string Action { get; set; } = "hold"; // buy/sell/short/cover/hold
    public int Quantity { get; set; }
    public float Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}

public class PortfolioManagerOutput
{
    public Dictionary<string, PortfolioDecision> Decisions { get; set; } = new();
}
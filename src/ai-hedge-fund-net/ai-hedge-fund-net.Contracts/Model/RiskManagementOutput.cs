namespace ai_hedge_fund_net.Contracts.Model;

public class RiskManagementOutput
{
    public decimal RemainingPositionLimit { get; set; }
    public decimal CurrentPrice { get; set; }

    public RiskReasoning Reasoning { get; set; } = new();
}

public class RiskReasoning
{
    public decimal PortfolioValue { get; set; }
    public decimal CurrentPosition { get; set; }
    public decimal PositionLimit { get; set; }
    public decimal RemainingLimit { get; set; }
    public decimal AvailableCash { get; set; }
}
namespace ai_hedge_fund_net.Contracts.Model;

public class TradingState
{
    public TradingMessage Message { get; set; } = new();
    public Dictionary<string, object> Data { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}
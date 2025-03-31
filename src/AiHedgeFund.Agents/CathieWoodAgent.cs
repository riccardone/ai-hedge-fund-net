using AiHedgeFund.Contracts;

namespace AiHedgeFund.Agents;

public class CathieWoodAgent
{
    public Task<TradeSignal> Run(TradingWorkflowState state)
    {
        return Task.FromResult(new TradeSignal("AAPL", "bullish", 90m, "Value investment logic"));
    }
}
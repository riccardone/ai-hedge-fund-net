using AiHedgeFund.Contracts;

namespace AiHedgeFund.Agents;

public class CathieWoodAgent
{
    public Task<IEnumerable<TradeSignal>> Run(TradingWorkflowState state)
    {
        return Task.FromResult(new List<TradeSignal>
        {
            new TradeSignal("AAPL", "bullish", 90m, "Value investment logic")
        }.AsEnumerable()); // TODO
    }
}
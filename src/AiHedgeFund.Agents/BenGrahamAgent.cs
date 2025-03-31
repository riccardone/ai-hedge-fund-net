using AiHedgeFund.Contracts;

public abstract class BenGrahamAgent
{
    public Task<TradeSignal> Run(TradingWorkflowState state)
    {
        return Task.FromResult(new TradeSignal("AAPL", "bullish", 90m, "Value investment logic"));
    }
}
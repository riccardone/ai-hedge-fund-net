using AiHedgeFund.Contracts;

namespace AiHedgeFund.Agents;

public class RiskManagerAgent
{
    public Task<RiskAssessment> Run(TradingWorkflowState state)
    {
        return Task.FromResult(new RiskAssessment()); // TODO
    }
}
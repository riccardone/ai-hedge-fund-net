using AiHedgeFund.Contracts;
using NLog;

namespace AiHedgeFund.Agents.Services;

public class PortfolioManager
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IAgentRegistry _agentRegistry;

    public PortfolioManager(IAgentRegistry agentRegistry)
    {
        _agentRegistry = agentRegistry;
    }

    public void Evaluate(string agentName, TradingWorkflowState state)
    {
        if (_agentRegistry.TryGet<TradeSignal>(agentName, out var agentFunc))
        {
            var result = agentFunc(state);
            foreach (var tradeSignal in result)
            {
                Logger.Info(tradeSignal.ToString);
                state.AnalystSignals?[agentName].Add(tradeSignal.Ticker, tradeSignal);
            }
        }
        else
        {
            Logger.Warn("Agent not found or type mismatch");
        }
    }

    public void RunRiskAssessments(string agentName, TradingWorkflowState state, RiskManagerAgent riskAgent)
    {
        var results = riskAgent.Run(state);

        if (!state.RiskAssessments.ContainsKey(agentName))
            state.RiskAssessments[agentName] = new Dictionary<string, RiskAssessment>();

        foreach (var kvp in results)
        {
            state.RiskAssessments[agentName][kvp.Key] = kvp.Value;
        }

        //if (state.ShowReasoning)
        //{
        //    foreach (var (ticker, assessment) in results)
        //    {
        //        var r = assessment.Reasoning;
        //        Logger.Info($"[RiskAssessment:{agentName}] {ticker} - Position: {r.CurrentPosition}, Limit: {r.PositionLimit}, Remaining: {r.RemainingLimit}, Cash: {r.AvailableCash}");
        //    }
        //}
    }
}
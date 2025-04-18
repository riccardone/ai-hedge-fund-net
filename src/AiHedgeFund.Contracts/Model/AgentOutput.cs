namespace AiHedgeFund.Contracts.Model;

public class AgentOutput
{
    public AgentOutput(string agentId, string ticker, DateTime applies, TradeSignal tradeSignal,
        TradeDecision tradeDecision, RiskAssessment riskAssessment)
    {
        AgentId = agentId;
        Ticker = ticker;
        Applies = applies;
        TradeSignal = tradeSignal;
        TradeDecision = tradeDecision;
        RiskAssessment = riskAssessment;
    }

    public string AgentId { get; }
    public string Ticker { get; }
    public DateTime Applies { get; }
    public TradeSignal TradeSignal { get; }
    public TradeDecision TradeDecision { get; }
    public RiskAssessment RiskAssessment { get; }
}
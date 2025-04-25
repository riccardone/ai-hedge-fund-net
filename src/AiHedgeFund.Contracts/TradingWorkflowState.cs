using System.Collections.ObjectModel;
using AiHedgeFund.Contracts.Model;

namespace AiHedgeFund.Contracts;

public class TradingWorkflowState
{
    public IDictionary<string, IEnumerable<NewsSentiment>> CompanyNews { get; set; } = new Dictionary<string, IEnumerable<NewsSentiment>>();
    public IDictionary<string, IEnumerable<FinancialMetrics>> FinancialMetrics { get; set; } = new Dictionary<string, IEnumerable<FinancialMetrics>>();
    public IDictionary<string, IEnumerable<FinancialLineItem>> FinancialLineItems { get; set; } = new Dictionary<string, IEnumerable<FinancialLineItem>>();
    public IDictionary<string, IEnumerable<Price>> Prices { get; set; } = new Dictionary<string, IEnumerable<Price>>();
    public DateTime StartDate { get; set; } 
    public DateTime EndDate { get; set; } 
    //public bool ShowReasoning { get; set; }
    public List<string> SelectedAnalysts { get; set; } = new();
    public string ModelName { get; set; } = "gpt-4o";
    public string ModelProvider { get; set; } = "OpenAI";
    public RiskLevel RiskLevel { get; set; }
    //public decimal MarginRequirement { get; set; }
    //public decimal InitialCash { get; set; }
    public List<string> Tickers { get; set; } = new(); 
    public Portfolio Portfolio { get; set; } = new();
    private readonly Dictionary<string, IDictionary<string, AgentReport>> _analystSignalsInternal = new Dictionary<string, IDictionary<string, AgentReport>>();
    public ReadOnlyDictionary<string, IDictionary<string, AgentReport>> AnalystSignals => new ReadOnlyDictionary<string, IDictionary<string, AgentReport>>(_analystSignalsInternal);

    public void AddOrUpdateAgentReport<T>(AgentReport agentReport)
    {
        if (agentReport?.TradeSignal == null || string.IsNullOrEmpty(agentReport.TradeSignal.Ticker))
            throw new ArgumentException($"{nameof(agentReport)} or its TradeSignal is null/invalid");

        var agentKey = typeof(T).Name.ToSnakeCase();

        if (!_analystSignalsInternal.ContainsKey(agentKey) || _analystSignalsInternal[agentKey] == null)
            _analystSignalsInternal[agentKey] = new Dictionary<string, AgentReport>();

        _analystSignalsInternal[agentKey][agentReport.TradeSignal.Ticker] = agentReport;
    }

    public void AddOrUpdateAgentReport<T>(TradeSignal tradeSignal, IEnumerable<FinancialAnalysisResult> financialAnalysisResults)
    {
        var displayName = typeof(T).Name.ToDisplayName();

        var agentReport = new AgentReport(displayName, tradeSignal, tradeSignal.Confidence,
            tradeSignal.Reasoning, financialAnalysisResults);

        AddOrUpdateAgentReport<T>(agentReport);
    }

    public Dictionary<string, TradeDecision?>? TradeDecisions { get; set; }
    public Dictionary<string, Dictionary<string, RiskAssessment>> RiskAssessments { get; set; } = new();
}

public class Portfolio
{
    public decimal Cash { get; set; }
    public Dictionary<string, Position> Positions { get; set; } = new();
    public Dictionary<string, RealizedGains> RealizedGains { get; set; } = new();

    public Portfolio()
    {
        Positions = new Dictionary<string, Position>();
        RealizedGains = new Dictionary<string, RealizedGains>();
    }
}

public class Position
{
    public int Long { get; set; } = 0;
    public int Short { get; set; } = 0;
    public decimal LongCostBasis { get; set; } = 0.0m;
    public decimal ShortCostBasis { get; set; } = 0.0m;
}

public class RealizedGains
{
    public decimal Long { get; set; } = 0.0m;
    public decimal Short { get; set; } = 0.0m;
}

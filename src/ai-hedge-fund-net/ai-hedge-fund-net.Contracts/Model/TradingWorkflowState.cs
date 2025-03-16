namespace ai_hedge_fund_net.Contracts.Model;

public class TradingWorkflowState
{
    public ITradingAgent TradingAgent { get; set; }  // Store Agent Here
    public List<FinancialMetrics> FinancialMetrics { get; set; } = new();
    public List<FinancialLineItem> FinancialLineItems { get; set; } = new();

    public List<string> Tickers { get; set; } = new();
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public bool ShowReasoning { get; set; }
    public List<string> SelectedAnalysts { get; set; } = new();
    public string ModelName { get; set; } = "gpt-4o";
    public string ModelProvider { get; set; } = "OpenAI";
    public double InitialCash { get; set; } = 100000.0;
    public double MarginRequirement { get; set; } = 0.0;

    public Portfolio Portfolio { get; set; } = new();
    public Dictionary<string, object> AnalystSignals { get; set; }
    public Dictionary<string, TradeDecision> TradeDecisions { get; set; }
}

public class Portfolio
{
    public double Cash { get; set; } = 100000.0;
    public double MarginRequirement { get; set; } = 0.0;
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
    public double LongCostBasis { get; set; } = 0.0;
    public double ShortCostBasis { get; set; } = 0.0;
}

public class RealizedGains
{
    public double Long { get; set; } = 0.0;
    public double Short { get; set; } = 0.0;
}
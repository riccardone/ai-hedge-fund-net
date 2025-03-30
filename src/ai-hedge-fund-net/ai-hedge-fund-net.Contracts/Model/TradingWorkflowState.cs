namespace ai_hedge_fund_net.Contracts.Model;

public class TradingWorkflowState
{
    public IDictionary<string, IEnumerable<FinancialMetrics>> FinancialMetrics { get; set; } = new Dictionary<string, IEnumerable<FinancialMetrics>>();
    public IDictionary<string, IEnumerable<FinancialLineItem>> FinancialLineItems { get; set; } = new Dictionary<string, IEnumerable<FinancialLineItem>>();

    public List<string> Tickers { get; set; } = new();
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public bool ShowReasoning { get; set; }
    public List<string> SelectedAnalysts { get; set; } = new();
    public string ModelName { get; set; } = "gpt-4o";
    public string ModelProvider { get; set; } = "OpenAI";
    public decimal InitialCash { get; set; } = 100000.0m;
    public decimal MarginRequirement { get; set; } = 0.0m;

    public Portfolio Portfolio { get; set; } = new();
    public Dictionary<string, IDictionary<string, object>> AnalystSignals { get; set; }
    public Dictionary<string, TradeDecision> TradeDecisions { get; set; }
}

public class Portfolio
{
    public decimal Cash { get; set; } = 100000.0m;
    public decimal MarginRequirement { get; set; } = 0.0m;
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
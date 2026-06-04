using AiHedgeFund.Contracts.Model;

namespace AiHedgeFund.Contracts;

public class TradingWorkflowState
{
    public IDictionary<string, IEnumerable<NewsSentiment>> CompanyNews { get; } = new Dictionary<string, IEnumerable<NewsSentiment>>();
    public IDictionary<string, IEnumerable<FinancialMetrics>> FinancialMetrics { get; set; } = new Dictionary<string, IEnumerable<FinancialMetrics>>();
    public IDictionary<string, IEnumerable<FinancialLineItem>> FinancialLineItems { get; set; } = new Dictionary<string, IEnumerable<FinancialLineItem>>();
    public IDictionary<string, IEnumerable<Price>> Prices { get; } = new Dictionary<string, IEnumerable<Price>>();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<string> SelectedAnalysts { get; set; } = new();
    public string ModelName { get; set; } = "gpt-4o";
    public string ModelProvider { get; set; } = "OpenAI";
    public RiskLevel RiskLevel { get; set; }
    public List<string> Tickers { get; set; } = new();
    public Portfolio Portfolio { get; set; } = new();
    public Dictionary<string, RiskAssessment> RiskAssessments { get; set; } = new();
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

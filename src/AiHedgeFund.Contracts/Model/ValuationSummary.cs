namespace AiHedgeFund.Contracts.Model;

public class ValuationSummary
{
    public decimal? IntrinsicValue { get; set; }
    public decimal MarginOfSafety { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public decimal ValuationBasis { get; set; }
    public string ValuationBasisLabel { get; set; }
    public decimal GrowthRate { get; set; }
    public decimal DiscountRate { get; set; }
    public int TerminalMultiple { get; set; }
}

public enum RiskLevel
{
    Low,    // Conservative (Buffett-style) - uses Owner Earnings
    Medium, // Balanced - uses Free Cash Flow
    High    // Aggressive - uses FCF with optimistic multipliers
}
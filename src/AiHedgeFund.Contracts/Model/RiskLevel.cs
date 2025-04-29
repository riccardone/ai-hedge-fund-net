namespace AiHedgeFund.Contracts.Model;

public enum RiskLevel
{
    Low,    // Conservative (Buffett-style) - uses Owner Earnings
    Medium, // Balanced - uses Free Cash Flow
    High    // Aggressive - uses FCF with optimistic multipliers
}
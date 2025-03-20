namespace ai_hedge_fund_net.Contracts.Model;

public class FinancialStrength
{
    public FinancialStrength(int score, IEnumerable<string> details)
    {
        Score = score;
        Details = details;
    }

    public int Score { get; }
    public IEnumerable<string> Details { get; }
}
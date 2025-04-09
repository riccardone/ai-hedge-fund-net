namespace AiHedgeFund.Contracts.Model;

public class FinancialAnalysisResult
{
    private readonly List<string> _details = new List<string>();
    public int Score { get; private set; }
    public IEnumerable<string> Details => _details;
    public int MaxScore { get; private set; } = 10;

    public FinancialAnalysisResult() { }

    public FinancialAnalysisResult(int score, IEnumerable<string> details, int maxScore = 10)
    {
        _details = details.ToList();
        Score = score;
        MaxScore = maxScore;
    }

    public void AddDetail(string detail)
    {
        _details.Add(detail);
    }

    public void SetScore(int score)
    {
        Score = score;
    }

    public void IncreaseScore(int score)
    {
        Score += score;
    }
}
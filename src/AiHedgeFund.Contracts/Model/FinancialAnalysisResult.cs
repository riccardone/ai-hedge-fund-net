namespace AiHedgeFund.Contracts.Model;

public class FinancialAnalysisResult
{
    private readonly List<string> _details = new List<string>();
    public int Score { get; private set; }
    public IEnumerable<string> Details => _details;
    public int MaxScore { get; private set; }
    public string Title { get; }

    public FinancialAnalysisResult(string title, int score, IEnumerable<string> details, int maxScore = 10) : this(title, score, maxScore)
    {
        _details = details.ToList();
    }

    public FinancialAnalysisResult(string title, int score, int maxScore)
    {
        Score = score;
        Title = title;
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
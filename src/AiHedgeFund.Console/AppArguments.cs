using AiHedgeFund.Contracts.Model;

namespace AiHedgeFund.Console;

public class AppArguments
{
    public List<string> AgentNames { get; set; } = new();
    public List<string> Tickers { get; set; } = new();
    public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-3);
    public DateTime EndDate { get; set; } = DateTime.Today;

    public RiskLevel RiskLevel { get; private set; } = RiskLevel.Medium; // Default value

    public AppArguments(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--agent":
                    AgentNames.Clear();
                    while (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                        AgentNames.Add(args[++i]);
                    break;

                case "--tickers":
                    Tickers.Clear();
                    while (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                        Tickers.Add(args[++i].ToUpperInvariant());
                    break;

                case "--start-date":
                    if (i + 1 < args.Length && DateTime.TryParse(args[++i], out var start))
                        StartDate = start;
                    else
                        throw new ArgumentException("Invalid or missing --start-date");
                    break;

                case "--end-date":
                    if (i + 1 < args.Length && DateTime.TryParse(args[++i], out var end))
                        EndDate = end;
                    else
                        throw new ArgumentException("Invalid or missing --end-date");
                    break;

                case "--risk-level":
                    if (i + 1 < args.Length && Enum.TryParse<RiskLevel>(args[++i], true, out var riskLevel))
                        RiskLevel = riskLevel;
                    else
                        throw new ArgumentException("Invalid or missing --risk-level. Use low, medium, or high.");
                    break;
            }
        }

        if (Tickers.Count == 0)
            throw new ArgumentException("At least one ticker must be specified using --tickers.");

        if (StartDate > EndDate)
            throw new ArgumentException("StartDate must be before EndDate.");
    }
}
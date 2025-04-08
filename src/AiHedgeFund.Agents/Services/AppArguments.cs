namespace AiHedgeFund.Agents.Services;

public class AppArguments
{
    public List<string> AgentNames { get; set; } = new() { "charlie_munger" }; // "ben_graham" "cathie_wood" "bill_ackman"
    public string RiskLevel { get; set; } = "medium";
    public List<string> Tickers { get; set; } = new() { "MSFT" };

    public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-3);
    public DateTime EndDate { get; set; } = DateTime.Today;

    public decimal InitialCash { get; set; } = 100_000m;
    public decimal MarginRequirement { get; set; } = 0.5m;

    public AppArguments(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--agent":
                    while (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                        AgentNames.Add(args[++i]);
                    break;

                case "--risk-level":
                    if (i + 1 < args.Length)
                        RiskLevel = args[++i];
                    break;

                case "--tickers":
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

                case "--initial-cash":
                    if (i + 1 < args.Length && decimal.TryParse(args[++i], out var cash))
                        InitialCash = cash;
                    else
                        throw new ArgumentException("Invalid or missing --initial-cash");
                    break;

                case "--margin-requirement":
                    if (i + 1 < args.Length && decimal.TryParse(args[++i], out var margin))
                        MarginRequirement = margin;
                    else
                        throw new ArgumentException("Invalid or missing --margin-requirement");
                    break;
            }
        }

        if (Tickers.Count == 0)
            throw new ArgumentException("At least one ticker must be specified using --tickers.");

        if (StartDate > EndDate)
            throw new ArgumentException("StartDate must be before EndDate.");
    }
}

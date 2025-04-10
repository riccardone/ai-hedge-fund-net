﻿namespace AiHedgeFund.Agents.Services;

public class AppArguments
{
    private static readonly List<string> AvailableAgents = new()
    {
        "charlie_munger", "stanley_druckenmiller", "ben_graham", "cathie_wood", "bill_ackman"
    };

    public List<string> AgentNames { get; set; } = new() { "charlie_munger" };
    public List<string> Tickers { get; set; } = new() { "MSFT" };
    public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-3);
    public DateTime EndDate { get; set; } = DateTime.Today;

    public AppArguments(string[] args)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            PrintHelp();
            Environment.Exit(0);
        }

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
            }
        }

        if (Tickers.Count == 0)
            throw new ArgumentException("At least one ticker must be specified using --tickers.");

        if (StartDate > EndDate)
            throw new ArgumentException("StartDate must be before EndDate.");
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  --agent [names]        : One or more agent names to run (default: charlie_munger)");
        Console.WriteLine("                           Available agents:");
        foreach (var agent in AvailableAgents)
        {
            Console.WriteLine($"                             - {agent}");
        }

        Console.WriteLine("  --tickers [symbols]    : One or more stock tickers (e.g., MSFT AAPL TSLA)");
        Console.WriteLine("  --start-date YYYY-MM-DD: Optional start date (default: 3 months ago)");
        Console.WriteLine("  --end-date YYYY-MM-DD  : Optional end date (default: today)");
        Console.WriteLine("  --help or -h           : Show this help message and exit");
    }
}

namespace AiHedgeFund.Agents.Services;

public class AppArguments
{
    public string AgentName { get; set; } = "ben_graham";
    public string RiskLevel { get; set; } = "medium";

    public AppArguments(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--agent":
                    if (i + 1 < args.Length)
                        AgentName = args[++i];
                    break;
                case "--risk-level":
                    if (i + 1 < args.Length)
                        RiskLevel = args[++i];
                    break;
            }
        }
    }
}
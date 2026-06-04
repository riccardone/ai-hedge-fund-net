using AiHedgeFund.Agents;
using AiHedgeFund.Agents.Services;
using AiHedgeFund.Contracts;
using Microsoft.Extensions.Hosting;

namespace AiHedgeFund.Console;

public class MainApp : IHostedService
{
    private readonly TradingInitializer _initializer;
    private readonly PortfolioManager _portfolio;
    private readonly RiskManagerAgent _riskAgent;

    public MainApp(TradingInitializer initializer, PortfolioManager portfolio, RiskManagerAgent riskAgent)
    {
        _initializer = initializer;
        _portfolio = portfolio;
        _riskAgent = riskAgent;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var state = await _initializer.InitializeAsync();

        var allResults = new List<AgentResult>();
        foreach (var agent in state.SelectedAnalysts)
        {
            var results = _portfolio.Evaluate(agent, state);
            allResults.AddRange(results);
        }

        _portfolio.RunRiskAssessments(state, _riskAgent, allResults);

        foreach (var group in allResults.GroupBy(r => r.AgentKey))
        {
            var first = group.First();
            ConsoleOutputFormatter.PrintAgentReport(
                first.AgentKey,
                first.AgentDisplayName,
                state.ModelProvider,
                state.ModelName,
                state.RiskLevel.ToString(),
                state.StartDate,
                state.EndDate,
                group.ToList()
            );
        }

        Environment.Exit(0);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
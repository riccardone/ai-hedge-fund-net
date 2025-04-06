using Microsoft.Extensions.Hosting;
using NLog;

namespace AiHedgeFund.Agents.Services;

public class MainApp : IHostedService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly AppArguments _args;
    private readonly TradingInitializer _initializer;
    private readonly PortfolioManager _portfolio;
    private readonly RiskManagerAgent _riskAgent;

    public MainApp(AppArguments args, TradingInitializer initializer, PortfolioManager portfolio, RiskManagerAgent riskAgent)
    {
        _args = args;
        _initializer = initializer;
        _portfolio = portfolio;
        _riskAgent = riskAgent;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var state = await _initializer.InitializeAsync();

        foreach (var agent in state.SelectedAnalysts)
            await _portfolio.EvaluateAsync(agent, state);

        _portfolio.RunRiskAssessments("risk_management_agent", state, _riskAgent);

        Environment.Exit(0); 
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
using AiHedgeFund.Agents;
using AiHedgeFund.Agents.Services;
using Microsoft.Extensions.Hosting;

namespace AiHedgeFund.Console;

public class MainApp : IHostedService
{
    private readonly AppArguments _appArguments;
    private readonly TradingInitializer _initializer;
    private readonly PortfolioManager _portfolio;
    private readonly RiskManagerAgent _riskAgent;

    public MainApp(AppArguments appArguments, TradingInitializer initializer, PortfolioManager portfolio, RiskManagerAgent riskAgent)
    {
        _appArguments = appArguments;
        _initializer = initializer;
        _portfolio = portfolio;
        _riskAgent = riskAgent;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var state = await _initializer.InitializeAsync(_appArguments.AgentNames, _appArguments.Tickers,
            _appArguments.RiskLevel, _appArguments.StartDate, _appArguments.EndDate);

        foreach (var agent in state.SelectedAnalysts)
            _portfolio.Evaluate(agent, state);

        _portfolio.RunRiskAssessments("risk_management_agent", state, _riskAgent);

        Environment.Exit(0); 
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
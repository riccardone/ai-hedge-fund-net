using AiHedgeFund.Contracts;
using Microsoft.Extensions.Hosting;
using NLog;

namespace AiHedgeFund.Agents.Services;

public class MainApp : IHostedService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly PortfolioManager _portfolio;
    private readonly AppArguments _args;

    public MainApp(PortfolioManager portfolio, AppArguments args)
    {
        _portfolio = portfolio;
        _args = args;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.Info("Starting with agent: {0}, risk-level: {1}", _args.AgentName, _args.RiskLevel);

        var state = new TradingWorkflowState
        {
            // Use _args.RiskLevel if needed
        };

        await _portfolio.EvaluateAsync(_args.AgentName, state);
        Environment.Exit(0);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
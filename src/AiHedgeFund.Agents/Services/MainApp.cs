using AiHedgeFund.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AiHedgeFund.Agents.Services;

public class MainApp : IHostedService
{
    private readonly ILogger<MainApp> _logger;
    private readonly PortfolioManager _portfolio;

    public MainApp(ILogger<MainApp> logger, PortfolioManager portfolio)
    {
        _logger = logger;
        _portfolio = portfolio;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var state = new TradingWorkflowState
        {
            // Populate your workflow state here
        };

        await _portfolio.EvaluateAsync("ben_graham", state);
        await _portfolio.EvaluateAsync("risk_manager", state);

        // End app after main logic
        Environment.Exit(0);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
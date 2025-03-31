using AiHedgeFund.Contracts;
using Microsoft.Extensions.Hosting;

namespace AiHedgeFund.Agents.Registry;

public class AgentBootstrapper : IHostedService
{
    private readonly IAgentRegistry _registry;
    private readonly BenGrahamAgent _benGraham;
    private readonly CathieWoodAgent _cathieWood;
    private readonly RiskManagerAgent _riskManager;

    public AgentBootstrapper(
        IAgentRegistry registry,
        BenGrahamAgent benGraham,
        CathieWoodAgent cathieWood,
        RiskManagerAgent riskManager)
    {
        _registry = registry;
        _benGraham = benGraham;
        _cathieWood = cathieWood;
        _riskManager = riskManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _registry.Register("ben_graham", _benGraham.Run);
        _registry.Register("cathie_wood", _cathieWood.Run);
        _registry.Register("risk_manager", _riskManager.Run);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
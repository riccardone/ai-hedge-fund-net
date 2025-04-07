using AiHedgeFund.Contracts;
using Microsoft.Extensions.Hosting;

namespace AiHedgeFund.Agents.Registry;

public class AgentBootstrapper : IHostedService
{
    private readonly IAgentRegistry _registry;
    private readonly BenGrahamAgent _benGraham;
    private readonly CathieWoodAgent _cathieWood;
    private readonly BillAckmanAgent _billAckman;

    public AgentBootstrapper(
        IAgentRegistry registry,
        BenGrahamAgent benGraham,
        CathieWoodAgent cathieWood,
        BillAckmanAgent billAckman)
    {
        _registry = registry;
        _benGraham = benGraham;
        _cathieWood = cathieWood;
        _billAckman = billAckman;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _registry.Register("ben_graham", _benGraham.Run);
        _registry.Register("cathie_wood", _cathieWood.Run);
        _registry.Register("bill_ackman", _billAckman.Run);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
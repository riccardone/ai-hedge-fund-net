using AiHedgeFund.Contracts;
using Microsoft.Extensions.Hosting;

namespace AiHedgeFund.Agents.Registry;

public class AgentBootstrapper : IHostedService
{
    private readonly IAgentRegistry _registry;
    private readonly BenGrahamAgent _benGraham;
    private readonly CathieWoodAgent _cathieWood;
    private readonly BillAckmanAgent _billAckman;
    private readonly CharlieMungerAgent _charlieMunger;
    private readonly StanleyDruckenmillerAgent _stanleyDruckenmiller;
    private readonly WarrenBuffettAgent _warrenBuffett;

    public AgentBootstrapper(
        IAgentRegistry registry,
        BenGrahamAgent benGraham,
        CathieWoodAgent cathieWood,
        BillAckmanAgent billAckman,
        CharlieMungerAgent charlieMunger,
        StanleyDruckenmillerAgent stanleyDruckenmiller,
        WarrenBuffettAgent warrenBuffett)
    {
        _registry = registry;
        _benGraham = benGraham;
        _cathieWood = cathieWood;
        _billAckman = billAckman;
        _charlieMunger = charlieMunger;
        _stanleyDruckenmiller = stanleyDruckenmiller;
        _warrenBuffett = warrenBuffett;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _registry.Register("ben_graham", _benGraham.Run);
        _registry.Register("cathie_wood", _cathieWood.Run);
        _registry.Register("bill_ackman", _billAckman.Run);
        _registry.Register("charlie_munger", _charlieMunger.Run);
        _registry.Register("stanley_druckenmiller", _stanleyDruckenmiller.Run);
        _registry.Register("warren_buffett", _warrenBuffett.Run);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
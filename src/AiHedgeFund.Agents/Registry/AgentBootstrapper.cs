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
        _registry.Register($"{nameof(BenGrahamAgent).ToSnakeCase()}", _benGraham.Run);
        _registry.Register($"{nameof(CathieWoodAgent).ToSnakeCase()}", _cathieWood.Run);
        _registry.Register($"{nameof(BillAckmanAgent).ToSnakeCase()}", _billAckman.Run);
        _registry.Register($"{nameof(CharlieMungerAgent).ToSnakeCase()}", _charlieMunger.Run);
        _registry.Register($"{nameof(StanleyDruckenmillerAgent).ToSnakeCase()}", _stanleyDruckenmiller.Run);
        _registry.Register($"{nameof(WarrenBuffettAgent).ToSnakeCase()}", _warrenBuffett.Run);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
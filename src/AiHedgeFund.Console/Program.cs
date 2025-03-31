using AiHedgeFund.Agents;
using AiHedgeFund.Agents.Registry;
using AiHedgeFund.Agents.Services;
using AiHedgeFund.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AiHedgeFund.Console;

internal class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register AgentRegistry and Agents
                services.AddSingleton<IAgentRegistry, AgentRegistry>();
                services.AddSingleton<BenGrahamAgent>();
                services.AddSingleton<CathieWoodAgent>();
                services.AddSingleton<RiskManagerAgent>();

                // Hosted service to register agents at startup
                services.AddHostedService<AgentBootstrapper>();

                // App services
                services.AddTransient<PortfolioManager>();
                services.AddHostedService<MainApp>();
            })
            .Build();

        await host.RunAsync();
    }
}
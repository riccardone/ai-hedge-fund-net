namespace ai_hedge_fund_net.ConsoleApp;

public static class ServiceLocator
{
    public static IServiceProvider Instance { get; private set; }

    public static void Init(IServiceProvider serviceProvider)
    {
        Instance = serviceProvider;
    }
}
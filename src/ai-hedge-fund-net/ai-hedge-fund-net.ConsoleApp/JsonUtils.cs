using NLog;
using System.Text.Json;

namespace ai_hedge_fund_net.ConsoleApp;

public static class JsonUtils
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static Dictionary<string, object>? ParseJson(string jsonResponse)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse);
        }
        catch (JsonException ex)
        {
            Logger.Error($"JSON Parsing Error: {ex.Message}");
            return null;
        }
    }
}
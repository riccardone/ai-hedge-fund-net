using System.Text.Json;

namespace ai_hedge_fund_net.ConsoleApp;

public static class JsonUtils
{
    public static Dictionary<string, object>? ParseJson(string jsonResponse)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON Parsing Error: {ex.Message}");
            return null;
        }
    }
}
using System.Text.Json;
using System.Text.RegularExpressions;
using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;

namespace AiHedgeFund.Agents.Services;

public static class LlmTradeSignalGenerator
{
    public static bool TryGenerateSignal(
        IHttpLib httpLib,
        string endpoint,
        string ticker,
        string systemMessage,
        object analysisData,
        string agentName,
        out TradeSignal tradeSignal,
        string model = "gpt-4o-mini",
        BaseRate? baseRate = null)
    {
        tradeSignal = default!;

        // Off-by-default grounding: when a base rate is supplied, give the model the
        // realized forward-return distribution of analogous historical setups as context
        // to reason WITH. It is advisory only and never forces a particular signal.
        var grounding = baseRate is null
            ? string.Empty
            : "\n\nHistorical base-rate context (advisory — realized outcomes of analogous "
              + $"past setups, not a forecast):\n{baseRate.ToContextLine()}\n";

        var userMessage = $@"Based on the following analysis, create a {agentName}-style investment signal:

Analysis Data for {ticker}:
{JsonSerializer.Serialize(analysisData, new JsonSerializerOptions { WriteIndented = true })}
{grounding}
Return JSON exactly in this format:
{{
  ""signal"": ""bullish"" or ""bearish"" or ""neutral"",
  ""confidence"": float (0-100),
  ""reasoning"": ""string""
}}";

        var payload = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemMessage },
                new { role = "user", content = userMessage }
            },
            temperature = 0.2
        };

        var json = JsonSerializer.Serialize(payload);

        if (!httpLib.TryPost(endpoint, json, out var response))
        {
            tradeSignal = new TradeSignal(ticker, "neutral", 0, "Error posting to LLM. Defaulting to neutral.");
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(response);
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content")
                .GetString();
            if (string.IsNullOrWhiteSpace(content))
            {
                tradeSignal = new TradeSignal(ticker, "neutral", 0, "Empty response from LLM.");
                return false;
            }

            if (!TryExtractJson(content, out var cleanedJson))
            {
                tradeSignal = new TradeSignal(ticker, "neutral", 0, "Failed to extract valid JSON from LLM response.");
                return false;
            }

            var result = JsonSerializer.Deserialize<TradeSignal>(cleanedJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null)
            {
                tradeSignal = new TradeSignal(ticker, "neutral", 0, "Deserialization failed.");
                return false;
            }

            result.SetTicker(ticker);
            tradeSignal = result;
            return true;
        }
        catch (Exception ex)
        {
            tradeSignal = new TradeSignal(ticker, "neutral", 0, $"LLM output error: {ex.Message}");
            return false;
        }
    }

    private static bool TryExtractJson(string content, out string json)
    {
        json = string.Empty;

        if (content.StartsWith("```"))
        {
            int start = content.IndexOf("{");
            int end = content.LastIndexOf("}");
            if (start >= 0 && end > start)
            {
                json = content[start..(end + 1)];
                return true;
            }
        }

        var match = Regex.Match(content, "\\{[\\s\\S]*?\\}");
        if (match.Success)
        {
            json = match.Value;
            return true;
        }

        if (content.Trim().StartsWith("{") && content.Trim().EndsWith("}"))
        {
            json = content.Trim();
            return true;
        }

        return false;
    }
}
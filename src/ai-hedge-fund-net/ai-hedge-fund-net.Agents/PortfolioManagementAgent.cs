using System.Text.Json;
using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;

namespace ai_hedge_fund_net.Agents;

public class PortfolioManagementAgent : ITradingAgent
{
    public string Name => nameof(PortfolioManagementAgent);

    private readonly TradingWorkflowState _state;
    private readonly IChatter _chatter;

    public PortfolioManagementAgent(TradingWorkflowState state, IChatter chatter)
    {
        _state = state;
        _chatter = chatter;
    }

    public PortfolioManagerOutput GeneratePortfolioDecision()
    {
        var tickers = _state.Tickers;
        var analystSignals = _state.AnalystSignals;
        var portfolio = _state.Portfolio;

        var currentPrices = new Dictionary<string, decimal>();
        var positionLimits = new Dictionary<string, decimal>();
        var maxShares = new Dictionary<string, int>();
        var signalsByTicker = new Dictionary<string, Dictionary<string, object>>();

        foreach (var ticker in tickers)
        {
            if (analystSignals.TryGetValue("risk_management_agent", out var riskDict) &&
                riskDict.TryGetValue(ticker, out var obj) && obj is RiskManagementOutput riskSignal)
            {
                currentPrices[ticker] = riskSignal.CurrentPrice;
                positionLimits[ticker] = riskSignal.RemainingPositionLimit;

                if (currentPrices[ticker] > 0)
                    maxShares[ticker] = (int)(positionLimits[ticker] / currentPrices[ticker]);
                else
                    maxShares[ticker] = 0;

                var signals = new Dictionary<string, object>();
                foreach (var kvp in analystSignals.Where(x => x.Key != "risk_management_agent"))
                {
                    if (kvp.Value.TryGetValue(ticker, out var rawSignal) && rawSignal is TradeSignal signal)
                    {
                        signals[kvp.Key] = new
                        {
                            signal = signal.Signal,
                            confidence = signal.Confidence
                        };
                    }
                }
                signalsByTicker[ticker] = signals;
            }
        }

        var systemMessage = @"You are a portfolio manager making final trading decisions based on multiple tickers. 

Trading Rules:
- Buy only with available cash; sell only if you hold.
- Short only with available margin; cover only if you have short positions.
- Follow max share limits per ticker and margin rules.
- Actions: buy, sell, short, cover, hold";

        var userMessage = $@"Based on the team's analysis, make trading decisions.

Signals by Ticker:
{JsonSerializer.Serialize(signalsByTicker, new JsonSerializerOptions { WriteIndented = true })}

Prices:
{JsonSerializer.Serialize(currentPrices)}

Max Shares:
{JsonSerializer.Serialize(maxShares)}

Portfolio Cash: {portfolio.Cash}
Current Positions: {JsonSerializer.Serialize(portfolio.Positions)}
Margin Requirement: {portfolio.MarginRequirement}

Return JSON:
{{
  ""decisions"": {{
    ""TICKER1"": {{ ""action"": ""buy/sell/short/cover/hold"", ""quantity"": int, ""confidence"": float, ""reasoning"": ""string"" }},
    ...
  }}
}}";

        var payload = new
        {
            model = _state.ModelName,
            messages = new[]
            {
                new { role = "system", content = systemMessage },
                new { role = "user", content = userMessage }
            },
            temperature = 0.2
        };

        var json = JsonSerializer.Serialize(payload);
        if (!_chatter.TryPost("chat/completions", json, out var response))
        {
            return new PortfolioManagerOutput(); // default to no decisions
        }

        using var doc = JsonDocument.Parse(response);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        if (string.IsNullOrWhiteSpace(content)) return new PortfolioManagerOutput();

        if (!TryExtractJson(content, out var jsonString)) return new PortfolioManagerOutput();

        try
        {
            var result = JsonSerializer.Deserialize<PortfolioManagerOutput>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new PortfolioManagerOutput();
        }
        catch
        {
            return new PortfolioManagerOutput();
        }
    }

    private bool TryExtractJson(string content, out string json)
    {
        json = "";
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            json = content.Substring(start, end - start + 1);
            return true;
        }
        return false;
    }

    public FinancialAnalysisResult AnalyzeEarningsStability(string ticker) => throw new NotImplementedException();
    public FinancialAnalysisResult AnalyzeFinancialStrength(string ticker) => throw new NotImplementedException();
    public FinancialAnalysisResult AnalyzeValuation(string ticker) => throw new NotImplementedException();
    public TradeSignal GenerateOutput(string ticker) => throw new NotImplementedException();
}
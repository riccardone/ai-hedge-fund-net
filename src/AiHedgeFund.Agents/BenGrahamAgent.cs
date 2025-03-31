using System.Text.Json;
using System.Text.RegularExpressions;
using AiHedgeFund.Contracts;
using NLog;

namespace AiHedgeFund.Agents;

public class BenGrahamAgent
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IChatter _chatter;

    public BenGrahamAgent(IChatter chatter)
    {
        _chatter = chatter;
    }

    public async Task<TradeSignal> Run(TradingWorkflowState state)
    {
        var ticker = state.Tickers.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(ticker))
            return new TradeSignal(ticker, "neutral", 0, "No ticker provided.");

        Logger.Info("[BenGraham] Starting analysis for {0}", ticker);

        var earnings = AnalyzeEarningsStability(state, ticker);
        var strength = AnalyzeFinancialStrength(state, ticker);
        var valuation = AnalyzeValuation(state, ticker);

        Logger.Info("{0} Earnings Stability: {1}", ticker, string.Join("; ", earnings.Details));
        Logger.Info("{0} Financial Strength: {1}", ticker, string.Join("; ", strength.Details));
        Logger.Info("{0} Valuation: {1}", ticker, string.Join("; ", valuation.Details));

        return await GenerateOutput(state, ticker);
    }

    private FinancialAnalysisResult AnalyzeEarningsStability(TradingWorkflowState state, string ticker)
    {
        var result = new FinancialAnalysisResult();
        result.SetScore(0);

        var epsValues = state.FinancialLineItems[ticker]
            .Where(item => item.Extras.ContainsKey("EarningsPerShare"))
            .Select(item => item.Extras["EarningsPerShare"])
            .ToList();

        if (epsValues.Count < 2)
        {
            result.AddDetail("Not enough multi-year EPS data.");
            return result;
        }

        int positiveYears = epsValues.Count(e => e > 0);
        if (positiveYears == epsValues.Count)
        {
            result.IncreaseScore(3);
            result.AddDetail("EPS was positive in all periods.");
        }
        else if (positiveYears >= epsValues.Count * 0.8)
        {
            result.IncreaseScore(2);
            result.AddDetail("EPS was positive in most periods.");
        }
        else
        {
            result.AddDetail("EPS was negative in multiple periods.");
        }

        if (epsValues.Last() > epsValues.First())
        {
            result.IncreaseScore(1);
            result.AddDetail("EPS grew over time.");
        }
        else
        {
            result.AddDetail("EPS did not grow.");
        }

        return result;
    }

    private FinancialAnalysisResult AnalyzeFinancialStrength(TradingWorkflowState state, string ticker)
    {
        var result = new FinancialAnalysisResult();
        result.SetScore(0);

        if (!state.FinancialLineItems.TryGetValue(ticker, out var items) || !TryGetLatestCompleteItem(items, out var latest))
        {
            result.AddDetail("No data for financial strength.");
            return result;
        }

        var extras = latest.Extras;

        decimal assets = extras["TotalAssets"];
        decimal liabilities = extras["TotalLiabilities"];
        decimal currentAssets = extras["TotalCurrentAssets"];
        decimal currentLiabilities = extras["TotalCurrentLiabilities"];

        if (currentLiabilities > 0)
        {
            var ratio = currentAssets / currentLiabilities;
            if (ratio >= 2) result.IncreaseScore(2);
            else if (ratio >= 1.5m) result.IncreaseScore(1);

            result.AddDetail($"Current ratio = {ratio:F2}");
        }

        if (assets > 0)
        {
            var debtRatio = liabilities / assets;
            if (debtRatio < 0.5m) result.IncreaseScore(2);
            else if (debtRatio < 0.8m) result.IncreaseScore(1);

            result.AddDetail($"Debt ratio = {debtRatio:F2}");
        }

        return result;
    }

    private FinancialAnalysisResult AnalyzeValuation(TradingWorkflowState state, string ticker)
    {
        var result = new FinancialAnalysisResult();
        result.SetScore(0);

        if (!state.FinancialLineItems.TryGetValue(ticker, out var items) || !TryGetLatestCompleteItem(items, out var latest))
        {
            result.AddDetail("No data for valuation.");
            return result;
        }

        var marketCap = state.FinancialMetrics[ticker].Last().MarketCap;
        if (marketCap <= 0)
        {
            result.AddDetail("Market Cap missing.");
            return result;
        }

        decimal ncav = (latest.Extras["TotalAssets"] ?? 0) - (latest.Extras["TotalLiabilities"] ?? 0);
        if (ncav > marketCap)
        {
            result.IncreaseScore(4);
            result.AddDetail("NCAV > Market Cap (deep value).");
        }
        else if (ncav >= marketCap * 0.67m)
        {
            result.IncreaseScore(2);
            result.AddDetail("NCAV >= 2/3 Market Cap.");
        }

        return result;
    }

    private static bool TryGetLatestCompleteItem(IEnumerable<FinancialLineItem> items, out FinancialLineItem? result)
    {
        foreach (var item in items.Reverse())
        {
            var x = item.Extras;
            if (x.ContainsKey("TotalAssets") && x.ContainsKey("TotalLiabilities"))
            {
                result = item;
                return true;
            }
        }

        result = null;
        return false;
    }

    private async Task<TradeSignal> GenerateOutput(TradingWorkflowState state, string ticker)
    {
        var analysisData = new Dictionary<string, object>
        {
            { "FinancialMetrics", state.FinancialMetrics },
            { "FinancialLineItems", state.FinancialLineItems },
            { "StartDate", state.StartDate },
            { "EndDate", state.EndDate },
            { "InitialCash", state.InitialCash },
            { "MarginRequirement", state.MarginRequirement },
            { "Portfolio", state.Portfolio }
        };

        var systemMessage = @"You are a Benjamin Graham AI agent, using conservative value investing principles:
- Prefer margin of safety (buy below intrinsic value)
- Focus on strong balance sheets
- Favor consistent earnings
- Consider dividend track record
- Avoid speculation

Give a clear signal with confidence and reasoning.";

        var userMessage = @$"Based on this data, generate a signal for {ticker}:

{JsonSerializer.Serialize(analysisData)}

Respond with:
{{
  ""signal"": ""bullish|bearish|neutral"",
  ""confidence"": float (0-100),
  ""reasoning"": ""string""
}}";

        var payload = new
        {
            model = state.ModelName,
            messages = new[]
            {
                new { role = "system", content = systemMessage },
                new { role = "user", content = userMessage }
            },
            temperature = 0.2
        };

        var body = JsonSerializer.Serialize(payload);

        if (!_chatter.TryPost("chat/completions", body, out var response))
        {
            return new TradeSignal(ticker, "neutral", 0, $"Chat failed: {response}");
        }

        if (!TryExtractJson(response, out var json))
        {
            return new TradeSignal(ticker, "neutral", 0, "Invalid response from model.");
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<TradeSignal>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            parsed?.SetTicker(ticker);
            return parsed ?? new TradeSignal(ticker, "neutral", 0, "Unable to parse model response.");
        }
        catch
        {
            return new TradeSignal(ticker, "neutral", 0, "Exception during model response parsing.");
        }
    }

    private bool TryExtractJson(string content, out string json)
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

        var match = Regex.Match(content, @"\{[\s\S]*?\}");
        if (match.Success)
        {
            json = match.Value;
            return true;
        }

        if (content.Trim().StartsWith("{") && content.Trim().EndsWith("}"))
        {
            json = content;
            return true;
        }

        return false;
    }
}

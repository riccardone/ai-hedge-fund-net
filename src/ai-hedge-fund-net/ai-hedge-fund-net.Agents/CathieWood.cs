using ai_hedge_fund_net.Contracts.Model;
using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Extensions;
using System.Text.Json;

public class CathieWood : ITradingAgent
{
    private readonly TradingWorkflowState _state;
    private readonly IChatter _chatter;

    public CathieWood(TradingWorkflowState state, IChatter chatter)
    {
        _state = state;
        _chatter = chatter;
    }

    public string Name { get; }

    public FinancialAnalysisResult AnalyzeEarningsStability(string ticker)
    {
        if (!_state.FinancialLineItems.TryGetValue(ticker, out var items) || items == null || !items.Any())
        {
            return new FinancialAnalysisResult(0, new[] { "No financial line item data available" }, 5);
        }

        int score = 0;
        var details = new List<string>();

        var revenues = items.Select(i => i.GetDecimal("revenue")).Where(r => r.HasValue).Select(r => r.Value).ToList();
        if (revenues.Count >= 3)
        {
            var growthRates = new List<double>();
            for (int i = 0; i < revenues.Count - 1; i++)
            {
                var growth = (revenues[i + 1] - revenues[i]) / Math.Abs(revenues[i]);
                growthRates.Add((double)growth);
            }

            if (growthRates.Count >= 2 && growthRates[^1] > growthRates[0])
            {
                score += 2;
                details.Add($"Revenue growth is accelerating: {growthRates[^1] * 100:F1}% vs {growthRates[0] * 100:F1}%");
            }

            var latestGrowth = growthRates[^1];
            if (latestGrowth > 1.0)
                score += 3;
            else if (latestGrowth > 0.5)
                score += 2;
            else if (latestGrowth > 0.2)
                score += 1;

            details.Add($"Latest revenue growth: {latestGrowth * 100:F1}%");
        }
        else
        {
            details.Add("Insufficient revenue data for growth analysis");
        }

        var grossMargins = items.Select(i => i.GetDecimal("gross_margin")).Where(m => m.HasValue).Select(m => m.Value).ToList();
        if (grossMargins.Count >= 2)
        {
            var trend = grossMargins[^1] - grossMargins[0];
            if (trend > 0.05m)
                score += 2;
            else if (trend > 0)
                score += 1;

            if (grossMargins[^1] > 0.5m)
                score += 2;

            details.Add($"Gross margin trend: +{trend * 100:F1}%, latest: {grossMargins[^1] * 100:F1}%");
        }
        else
        {
            details.Add("Insufficient gross margin data");
        }

        var operatingExpenses = items.Select(i => i.GetDecimal("operating_expense")).Where(o => o.HasValue).Select(o => o.Value).ToList();
        if (revenues.Count >= 2 && operatingExpenses.Count >= 2)
        {
            var revGrowth = (revenues[^1] - revenues[0]) / Math.Abs(revenues[0]);
            var opexGrowth = (operatingExpenses[^1] - operatingExpenses[0]) / Math.Abs(operatingExpenses[0]);
            if (revGrowth > opexGrowth)
            {
                score += 2;
                details.Add("Positive operating leverage: revenue growing faster than expenses");
            }
        }
        else
        {
            details.Add("Insufficient data for operating leverage analysis");
        }

        var rdExpenses = items.Select(i => i.GetDecimal("research_and_development")).Where(rd => rd.HasValue).Select(rd => rd.Value).ToList();
        if (rdExpenses.Any() && revenues.Any())
        {
            var rdIntensity = rdExpenses[^1] / revenues[^1];
            if (rdIntensity > 0.15m)
                score += 3;
            else if (rdIntensity > 0.08m)
                score += 2;
            else if (rdIntensity > 0.05m)
                score += 1;

            details.Add($"R&D intensity: {rdIntensity * 100:F1}% of revenue");
        }
        else
        {
            details.Add("No R&D data available");
        }

        var normalized = (int)Math.Round((score / 12.0) * 5.0);

        return new FinancialAnalysisResult(normalized, details, 5);
    }

    public FinancialAnalysisResult AnalyzeFinancialStrength(string ticker)
    {
        throw new NotImplementedException();
    }

    public FinancialAnalysisResult AnalyzeValuation(string ticker)
    {
        throw new NotImplementedException();
    }

    public TradeSignal GenerateOutput(string ticker)
    {
        var disruptive = AnalyzeEarningsStability(ticker);
        var innovation = AnalyzeFinancialStrength(ticker);
        var valuation = AnalyzeValuation(ticker);

        var analysisData = new
        {
            Disruptive = disruptive,
            Innovation = innovation,
            Valuation = valuation,
            TotalScore = disruptive.Score + innovation.Score + valuation.Score,
            MaxScore = disruptive.MaxScore + innovation.MaxScore + valuation.MaxScore
        };

        var systemMessage = """
            You are a Cathie Wood AI agent, making investment decisions using her principles:

            1. Seek companies leveraging disruptive innovation.
            2. Emphasize exponential growth potential, large TAM.
            3. Focus on technology, healthcare, or other future-facing sectors.
            4. Consider multi-year time horizons for potential breakthroughs.
            5. Accept higher volatility in pursuit of high returns.
            6. Evaluate management's vision and ability to invest in R&D.

            Rules:
            - Identify disruptive or breakthrough technology.
            - Evaluate strong potential for multi-year revenue growth.
            - Check if the company can scale effectively in a large market.
            - Use a growth-biased valuation approach.
            - Provide a data-driven recommendation (bullish, bearish, or neutral).
        """;

        var humanMessage =
            "Based on the following analysis, create a Cathie Wood-style investment signal.\n\n" +
            $"Analysis Data for {ticker}:\n" +
            $"{JsonSerializer.Serialize(analysisData, new JsonSerializerOptions { WriteIndented = true })}\n\n" +
            "Return the trading signal in this JSON format:\n" +
            "{\n" +
            "  \"signal\": \"bullish/bearish/neutral\",\n" +
            "  \"confidence\": float (0-100),\n" +
            "  \"reasoning\": \"string\"\n" +
            "}";

        var payload = JsonSerializer.Serialize(new
        {
            model = _state.ModelName,
            messages = new[]
            {
                new { role = "system", content = systemMessage },
                new { role = "user", content = humanMessage }
            },
            temperature = 0.2
        });

        if (_chatter.TryPost("/v1/chat/completions", payload, out var response))
        {
            try
            {
                var result = JsonSerializer.Deserialize<OpenAiResponse>(response);
                var content = result?.Choices?.FirstOrDefault()?.Message?.Content;

                if (!string.IsNullOrWhiteSpace(content))
                {
                    var signalJson = JsonSerializer.Deserialize<CathieWoodSignalJson>(content);

                    return new TradeSignal
                    {
                        Ticker = ticker,
                        Signal = signalJson?.Signal ?? "neutral",
                        Confidence = (decimal)(signalJson?.Confidence ?? 0),
                        Reasoning = signalJson?.Reasoning ?? "No reasoning provided"
                    };
                }
            }
            catch (Exception ex)
            {
                // Log and fall back
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, $"Failed to parse LLM response for {ticker}");
            }
        }

        // Default fallback
        return new TradeSignal
        {
            Ticker = ticker,
            Signal = "neutral",
            Confidence = 0,
            Reasoning = "Error in analysis, defaulting to neutral"
        };
    }

    private class OpenAiResponse
    {
        public List<Choice> Choices { get; set; }

        public class Choice
        {
            public MessageContent Message { get; set; }
        }

        public class MessageContent
        {
            public string Content { get; set; }
        }
    }

    private class CathieWoodSignalJson
    {
        public string Signal { get; set; }
        public double Confidence { get; set; }
        public string Reasoning { get; set; }
    }
}

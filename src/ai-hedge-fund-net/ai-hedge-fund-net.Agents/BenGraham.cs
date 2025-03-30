using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;
using System.Text.Json;

namespace ai_hedge_fund_net.Agents;

public class BenGraham : ITradingAgent
{
    public string Name => nameof(BenGraham);
    private readonly TradingWorkflowState _tradingWorkflowState;
    private readonly IChatter _chatter;

    public BenGraham(TradingWorkflowState tradingWorkflowState, IChatter chatter)
    {
        _tradingWorkflowState = tradingWorkflowState;
        _chatter = chatter;
    }

    public FinancialAnalysisResult AnalyzeEarningsStability(string ticker)
    {
        // Graham wants at least several years of consistently positive earnings(ideally 5 +).
        // We'll check:
        // 1.Number of years with positive EPS.
        // 2.Growth in EPS from first to last period.

        var results = new FinancialAnalysisResult();
        results.SetScore(0);

        var epsValues = _tradingWorkflowState.FinancialLineItems[ticker]
            .Where(item => item.Extras.ContainsKey("EarningsPerShare"))
            .Select(item => item.Extras["EarningsPerShare"])
            .ToList();

        if (epsValues.Count < 2)
        {
            results.AddDetail("Not enough multi-year EPS data.");
            return results;
        }

        int positiveEpsYears = epsValues.Count(eps => eps > 0);
        if (positiveEpsYears == epsValues.Count)
        {
            results.IncreaseScore(3);
            results.AddDetail("EPS was positive in all available periods.");
        }
        else if (positiveEpsYears >= epsValues.Count * 0.8)
        {
            results.IncreaseScore(2);
            results.AddDetail("EPS was positive in most periods.");
        }
        else
        {
            results.AddDetail("EPS was negative in multiple periods.");
        }

        if (epsValues.Last() > epsValues.First())
        {
            results.IncreaseScore(1);
            results.AddDetail("EPS grew from earliest to latest period.");
        }
        else
        {
            results.AddDetail("EPS did not grow from earliest to latest period.");
        }

        return results;
    }

    public FinancialAnalysisResult AnalyzeFinancialStrength(string ticker)
    {
        // Graham checks liquidity(current ratio >= 2), manageable debt,
        // and dividend record(preferably some history of dividends).

        var results = new FinancialAnalysisResult();
        results.SetScore(0);

        if (!_tradingWorkflowState.FinancialLineItems.TryGetValue(ticker, out var items) ||
            !TryGetLatestCompleteLineItem(items, out var latestItem))
        {
            results.AddDetail("No complete data for financial strength analysis");
            return results;
        }

        var extras = latestItem.Extras;

        decimal totalAssets = extras["TotalAssets"];
        decimal totalLiabilities = extras["TotalLiabilities"];
        decimal currentAssets = extras["TotalCurrentAssets"];
        decimal currentLiabilities = extras["TotalCurrentLiabilities"];

        if (currentLiabilities > 0)
        {
            var currentRatio = currentAssets / currentLiabilities;
            if (currentRatio >= 2.0m)
            {
                results.IncreaseScore(2);
                results.AddDetail($"Current ratio = {currentRatio:F2} (>=2.0: solid).");
            }
            else if (currentRatio >= 1.5m)
            {
                results.IncreaseScore(1);
                results.AddDetail($"Current ratio = {currentRatio:F2} (moderately strong).");
            }
            else
            {
                results.AddDetail($"Current ratio = {currentRatio:F2} (<1.5: weaker liquidity).");
            }
        }
        else
        {
            results.AddDetail("Cannot compute current ratio (missing or zero current liabilities). ");
        }

        if (totalAssets > 0)
        {
            decimal debtRatio = totalLiabilities / totalAssets;
            if (debtRatio < 0.5m)
            {
                results.IncreaseScore(2);
                results.AddDetail($"Debt ratio = {debtRatio:F2}, under 0.50 (conservative).");
            }
            else if (debtRatio < 0.8m)
            {
                results.IncreaseScore(1);
                results.AddDetail($"Debt ratio = {debtRatio:F2}, somewhat high but could be acceptable.");
            }
            else
            {
                results.AddDetail($"Debt ratio = {debtRatio:F2}, quite high by Graham standards.");
            }
        }
        else
        {
            results.AddDetail("Cannot compute debt ratio (missing total assets).");
        }

        return results;
    }

    private static bool TryGetLatestCompleteLineItem(IEnumerable<FinancialLineItem> items, out FinancialLineItem? completeItem)
    {
        foreach (var item in items.Reverse())
        {
            var extras = item.Extras;
            if (extras.ContainsKey("TotalAssets") &&
                extras.ContainsKey("TotalLiabilities"))
            {
                completeItem = item;
                return true;
            }
        }

        completeItem = null;
        return false;
    }

    public FinancialAnalysisResult AnalyzeValuation(string ticker)
    {
        // Core Graham approach to valuation:
        // 1.Net - Net Check: (Current Assets - Total Liabilities) vs.Market Cap
        // 2.Graham Number: sqrt(22.5 * EPS * Book Value per Share)
        // 3.Compare per - share price to Graham Number => margin of safety

        var results = new FinancialAnalysisResult();
        results.SetScore(0);

        if (!_tradingWorkflowState.FinancialLineItems.TryGetValue(ticker, out var items) ||
            !TryGetLatestCompleteLineItem(items, out var latest))
        {
            results.AddDetail("No complete data for financial strength analysis");
            return results;
        }

        if (latest == null || _tradingWorkflowState.FinancialMetrics[ticker].Last().MarketCap <= 0)
        {
            results.AddDetail("Insufficient data to perform valuation.");
            return results;
        }

        decimal netCurrentAssetValue = (latest.Extras["TotalAssets"] ?? 0) - (latest.Extras["TotalLiabilities"] ?? 0);
        if (netCurrentAssetValue > _tradingWorkflowState.FinancialMetrics[ticker].Last().MarketCap)
        {
            results.IncreaseScore(4);
            results.AddDetail("Net-Net: NCAV > Market Cap (classic Graham deep value).");
        }
        else if (netCurrentAssetValue >= _tradingWorkflowState.FinancialMetrics[ticker].Last().MarketCap * 0.67m)
        {
            results.IncreaseScore(2);
            results.AddDetail("NCAV Per Share >= 2/3 of Price Per Share (moderate net-net discount).");
        }

        return results;
    }

    public TradeSignal GenerateOutput(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return new TradeSignal
            {
                Signal = "neutral",
                Confidence = 0,
                Reasoning = "No ticker provided."
            };
        }

        if (!_tradingWorkflowState.FinancialMetrics.Any())
        {
            return new TradeSignal
            {
                Signal = "neutral",
                Confidence = 0,
                Reasoning = "No metrics provided."
            };
        }

        if (!_tradingWorkflowState.FinancialLineItems.Any())
        {
            return new TradeSignal
            {
                Signal = "neutral",
                Confidence = 0,
                Reasoning = "No financial data provided."
            };
        }

        //var ticker = tickers[0]; // Assuming single ticker processing, adjust if needed.
        var analysisData = new Dictionary<string, object>
        {
            { "FinancialMetrics", _tradingWorkflowState.FinancialMetrics },
            { "FinancialLineItems", _tradingWorkflowState.FinancialLineItems },
            { "StartDate", _tradingWorkflowState.StartDate },
            { "EndDate", _tradingWorkflowState.EndDate },
            { "InitialCash", _tradingWorkflowState.InitialCash },
            { "MarginRequirement", _tradingWorkflowState.MarginRequirement },
            { "Portfolio", _tradingWorkflowState.Portfolio }
        };

        var systemMessage = @"You are a Benjamin Graham AI agent, making investment decisions using his principles:
            1. Insist on a margin of safety by buying below intrinsic value (e.g., using Graham Number, net-net).
            2. Emphasize the company's financial strength (low leverage, ample current assets).
            3. Prefer stable earnings over multiple years.
            4. Consider dividend record for extra safety.
            5. Avoid speculative or high-growth assumptions; focus on proven metrics.

            Return a rational recommendation: bullish, bearish, or neutral, with a confidence level (0-100) and concise reasoning.";

        var userMessage = @$"Based on the following analysis, create a Graham-style investment signal:

            Analysis Data for {ticker}:
            {JsonSerializer.Serialize(analysisData, new JsonSerializerOptions { WriteIndented = true })}

            Return JSON exactly in this format:
            {{
              ""signal"": ""bullish"" or ""bearish"" or ""neutral"",
              ""confidence"": float (0-100),
              ""reasoning"": ""string""
            }}";

        var payload = new
        {
            model = _tradingWorkflowState.ModelName,
            messages = new[]
            {
                new { role = "system", content = systemMessage },
                new { role = "user", content = userMessage }
            },
            temperature = 0.2
        };

        var requestContent = JsonSerializer.Serialize(payload);

        if (!_chatter.TryPost("chat/completions", requestContent, out string response))
        {
            return new TradeSignal
            {
                Signal = "neutral",
                Confidence = 0,
                Reasoning = $"Error generating analysis ({response}). Defaulting to neutral."
            };
        }

        using var doc = JsonDocument.Parse(response);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        if (string.IsNullOrEmpty(content))
        {
            return new TradeSignal
            {
                Signal = "neutral",
                Confidence = 0,
                Reasoning = "Error processing analysis; defaulting to neutral."
            };
        }

        var parsedResult = JsonSerializer.Deserialize<TradeSignal>(content);
        return parsedResult ?? new TradeSignal
        {
            Signal = "neutral",
            Confidence = 0,
            Reasoning = "Error parsing response; defaulting to neutral."
        };
    }
}
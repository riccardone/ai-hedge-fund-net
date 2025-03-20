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

    public IDictionary<string, IDictionary<string, IEnumerable<string>>> AnalyzeEarningsStability()
    {
        // Graham wants at least several years of consistently positive earnings(ideally 5 +).
        // We'll check:
        // 1.Number of years with positive EPS.
        // 2.Growth in EPS from first to last period.

        var results = new Dictionary<string, IDictionary<string, IEnumerable<string>>>();
        foreach (var ticker in _tradingWorkflowState.Tickers)
        {
            var details = new List<string>();
            int score = 0;

            var epsValues = _tradingWorkflowState.FinancialLineItems[ticker]
                .Where(item => item.Extras.ContainsKey("EarningsPerShare"))
                .Select(item => item.Extras["EarningsPerShare"])
                .ToList();

            if (epsValues.Count < 2)
            {
                details.Add("Not enough multi-year EPS data.");
                results.Add(ticker, new Dictionary<string, IEnumerable<string>> { { "Score", new[] { score.ToString() } }, { "Details", details } });
                continue;
            }

            int positiveEpsYears = epsValues.Count(eps => eps > 0);
            if (positiveEpsYears == epsValues.Count)
            {
                score += 3;
                details.Add("EPS was positive in all available periods.");
            }
            else if (positiveEpsYears >= epsValues.Count * 0.8)
            {
                score += 2;
                details.Add("EPS was positive in most periods.");
            }
            else
            {
                details.Add("EPS was negative in multiple periods.");
            }

            if (epsValues.Last() > epsValues.First())
            {
                score += 1;
                details.Add("EPS grew from earliest to latest period.");
            }
            else
            {
                details.Add("EPS did not grow from earliest to latest period.");
            }
            results.Add(ticker, new Dictionary<string, IEnumerable<string>> { { "Score", new[] { score.ToString() } }, { "Details", details } });
        }

        return results;
    }

    public IDictionary<string, FinancialStrength> AnalyzeFinancialStrength()
    {
        // Graham checks liquidity(current ratio >= 2), manageable debt,
        // and dividend record(preferably some history of dividends).

        var details = new List<string>();
        int score = 0;

        var results = new Dictionary<string, FinancialStrength>();

        foreach (var ticker in _tradingWorkflowState.Tickers)
        {
            var latestItem = _tradingWorkflowState.FinancialLineItems[ticker].LastOrDefault();
            if (latestItem == null)
            {
                details.Add("No data for financial strength analysis");
                return new Dictionary<string, FinancialStrength>
                {
                    { ticker, new FinancialStrength(score, details) }
                };
            }

            decimal totalAssets = latestItem.Extras["TotalAssets"] ?? 0;
            decimal totalLiabilities = latestItem.Extras["TotalLiabilities"] ?? 0;
            decimal currentAssets = latestItem.Extras["CurrentAssets"] ?? 0;
            decimal currentLiabilities = latestItem.Extras["CurrentLiabilities"] ?? 0;

            if (currentLiabilities > 0)
            {
                decimal currentRatio = currentAssets / currentLiabilities;
                if (currentRatio >= 2.0m)
                {
                    score += 2;
                    details.Add($"Current ratio = {currentRatio:F2} (>=2.0: solid).");
                }
                else if (currentRatio >= 1.5m)
                {
                    score += 1;
                    details.Add($"Current ratio = {currentRatio:F2} (moderately strong).");
                }
                else
                {
                    details.Add($"Current ratio = {currentRatio:F2} (<1.5: weaker liquidity).");
                }
            }
            else
            {
                details.Add("Cannot compute current ratio (missing or zero current liabilities). ");
            }

            if (totalAssets > 0)
            {
                decimal debtRatio = totalLiabilities / totalAssets;
                if (debtRatio < 0.5m)
                {
                    score += 2;
                    details.Add($"Debt ratio = {debtRatio:F2}, under 0.50 (conservative).");
                }
                else if (debtRatio < 0.8m)
                {
                    score += 1;
                    details.Add($"Debt ratio = {debtRatio:F2}, somewhat high but could be acceptable.");
                }
                else
                {
                    details.Add($"Debt ratio = {debtRatio:F2}, quite high by Graham standards.");
                }
            }
            else
            {
                details.Add("Cannot compute debt ratio (missing total assets).");
            }

            results.Add(ticker, new FinancialStrength(score, details));
        }

        return results;
    }

    public IDictionary<string, IEnumerable<string>> AnalyzeValuation()
    {
        // Core Graham approach to valuation:
        // 1.Net - Net Check: (Current Assets - Total Liabilities) vs.Market Cap
        // 2.Graham Number: sqrt(22.5 * EPS * Book Value per Share)
        // 3.Compare per - share price to Graham Number => margin of safety

        var details = new List<string>();
        int score = 0;
        foreach (var ticker in _tradingWorkflowState.Tickers)
        {
            var latest = _tradingWorkflowState.FinancialLineItems[ticker].LastOrDefault();
            if (latest == null || _tradingWorkflowState.FinancialMetrics[ticker].MarketCap <= 0)
            {
                details.Add("Insufficient data to perform valuation.");
                return new Dictionary<string, IEnumerable<string>> { { "Score", new[] { score.ToString() } }, { "Details", details } };
            }

            decimal netCurrentAssetValue = (latest.Extras["CurrentAssets"] ?? 0) - (latest.Extras["TotalLiabilities"] ?? 0);
            if (netCurrentAssetValue > _tradingWorkflowState.FinancialMetrics[ticker].MarketCap)
            {
                score += 4;
                details.Add("Net-Net: NCAV > Market Cap (classic Graham deep value).");
            }
            else if (netCurrentAssetValue >= _tradingWorkflowState.FinancialMetrics[ticker].MarketCap * 0.67m)
            {
                score += 2;
                details.Add("NCAV Per Share >= 2/3 of Price Per Share (moderate net-net discount).");
            }
        }
        

        return new Dictionary<string, IEnumerable<string>> { { "Score", new[] { score.ToString() } }, { "Details", details } };
    }

    public async Task<TradeSignal> GenerateOutputAsync()
    {
        var tickers = _tradingWorkflowState.Tickers;
        if (tickers == null || tickers.Count == 0)
        {
            return new TradeSignal
            {
                Signal = "neutral",
                Confidence = 0,
                Reasoning = "No tickers provided."
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

        var ticker = tickers[0]; // Assuming single ticker processing, adjust if needed.
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

        if(!_chatter.TryPost("chat/completions", requestContent, out string response))
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
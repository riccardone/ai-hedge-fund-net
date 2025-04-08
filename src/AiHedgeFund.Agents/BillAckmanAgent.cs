using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using NLog;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AiHedgeFund.Agents;

/// <summary>
/// Analyzes stocks using Bill Ackman's investing principles and LLM reasoning.
/// Fetches multiple periods of data so we can analyze long-term trends.
/// </summary>
public class BillAckmanAgent
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IHttpLib _chatter;

    public BillAckmanAgent(IHttpLib chatter)
    {
        _chatter = chatter;
    }

    public IEnumerable<TradeSignal> Run(TradingWorkflowState state)
    {
        var signals = new List<TradeSignal>();
        if (!state.Tickers.Any())
        {
            Logger.Warn("No ticker provided.");
            return signals;
        }
        
        foreach (var ticker in state.Tickers)
        {
            Logger.Info("[BillAckman] Starting analysis for {0}", ticker);

            if (!state.FinancialMetrics.TryGetValue(ticker, out var metrics)
                || !state.FinancialLineItems.TryGetValue(ticker, out var lineItems))
            {
                Logger.Warn($"Missing data for {ticker}");
                continue;
            }

            var marketCap = metrics.OrderByDescending(m => m.Period).FirstOrDefault()?.MarketCap;
            if (marketCap == null)
            {
                Logger.Warn($"No market cap for {ticker}");
                continue;
            }

            var businessQuality = AnalyzeBusinessQuality(metrics, lineItems);
            var financialDiscipline = AnalyzeFinancialDiscipline(metrics, lineItems);
            var valuation = AnalyzeValuation(metrics, marketCap);

            Logger.Info("{0} Business Quality: {1}", ticker, string.Join("; ", businessQuality.Details));
            Logger.Info("{0} Financial Discipline: {1}", ticker, string.Join("; ", financialDiscipline.Details));
            Logger.Info("{0} Valuation: {1}", ticker, string.Join("; ", valuation.Details));

            var totalScore = businessQuality.Score + financialDiscipline.Score + valuation.Score;
            const int maxScore = 15;

            if (TryGenerateOutput(state, ticker, businessQuality, financialDiscipline, valuation, totalScore, maxScore, out TradeSignal tradeSignal))
            {
                signals.Add(tradeSignal);
            }
            else
            {
                var signal = GenerateOutputWithoutLLM(totalScore, maxScore, businessQuality, financialDiscipline, valuation, out var reasoning, out var confidence);
                signals.Add(new TradeSignal(ticker, signal, confidence, reasoning));
            }
        }

        return signals;
    }

    private static FinancialAnalysisResult AnalyzeBusinessQuality(IEnumerable<FinancialMetrics> metrics, IEnumerable<FinancialLineItem> lineItems)
    {
        var result = new FinancialAnalysisResult();
        const int maxScore = 7;

        if (!metrics.Any() || !lineItems.Any())
        {
            result.AddDetail("Insufficient data to analyze business quality");
            result.SetScore(0);
            return new FinancialAnalysisResult(0, result.Details, maxScore);
        }

        var ordered = metrics.OrderBy(m => m.Period).ToList();

        // Revenue growth
        var revenues = ordered.Where(m => m.TotalRevenue.HasValue).Select(li => li.TotalRevenue).ToList();

        if (revenues.Count >= 2)
        {
            var initial = revenues[0];
            var final = revenues[^1];
            if (final > initial)
            {
                var growthRate = (final - initial) / Math.Abs(initial.Value);
                if (growthRate > 0.5m)
                {
                    result.IncreaseScore(2);
                    result.AddDetail($"Revenue grew by {growthRate:P1} over the period.");
                }
                else
                {
                    result.IncreaseScore(1);
                    result.AddDetail($"Revenue grew by {growthRate:P1} over the period.");
                }
            }
            else
            {
                result.AddDetail("Revenue did not grow significantly.");
            }
        }
        else
        {
            result.AddDetail("Not enough revenue data.");
        }

        // Operating margin
        var opMargins = ordered.Select(li => li.OperatingMargin).Where(v => v.HasValue).Select(v => v.Value).ToList();

        if (opMargins.Count > 0)
        {
            int above15 = opMargins.Count(m => m > 0.15m);
            if (above15 >= (opMargins.Count / 2 + 1))
            {
                result.IncreaseScore(2);
                result.AddDetail("Operating margin exceeded 15% in most periods.");
            }
            else
            {
                result.AddDetail("Operating margin not consistently above 15%.");
            }
        }
        else
        {
            result.AddDetail("No operating margin data available.");
        }

        // Free cash flow
        var fcfs = ordered.Select(li => li.FreeCashFlow).ToList();

        if (fcfs.Count > 0)
        {
            int positiveFcf = fcfs.Count(f => f > 0);
            if (positiveFcf >= (fcfs.Count / 2 + 1))
            {
                result.IncreaseScore(1);
                result.AddDetail("Positive free cash flow in majority of periods.");
            }
            else
            {
                result.AddDetail("Free cash flow not consistently positive.");
            }
        }
        else
        {
            result.AddDetail("No free cash flow data available.");
        }

        // Return on equity
        var latestMetrics = metrics.First();
        if (latestMetrics.ReturnOnEquity.HasValue)
        {
            if (latestMetrics.ReturnOnEquity > 0.15m)
            {
                result.IncreaseScore(2);
                result.AddDetail($"ROE is {latestMetrics.ReturnOnEquity:P1}, indicating possible moat.");
            }
            else
            {
                result.AddDetail($"ROE is {latestMetrics.ReturnOnEquity:P1}, not indicative of a strong moat.");
            }
        }
        else
        {
            result.AddDetail("No ROE data available.");
        }

        return new FinancialAnalysisResult(result.Score, result.Details, maxScore);
    }

    private static FinancialAnalysisResult AnalyzeFinancialDiscipline(IEnumerable<FinancialMetrics> metrics, IEnumerable<FinancialLineItem> lineItems)
    {
        var result = new FinancialAnalysisResult();
        const int maxScore = 4;

        if (!metrics.Any() || !lineItems.Any())
        {
            result.AddDetail("Insufficient data to analyze financial discipline");
            return new FinancialAnalysisResult(0, result.Details, maxScore);
        }

        var orderedMetrics = metrics.OrderBy(m => m.Period).ToList();

        // --- Debt-to-Equity ---
        var debtToEquity = orderedMetrics
            .Where(m => m.DebtToEquity.HasValue && m.DebtToEquity.Value >= 0 && m.DebtToEquity.Value < 10_000) // ignore extreme or invalid values
            .Select(m => m.DebtToEquity.Value)
            .ToList();

        if (debtToEquity.Count > 0)
        {
            int belowOneCount = debtToEquity.Count(r => r < 1.0m);
            if (belowOneCount >= (debtToEquity.Count / 2 + 1))
            {
                result.IncreaseScore(2);
                result.AddDetail("Debt-to-equity < 1.0 in most periods.");
            }
            else
            {
                result.AddDetail("High debt-to-equity in many periods.");
            }
        }
        else
        {
            // --- Fallback: Liabilities to Assets ---
            var liabToAssets = lineItems
                .Select(li =>
                {
                    decimal? liabilities = null;
                    decimal? assets = null;
                    if (li.Extras.TryGetValue("total_liabilities", out var l) && l is decimal dLiab)
                        liabilities = dLiab;
                    if (li.Extras.TryGetValue("total_assets", out var a) && a is decimal dAssets)
                        assets = dAssets;
                    if (liabilities.HasValue && assets.HasValue && assets.Value > 0)
                        return liabilities.Value / assets.Value;
                    return (decimal?)null;
                })
                .Where(v => v.HasValue).Select(v => v.Value).ToList();

            if (liabToAssets.Count > 0)
            {
                int below50pctCount = liabToAssets.Count(r => r < 0.5m);
                if (below50pctCount >= (liabToAssets.Count / 2 + 1))
                {
                    result.IncreaseScore(2);
                    result.AddDetail("Liabilities-to-assets < 50% in most periods.");
                }
                else
                {
                    result.AddDetail("High liabilities-to-assets in many periods.");
                }
            }
            else
            {
                result.AddDetail("No leverage ratio data available.");
            }
        }

        // --- Dividends ---
        var dividends = orderedMetrics.Select(li => li.DividendsAndOtherCashDistributions).ToList();

        if (dividends.Count > 0)
        {
            int paidDividendsCount = dividends.Count(d => d < 0); // negative = cash outflow
            if (paidDividendsCount >= (dividends.Count / 2 + 1))
            {
                result.IncreaseScore(1);
                result.AddDetail("Company returned capital via dividends in most periods.");
            }
            else
            {
                result.AddDetail("Dividends not consistently paid.");
            }
        }
        else
        {
            result.AddDetail("No dividend data available.");
        }

        // --- Share Buybacks (Outstanding Shares Decreasing) ---
        var shares = orderedMetrics
            .Select(li => li.CommonStockSharesOutstanding)
            .Where(v => v.HasValue)
            .Select(v => v.Value)
            .ToList();

        if (shares.Count >= 2)
        {
            if (shares[^1] < shares[0])
            {
                result.IncreaseScore(1);
                result.AddDetail("Outstanding shares decreased over time (buybacks).");
            }
            else
            {
                result.AddDetail("No reduction in outstanding shares.");
            }
        }
        else
        {
            result.AddDetail("No multi-period share count data.");
        }

        return new FinancialAnalysisResult(result.Score, result.Details, maxScore);
    }

    private class AnalysisResult
    {
        public decimal Score { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    private static FinancialAnalysisResult AnalyzeValuation(IEnumerable<FinancialMetrics> metrics, decimal? marketCap)
    {
        var result = new FinancialAnalysisResult();
        const int maxScore = 3;

        if (!metrics.Any() || marketCap == null || marketCap <= 0)
        {
            result.AddDetail("Insufficient data to perform valuation");
            return new FinancialAnalysisResult(0, result.Details, maxScore);
        }

        var fcf = metrics.Select(li => li.OperatingCashFlow).ToList();
        if (fcf.Count == 0 || fcf[^1] <= 0)
        {
            return new FinancialAnalysisResult(0,
                new[] { $"No positive FCF for valuation; FCF = {fcf.LastOrDefault():N2}" }, maxScore);
        }

        var baseFcf = fcf[^1];
        const decimal growthRate = 0.06m;
        const decimal discountRate = 0.10m;
        const int terminalMultiple = 15;
        const int projectionYears = 5;

        decimal presentValue = 0;
        for (int year = 1; year <= projectionYears; year++)
        {
            decimal futureFcf = baseFcf * (decimal)Math.Pow((double)(1 + growthRate), year);
            presentValue += futureFcf / (decimal)Math.Pow((double)(1 + discountRate), year);
        }

        decimal terminalValue = (baseFcf * (decimal)Math.Pow((double)(1 + growthRate), projectionYears) * terminalMultiple)
                                / (decimal)Math.Pow((double)(1 + discountRate), projectionYears);

        decimal intrinsicValue = presentValue + terminalValue;
        decimal marginOfSafety = (intrinsicValue - marketCap.Value) / marketCap.Value;

        if (marginOfSafety > 0.3m)
        {
            result.IncreaseScore(3);
        }
        else if (marginOfSafety > 0.1m)
        {
            result.IncreaseScore(1);
        }

        result.AddDetail($"Intrinsic value: ~{intrinsicValue:N0}");
        result.AddDetail($"Market cap: ~{marketCap:N0}");
        result.AddDetail($"Margin of safety: {marginOfSafety:P1}");
        // debug
        result.AddDetail($"Base FCF: {baseFcf:N0}");
        result.AddDetail($"Present Value (5y): {presentValue:N0}");
        result.AddDetail($"Terminal Value: {terminalValue:N0}");
        result.AddDetail($"Intrinsic Value: {intrinsicValue:N0}");


        return new FinancialAnalysisResult(result.Score, result.Details, maxScore);
    }

    private string GenerateOutputWithoutLLM(int totalScore, int maxScore, FinancialAnalysisResult businessQuality,
        FinancialAnalysisResult financialDiscipline, FinancialAnalysisResult valuation, out string reasoning,
        out decimal confidence)
    {
        string signal;
        var details = new List<string>();

        if (totalScore >= 0.7 * maxScore)
        {
            signal = "bullish";
            confidence = 90;
        }
        else if (totalScore <= 0.3 * maxScore)
        {
            signal = "bearish";
            confidence = 75;
        }
        else
        {
            signal = "neutral";
            confidence = 60;
        }

        details.Add($"Total score: {totalScore}/{maxScore}");
        details.Add($"Business Quality: {businessQuality.Score}/{businessQuality.MaxScore}");
        details.Add($"Financial Discipline: {financialDiscipline.Score}/{financialDiscipline.MaxScore}");
        details.Add($"Valuation: {valuation.Score}/{valuation.MaxScore}");

        reasoning = string.Join("; ", details);
        return signal;
    }


    private bool TryGenerateOutput(TradingWorkflowState state, string ticker, FinancialAnalysisResult businessQuality,
        FinancialAnalysisResult financialDiscipline, FinancialAnalysisResult valuation, int totalScore, int maxScore,
        out TradeSignal tradeSignal)
    {
        tradeSignal = null;

        var systemMessage = """
        You are a Bill Ackman AI agent, making investment decisions using his principles:

        1. Seek high-quality businesses with durable competitive advantages (moats).
        2. Prioritize consistent free cash flow and growth potential.
        3. Advocate for strong financial discipline (reasonable leverage, efficient capital allocation).
        4. Valuation matters: target intrinsic value and margin of safety.
        5. Invest with high conviction in a concentrated portfolio for the long term.
        6. Potential activist approach if management or operational improvements can unlock value.

        Rules:
        - Evaluate brand strength, market position, or other moats.
        - Check free cash flow generation, stable or growing earnings.
        - Analyze balance sheet health (reasonable debt, good ROE).
        - Buy at a discount to intrinsic value; higher discount => stronger conviction.
        - Engage if management is suboptimal or if there's a path for strategic improvements.
        - Provide a rational, data-driven recommendation (bullish, bearish, or neutral).
        """;

        var analysisData = new
        {
            Ticker = ticker,
            TotalScore = totalScore,
            MaxScore = maxScore,
            BusinessQuality = businessQuality.Details,
            FinancialDiscipline = financialDiscipline.Details,
            Valuation = valuation.Details
        };

        var humanMessage = @$"
        Based on the following analysis, create an Ackman-style investment signal.

        Analysis Data for {ticker}:
        {JsonSerializer.Serialize(analysisData, new JsonSerializerOptions { WriteIndented = true })}

            Return JSON exactly in this format:
            {{
              ""signal"": ""bullish"" or ""bearish"" or ""neutral"",
              ""confidence"": float (0-100),
              ""reasoning"": ""string""
            }}
        ";

        var payload = new
        {
            model = state.ModelName,
            messages = new[]
            {
                new { role = "system", content = systemMessage },
                new { role = "user", content = humanMessage }
            },
            temperature = 0.2
        };

        var json = JsonSerializer.Serialize(payload);

        if (!_chatter.TryPost("chat/completions", json, out var response))
        {
            tradeSignal = new TradeSignal(ticker, "neutral", 0, "Error posting to LLM. Defaulting to neutral.");
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(response);
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content")
                .GetString();
            if (string.IsNullOrEmpty(content))
            {
                tradeSignal = new TradeSignal(ticker, "neutral", 0, "Empty response from LLM. Defaulting to neutral.");
                return false;
            }

            if (!TryExtractJson(content, out var cleanedJson))
            {
                tradeSignal = new TradeSignal(ticker, "neutral", 0, "Failed to extract valid JSON from LLM response.");
                return false;
            }

            var result = JsonSerializer.Deserialize<TradeSignal>(cleanedJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            result.SetTicker(ticker);
            tradeSignal = result;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to deserialize LLM output for {0}", ticker);
            tradeSignal = new TradeSignal(ticker, "neutral", 0, "Failed to parse LLM response. Defaulting to neutral.");
            return false;
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

        var match = Regex.Match(content, "\\{[\\s\\S]*?\\}");
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

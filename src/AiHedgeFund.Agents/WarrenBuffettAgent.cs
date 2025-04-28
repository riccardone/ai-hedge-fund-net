using AiHedgeFund.Agents.Services;
using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using Microsoft.Extensions.Logging;

namespace AiHedgeFund.Agents;

public class WarrenBuffettAgent
{
    private readonly ILogger<WarrenBuffettAgent> _logger;
    private readonly IHttpLib _httpLib;
    private readonly IValuationEngine _valuationEngine;

    public WarrenBuffettAgent(IHttpLib httpLib, IValuationEngine valuationEngine, ILogger<WarrenBuffettAgent> logger)
    {
        _httpLib = httpLib;
        _valuationEngine = valuationEngine;
        _logger = logger;
    }

    public void Run(TradingWorkflowState state)
    {
        if (!state.Tickers.Any())
        {
            _logger.LogWarning("No tickers provided.");
            return;
        }

        foreach (var ticker in state.Tickers)
        {
            _logger.LogDebug("[Warren Buffett] Starting analysis for {0}", ticker);

            if (!state.FinancialMetrics.TryGetValue(ticker, out var metrics) ||
                !state.FinancialLineItems.TryGetValue(ticker, out var lineItems))
            {
                _logger.LogWarning($"Missing financial data for {ticker}");
                continue;
            }

            var marketCap = metrics.MaxBy(m => m.Period)?.MarketCap;
            if (marketCap == null)
            {
                _logger.LogWarning($"No market cap for {ticker}");
                continue;
            }

            var fundamentals = Fundamentals(metrics); // max score: 7
            var consistency = Consistency(metrics, state.RiskLevel); // max score: 3
            ValuationSummary? valuationSummary = null;
            if(_valuationEngine.TryCalculateIntrinsicValue(metrics.MaxBy(m => m.ReportPeriod), state.RiskLevel, state.Prices[ticker].MinBy(p => p.Date)?.Close, out var summary))
            {
                valuationSummary = summary;
            }

            int totalScore = fundamentals.Score + consistency.Score;
            int maxScore = fundamentals.MaxScore + consistency.MaxScore;

            decimal? marginOfSafety = null;

            if (valuationSummary.IntrinsicValue.HasValue && marketCap.HasValue && marketCap > 0)
            {
                marginOfSafety = (valuationSummary.IntrinsicValue.Value * metrics.OrderByDescending(m => m.EndDate).First().OutstandingShares ?? 1) - marketCap.Value;
                marginOfSafety /= marketCap.Value;

                if (marginOfSafety > 0.3m)
                {
                    totalScore += 2;
                    maxScore += 2;
                }
            }

            if (TryGenerateOutput(ticker, fundamentals, consistency, valuationSummary, totalScore, maxScore,
                    out var tradeSignal))
                state.AddOrUpdateAgentReport<WarrenBuffettAgent>(tradeSignal,
                    new[]
                    {
                        fundamentals, consistency,
                        valuationSummary != null && valuationSummary.IntrinsicValue.HasValue
                            ? new FinancialAnalysisResult("IntrinsicValue", (int)valuationSummary.IntrinsicValue.Value,
                                new List<string>(), 10)
                            : null
                    });
            else
                _logger.LogError($"Error while generating signal for {ticker}");
        }
    }

    /// <summary>
    /// Analyze company fundamentals based on Buffett's criteria.
    /// ROE > 15% → +2 points
    /// Debt-to-Equity< 0.5 → +2 points
    /// Operating Margin> 15% → +2 points
    /// Current Ratio > 1.5 → +1 point
    /// Max Score: 7
    /// </summary>
    /// <param name="metrics"></param>
    /// <param name="lineItems"></param>
    /// <returns></returns>
    private FinancialAnalysisResult Fundamentals(IEnumerable<FinancialMetrics> metrics)
    {
        var latest = metrics.MaxBy(m => m.EndDate);
        if (latest == null)
            return new FinancialAnalysisResult(nameof(Fundamentals),0, new[] { "No recent financial metrics available." });

        var score = 0;
        var details = new List<string>();

        if (latest.ReturnOnEquity.HasValue)
        {
            if (latest.ReturnOnEquity > 0.15m)
            {
                score += 2;
                details.Add($"Strong ROE of {latest.ReturnOnEquity:P1}, reflecting efficient capital use");
            }
            else
            {
                details.Add($"Weak ROE of {latest.ReturnOnEquity:P1}");
            }
        }
        else
        {
            details.Add("ROE data not available");
        }

        if (latest.DebtToEquity.HasValue)
        {
            if (latest.DebtToEquity < 0.5m)
            {
                score += 2;
                details.Add("Conservative debt levels (D/E < 0.5)");
            }
            else
            {
                details.Add($"High debt-to-equity ratio of {latest.DebtToEquity:F2}");
            }
        }
        else
        {
            details.Add("Debt-to-equity data not available");
        }

        if (latest.OperatingMargin.HasValue)
        {
            if (latest.OperatingMargin > 0.15m)
            {
                score += 2;
                details.Add("Strong operating margins (> 15%)");
            }
            else
            {
                details.Add($"Weak operating margin of {latest.OperatingMargin:P1}");
            }
        }
        else
        {
            details.Add("Operating margin data not available");
        }

        if (latest.CurrentRatio.HasValue)
        {
            if (latest.CurrentRatio > 1.5m)
            {
                score += 1;
                details.Add("Good liquidity position (Current Ratio > 1.5)");
            }
            else
            {
                details.Add($"Weak liquidity with current ratio of {latest.CurrentRatio:F2}");
            }
        }
        else
        {
            details.Add("Current ratio data not available");
        }

        return new FinancialAnalysisResult(nameof(Fundamentals), score, details, maxScore: 7);
    }

    /// <summary>
    /// Analyze earnings consistency and growth.Use NetIncome from FinancialLineItem
    /// Require at least 4 periods
    /// Score +3 if earnings grow consistently
    /// Always calculate growth rate when possible
    /// </summary>
    /// <param name="metrics"></param>
    /// <param name="lineItems"></param>
    /// <param name="riskLevel"></param>
    /// <returns></returns>
    private FinancialAnalysisResult Consistency(IEnumerable<FinancialMetrics> metrics, RiskLevel riskLevel)
    {
        var ordered = metrics.OrderBy(m => m.EndDate).ToList(); // Oldest to newest
        var earnings = ordered
            .Where(m => m.NetIncome.HasValue)
            .Select(m => m.NetIncome.Value)
            .ToList();

        var details = new List<string>();
        int score = 0;
        int maxScore = 3;

        if (earnings.Count < 4)
        {
            details.Add("Insufficient earnings data for trend analysis (need 4+ periods).");
            return new FinancialAnalysisResult(nameof(Consistency), score, details, maxScore);
        }

        // Count growth periods
        int growthPeriods = 0;
        for (int i = 0; i < earnings.Count - 1; i++)
        {
            if (earnings[i] < earnings[i + 1])
                growthPeriods++;
        }

        int total = earnings.Count - 1;
        double growthRatio = (double)growthPeriods / total;

        var oldest = earnings.First();
        var latest = earnings.Last();
        var growthRate = oldest != 0 ? (latest - oldest) / Math.Abs(oldest) : 0;

        // Recalibrate scoring based on risk
        switch (riskLevel)
        {
            case RiskLevel.Low:
                if (growthRatio > 0.5)
                {
                    score = 3;
                    details.Add("Consistent earnings growth over the past periods.");
                }
                else
                {
                    details.Add("Inconsistent earnings growth pattern.");
                }
                break;

            case RiskLevel.Medium:
                if (growthRatio >= 0.4)
                    score = 2;
                else if (growthRatio >= 0.25)
                    score = 1;

                details.Add($"Earnings growth seen in {growthRatio:P0} of periods.");
                break;

            case RiskLevel.High:
                if (growthRate > 0.2m)
                    score = 3;
                else if (growthRate > 0)
                    score = 2;
                else if (growthRatio >= 0.25)
                    score = 1;

                details.Add($"Earnings growth rate of {growthRate:P1} across all periods.");
                break;
        }

        if (oldest != 0)
            details.Add($"Total earnings growth of {growthRate:P1} over {earnings.Count} periods.");

        return new FinancialAnalysisResult(nameof(Consistency), score, details, maxScore);
    }

    private bool TryGenerateOutput(string ticker, FinancialAnalysisResult fundamentals,
        FinancialAnalysisResult consistency, ValuationSummary valuationSummary, double totalScore, int maxScore,
        out TradeSignal tradeSignal)
    {
        tradeSignal = default!;

        var systemMessage =
            @"You are a Warren Buffett AI agent. Decide on investment signals based on Warren Buffett’s principles:

                Circle of Competence: Only invest in businesses you understand
                Margin of Safety: Buy well below intrinsic value
                Economic Moat: Prefer companies with lasting advantages
                Quality Management: Look for conservative, shareholder-oriented teams
                Financial Strength: Low debt, strong returns on equity
                Long-term Perspective: Invest in businesses, not just stocks

                Rules:
                - Buy only if margin of safety > 30%
                - Focus on owner earnings and intrinsic value
                - Prefer consistent earnings growth
                - Avoid high debt or poor management
                - Hold good businesses long term
                - Sell when fundamentals deteriorate or the valuation is too high";

        var analysisData = new
        {
            score = totalScore,
            max_score = maxScore,
            Fundamentals = fundamentals,
            Consistency = consistency,
            MarginOfSafety = valuationSummary.MarginOfSafety,
            IntrinsicValue = valuationSummary.IntrinsicValue,
            DiscountRate = valuationSummary.DiscountRate,
            GrowthRate = valuationSummary.GrowthRate,
            AcceptedRiskLevel = valuationSummary.RiskLevel,
            TerminalMultiple = valuationSummary.TerminalMultiple,
            ValuationBasis = valuationSummary.ValuationBasis
        };

        return LlmTradeSignalGenerator.TryGenerateSignal(
            _httpLib,
            endpoint: "chat/completions",
            ticker: ticker,
            systemMessage: systemMessage,
            analysisData: analysisData,
            agentName: "Warren Buffett",
            out tradeSignal
        );
    }
}

public class BuffettAnalysisResult : IAgentAnalysisResult
{
    public BuffettAnalysisResult(string fundamentals, string consistency, string intrinsicValue, string marginOfSafety)
    {
        Fundamentals = fundamentals;
        Consistency = consistency;
        IntrinsicValue = intrinsicValue;
        MarginOfSafety = marginOfSafety;
    }

    private string Fundamentals { get; }
    private string Consistency { get; }
    private string IntrinsicValue { get; }
    private string MarginOfSafety { get; }

    public List<(string Title, string Value)> ToSections()
    {
        return new List<(string, string)>
        {
            ("Fundamentals", Fundamentals),
            ("Consistency", Consistency),
            ("Intrinsic Value", IntrinsicValue),
            ("Margin of Safety", MarginOfSafety)
        };
    }
}
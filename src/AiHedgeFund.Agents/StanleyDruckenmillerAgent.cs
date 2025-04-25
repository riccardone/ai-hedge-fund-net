using AiHedgeFund.Agents.Services;
using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using NLog;

namespace AiHedgeFund.Agents;

public class StanleyDruckenmillerAgent
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IHttpLib _httpLib;

    public StanleyDruckenmillerAgent(IHttpLib httpLib)
    {
        _httpLib = httpLib;
    }

    public void Run(TradingWorkflowState state)
    {
        if (!state.Tickers.Any())
        {
            Logger.Warn("No ticker provided.");
            return;
        }

        foreach (var ticker in state.Tickers)
        {
            Logger.Debug("[StanleyDruckenmiller] Starting analysis for {0}", ticker);

            if (!state.FinancialMetrics.TryGetValue(ticker, out var metrics) ||
                !state.FinancialLineItems.TryGetValue(ticker, out var lineItems))
            {
                Logger.Warn($"Missing financial data for {ticker}");
                continue;
            }

            var marketCap = metrics.OrderByDescending(m => m.EndDate).FirstOrDefault()?.MarketCap;
            if (marketCap == null)
            {
                Logger.Warn($"No market cap for {ticker}");
                continue;
            }

            var growthMomentum = GrowthAndMomentum(metrics, lineItems, state.Prices[ticker]);
            var riskReward = RiskReward(metrics, lineItems, state.Prices[ticker]);
            var sentiment = Sentiment(state.CompanyNews[ticker]);
            var insiderActivity = AnalyzeInsiderActivity(metrics);
            var valuation = AnalyzeValuation(metrics, lineItems, marketCap);

            // Weighted total score as per Druckenmiller’s method
            var totalScore =
                growthMomentum.Score * 0.35 +
                riskReward.Score * 0.20 +
                valuation.Score * 0.20 +
                sentiment.Score * 0.15 +
                insiderActivity.Score * 0.10;

            const int maxScore = 10;

            if (TryGenerateOutput(ticker, growthMomentum, riskReward, valuation, sentiment, insiderActivity, totalScore, maxScore, out var tradeSignal))
                state.AddOrUpdateAgentReport<StanleyDruckenmillerAgent>(tradeSignal, new []{ growthMomentum, riskReward, sentiment, insiderActivity, valuation });
            else
                Logger.Error($"Error while running {nameof(StanleyDruckenmillerAgent)} for {ticker}");
        }
    }

    /// <summary>
    /// Evaluate:
    /// - Revenue Growth(YoY)
    ///    - EPS Growth(YoY)
    ///    - Price Momentum
    /// </summary>
    /// <param name="metrics"></param>
    /// <param name="lineItems"></param>
    /// <param name="prices"></param>
    /// <returns></returns>
    private FinancialAnalysisResult GrowthAndMomentum(IEnumerable<FinancialMetrics> metrics, IEnumerable<FinancialLineItem> lineItems, IEnumerable<Price> prices)
    {
        var details = new List<string>();
        double rawScore = 0;

        // Revenue Growth
        var revenues = lineItems?
            .Where(li => li?.Extras != null)
            .SelectMany(li => li.Extras.TryGetValue("TotalRevenue", out var r) && r is decimal d ? new[] { d } : Array.Empty<decimal>())
            .ToList() ?? new List<decimal>();

        if (revenues.Count >= 2)
        {
            var growth = (revenues[0] - revenues[^1]) / Math.Abs(revenues[^1]);
            if (growth > 0.30m) { rawScore += 3; details.Add($"Strong revenue growth: {growth:P1}"); }
            else if (growth > 0.15m) { rawScore += 2; details.Add($"Moderate revenue growth: {growth:P1}"); }
            else if (growth > 0.05m) { rawScore += 1; details.Add($"Slight revenue growth: {growth:P1}"); }
            else { details.Add($"Minimal/negative revenue growth: {growth:P1}"); }
        }
        else
        {
            details.Add("Not enough revenue data points for growth calculation.");
        }

        // EPS Growth
        var ordered = metrics.OrderBy(m => m.EndDate).ToList();
        var epsList = ordered.Where(e => e.EarningsPerShareGrowth.HasValue).Select(li => li.EarningsPerShareGrowth).ToList();
        if (epsList.Count() >= 2)
        {
            var growth = (epsList[0] - epsList[^1]) / Math.Abs(epsList[^1].Value);
            if (growth > 0.30m) { rawScore += 3; details.Add($"Strong EPS growth: {growth:P1}"); }
            else if (growth > 0.15m) { rawScore += 2; details.Add($"Moderate EPS growth: {growth:P1}"); }
            else if (growth > 0.05m) { rawScore += 1; details.Add($"Slight EPS growth: {growth:P1}"); }
            else { details.Add($"Minimal/negative EPS growth: {growth:P1}"); }
        }
        else
        {
            details.Add("Not enough EPS data points for growth calculation.");
        }

        // Price Momentum
        if (prices.Count() > 30)
        {
            var sorted = prices.OrderBy(p => p.Date).ToList();
            var start = sorted.First().Close;
            var end = sorted.Last().Close;
            if (start > 0)
            {
                var pctChange = (end - start) / start;
                if (pctChange > 0.50m) { rawScore += 3; details.Add($"Very strong price momentum: {pctChange:P1}"); }
                else if (pctChange > 0.20m) { rawScore += 2; details.Add($"Moderate price momentum: {pctChange:P1}"); }
                else if (pctChange > 0) { rawScore += 1; details.Add($"Slight positive momentum: {pctChange:P1}"); }
                else { details.Add($"Negative price momentum: {pctChange:P1}"); }
            }
            else
            {
                details.Add("Invalid start price (<= 0); can't compute momentum.");
            }
        }
        else
        {
            details.Add("Not enough recent price data for momentum analysis.");
        }

        var finalScore = (int)Math.Min(10, (rawScore / 9.0) * 10);
        return new FinancialAnalysisResult(nameof(GrowthAndMomentum), finalScore, details);
    }

    /// <summary>
    /// Assesses risk via:
    /// - Debt-to-Equity
    /// - Price Volatility
    /// Aims for strong upside with contained downside.
    /// </summary>
    /// <param name="metrics"></param>
    /// <param name="lineItems"></param>
    /// <param name="prices"></param>
    /// <returns></returns>
    private FinancialAnalysisResult RiskReward(IEnumerable<FinancialMetrics> metrics, IEnumerable<FinancialLineItem> lineItems, IEnumerable<Price> prices)
    {
        var details = new List<string>();
        double rawScore = 0;

        if (lineItems == null || !lineItems.Any() || prices == null || !prices.Any())
        {
            details.Add("Insufficient data for risk-reward analysis");
            return new FinancialAnalysisResult("RiskReward", 0, details);

        }

        // 1. Debt-to-Equity
        var ordered = metrics.OrderBy(m => m.EndDate).ToList();
        var debts = ordered.Where(t => t.TotalDebt.HasValue).Select(li => li.TotalDebt.Value).ToList();
        var equities = ordered.Where(t => t.TotalShareholderEquity.HasValue).Select(li => li.TotalShareholderEquity.Value).ToList();

        if (debts.Any() && equities.Any() && debts.Count == equities.Count)
        {
            var debt = debts[0];
            var equity = equities[0] == 0 ? 1e-9m : equities[0];
            var ratio = debt / equity;

            if (ratio < 0.3m) { rawScore += 3; details.Add($"Low debt-to-equity: {ratio:F2}"); }
            else if (ratio < 0.7m) { rawScore += 2; details.Add($"Moderate debt-to-equity: {ratio:F2}"); }
            else if (ratio < 1.5m) { rawScore += 1; details.Add($"Somewhat high debt-to-equity: {ratio:F2}"); }
            else { details.Add($"High debt-to-equity: {ratio:F2}"); }
        }
        else
        {
            details.Add("No consistent debt/equity data available.");
        }

        // 2. Price Volatility
        if (prices.Count() > 10)
        {
            var closes = prices.OrderBy(p => p.Date).Select(p => p.Close).Where(c => c > 0).Select(c => c).ToList();

            if (closes.Count > 10)
            {
                var dailyReturns = new List<decimal>();
                for (int i = 1; i < closes.Count; i++)
                {
                    if (closes[i - 1] > 0)
                    {
                        dailyReturns.Add((closes[i] - closes[i - 1]) / closes[i - 1]);
                    }
                }

                if (dailyReturns.Any())
                {
                    var stdev = Math.Sqrt((double)dailyReturns.Average(x => Math.Pow((double)x - (double)dailyReturns.Average(), 2)));

                    if (stdev < 0.01) { rawScore += 3; details.Add($"Low volatility: daily returns stdev {stdev:P2}"); }
                    else if (stdev < 0.02) { rawScore += 2; details.Add($"Moderate volatility: daily returns stdev {stdev:P2}"); }
                    else if (stdev < 0.04) { rawScore += 1; details.Add($"High volatility: daily returns stdev {stdev:P2}"); }
                    else { details.Add($"Very high volatility: daily returns stdev {stdev:P2}"); }
                }
                else
                {
                    details.Add("Insufficient daily returns data for volatility calc.");
                }
            }
            else
            {
                details.Add("Not enough close-price data points for volatility analysis.");
            }
        }
        else
        {
            details.Add("Not enough price data for volatility analysis.");
        }

        int finalScore = Convert.ToInt32(Math.Min(10, (rawScore / 6.0) * 10));
        return new FinancialAnalysisResult(nameof(RiskReward), finalScore, details);
    }

    /// <summary>
    /// Basic news sentiment: negative keyword check vs. overall volume.
    /// </summary>
    /// <param name="news"></param>
    /// <returns></returns>
    private FinancialAnalysisResult Sentiment(IEnumerable<NewsSentiment> news)
    {
        var newsList = news?.ToList() ?? new List<NewsSentiment>();
        if (!newsList.Any())
            return new FinancialAnalysisResult(nameof(Sentiment),5, new[] { "No news data; defaulting to neutral sentiment" });

        var negativeKeywords = new[]
        {
            "lawsuit", "fraud", "negative", "downturn", "decline", "investigation", "recall"
        };

        int negativeCount = 0;
        foreach (var item in newsList)
        {
            var title = item.Title?.ToLowerInvariant() ?? string.Empty;
            if (negativeKeywords.Any(keyword => title.Contains(keyword)))
                negativeCount++;
        }

        int score;
        List<string> details = new();

        if (negativeCount > newsList.Count * 0.3)
        {
            score = 3;
            details.Add($"High proportion of negative headlines: {negativeCount}/{newsList.Count}");
        }
        else if (negativeCount > 0)
        {
            score = 6;
            details.Add($"Some negative headlines: {negativeCount}/{newsList.Count}");
        }
        else
        {
            score = 8;
            details.Add("Mostly positive/neutral headlines");
        }

        return new FinancialAnalysisResult(nameof(Sentiment), score, details);
    }

    /// <summary>
    /// Simple insider-trade analysis:
    /// - If there's heavy insider buying, we nudge the score up.
    /// - If there's mostly selling, we reduce it.
    /// - Otherwise, neutral.
    /// </summary>
    /// <param name="metrics"></param>
    /// <returns></returns>
    private FinancialAnalysisResult AnalyzeInsiderActivity(IEnumerable<FinancialMetrics> metrics)
    {
        // TODO integrate with a different financial source to get insider trading
        var ordered = metrics.OrderBy(m => m.EndDate).ToList();
        var insiderTrades = ordered.Where(t => t.TransactionSharesFromInsiders.HasValue).Select(li => li.TransactionSharesFromInsiders.Value).ToList();

        var details = new List<string>();
        var score = 5; // default neutral score

        if (!insiderTrades.Any())
        {
            details.Add("No insider trades data; defaulting to neutral");
            return new FinancialAnalysisResult("InsiderActivity", score, details);
        }

        int buys = 0, sells = 0;
        foreach (var trade in insiderTrades)
        {
            if (trade > 0)
                buys++;
            else if (trade < 0)
                sells++;
        }

        var total = buys + sells;
        if (total == 0)
        {
            details.Add("No buy/sell transactions found; neutral");
            return new FinancialAnalysisResult("InsiderActivity", score, details);
        }

        var buyRatio = (double)buys / total;
        if (buyRatio > 0.7)
        {
            score = 8;
            details.Add($"Heavy insider buying: {buys} buys vs. {sells} sells");
        }
        else if (buyRatio > 0.4)
        {
            score = 6;
            details.Add($"Moderate insider buying: {buys} buys vs. {sells} sells");
        }
        else
        {
            score = 4;
            details.Add($"Mostly insider selling: {buys} buys vs. {sells} sells");
        }

        return new FinancialAnalysisResult("InsiderActivity", score, details);
    }

    /// <summary>
    /// Druckenmiller is willing to pay up for growth, but still checks:
    /// - P/E
    /// - P/FCF
    /// - EV/EBIT
    /// - EV/EBITDA
    ///    Each can yield up to 2 points => max 8 raw points => scale to 0–10.
    /// </summary>
    /// <param name="metrics"></param>
    /// <param name="marketCap"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private FinancialAnalysisResult AnalyzeValuation(IEnumerable<FinancialMetrics> metrics, IEnumerable<FinancialLineItem> items, decimal? marketCap)
    {
        var details = new List<string>();
        var rawScore = 0;

        if (!items.Any() || !metrics.Any() || marketCap is null)
        {
            details.Add("Insufficient data for valuation analysis");
            return new FinancialAnalysisResult("Valuation",5, details); // neutral
        }

        var netIncomes = metrics.Where(x => x.NetIncome.HasValue).Select(x => x.NetIncome.Value).ToList();
        var fcfs = metrics.Select(x => x.FreeCashFlow).ToList();
        var ebits = items
            .Where(x => x.Extras.TryGetValue("EBIT", out var oi) && oi is decimal)
            .Select(x => (decimal)x.Extras["EBIT"])
            .ToList();
        var ebitdas = items
            .Where(x => x.Extras.TryGetValue("EBITDA", out var oi) && oi is decimal)
            .Select(x => (decimal)x.Extras["EBITDA"])
            .ToList();
        var debts = metrics.Where(x => x.TotalDebt.HasValue).Select(x => x.TotalDebt.Value).ToList();
        var cashes = metrics.Where(x => x.CashAndCashEquivalentsAtCarryingValue.HasValue).Select(x => x.CashAndCashEquivalentsAtCarryingValue.Value).ToList();

        var recentDebt = debts.FirstOrDefault();
        var recentCash = cashes.FirstOrDefault();
        var enterpriseValue = marketCap.Value + recentDebt - recentCash;

        // 1) P/E
        var recentNetIncome = netIncomes.FirstOrDefault();
        if (recentNetIncome > 0)
        {
            var pe = marketCap.Value / recentNetIncome;
            if (pe < 15)
            {
                rawScore += 2;
                details.Add($"Attractive P/E: {pe:F2}, the stock valuation is attractive");
            }
            else if (pe < 25)
            {
                rawScore += 1;
                details.Add($"Fair P/E: {pe:F2}, the stock appear to be fairly valued");
            }
            else
            {
                details.Add($"High or Very high P/E: {pe:F2}, the stock may be overvalued");
            }
        }
        else
        {
            details.Add("No positive net income for P/E calculation");
        }

        // 2) P/FCF
        var recentFcf = fcfs.FirstOrDefault();
        if (recentFcf > 0)
        {
            var pfcf = marketCap.Value / recentFcf;
            if (pfcf < 15)
            {
                rawScore += 2;
                details.Add($"Attractive P/FCF: {pfcf:F2}, the company is attractive relative to how much free cash it's generating");
            }
            else if (pfcf < 25)
            {
                rawScore += 1;
                details.Add($"Fair P/FCF: {pfcf:F2}, the company is fair valued relative to how much free cash it's generating");
            }
            else
            {
                details.Add($"High/Very high P/FCF: {pfcf:F2}, the company is expensive relative to how much free cash it's generating");
            }
        }
        else
        {
            details.Add("No positive free cash flow for P/FCF calculation");
        }

        // 3) EV/EBIT
        var recentEbit = ebits.FirstOrDefault();
        if (enterpriseValue > 0 && recentEbit > 0)
        {
            var evEbit = enterpriseValue / recentEbit;
            if (evEbit < 15)
            {
                rawScore += 2;
                details.Add($"Attractive EV/EBIT: {evEbit:F2}, the stock is cheap relative to its operating earnings");
            }
            else if (evEbit < 25)
            {
                rawScore += 1;
                details.Add($"Fair EV/EBIT: {evEbit:F2}, the stock value is fair relative to its operating earnings");
            }
            else
            {
                details.Add($"High EV/EBIT: {evEbit:F2}, the stock is expensive relative to its operating earnings");
            }
        }
        else
        {
            details.Add("No valid EV/EBIT because EV <= 0 or EBIT <= 0");
        }

        // 4) EV/EBITDA
        var recentEbitda = ebitdas.FirstOrDefault();
        if (enterpriseValue > 0 && recentEbitda > 0)
        {
            var evEbitda = enterpriseValue / recentEbitda;
            if (evEbitda < 10)
            {
                rawScore += 2;
                details.Add($"Attractive EV/EBITDA: {evEbitda:F2}, the stock is attractive relative to its cash-operating profitability");
            }
            else if (evEbitda < 18)
            {
                rawScore += 1;
                details.Add($"Fair EV/EBITDA: {evEbitda:F2}, the stock value is fair relative to its cash-operating profitability");
            }
            else
            {
                details.Add($"High EV/EBITDA: {evEbitda:F2}, the stock is expensive relative to its cash-operating profitability");
            }
        }
        else
        {
            details.Add("No valid EV/EBITDA because EV <= 0 or EBITDA <= 0");
        }

        var finalScore = Math.Min(10, rawScore * 10.0 / 8.0);
        return new FinancialAnalysisResult("Valuation", (int)Math.Round(finalScore), details);
    }


    private bool TryGenerateOutput(string ticker, FinancialAnalysisResult growthMomentum,
        FinancialAnalysisResult riskReward, FinancialAnalysisResult valuation, FinancialAnalysisResult sentiment,
        FinancialAnalysisResult insiderActivity, double totalScore, int maxScore, out TradeSignal tradeSignal)
    {
        tradeSignal = default!;

        var systemMessage =
            @"You are a Stanley Druckenmiller AI agent, making investment decisions using his principles:

1. Seek asymmetric risk-reward opportunities (large upside, limited downside).
2. Emphasize growth, momentum, and market sentiment.
3. Preserve capital by avoiding major drawdowns.
4. Willing to pay higher valuations for true growth leaders.
5. Be aggressive when conviction is high.
6. Cut losses quickly if the thesis changes.

Rules:
- Reward companies showing strong revenue/earnings growth and positive stock momentum.
- Evaluate sentiment and insider activity as supportive or contradictory signals.
- Watch out for high leverage or extreme volatility that threatens capital.
- Output a JSON object with signal, confidence, and a reasoning string.";

        var analysisData = new
        {
            score = totalScore,
            max_score = maxScore,
            GrowthAndMomentum = growthMomentum,
            RiskReward = riskReward,
            Valuation = valuation,
            Sentiment = sentiment,
            InsiderActivity = insiderActivity
        };

        return LlmTradeSignalGenerator.TryGenerateSignal(
            _httpLib,
            endpoint: "chat/completions",
            ticker: ticker,
            systemMessage: systemMessage,
            analysisData: analysisData,
            agentName: "Stanley Druckenmiller",
            out tradeSignal
        );
    }
}
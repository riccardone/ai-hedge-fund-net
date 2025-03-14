using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;

namespace ai_hedge_fund_net.Agents
{
    public class BenGraham : ITradingAgent
    {
        public string Name => nameof(BenGraham);

        public IDictionary<string, IEnumerable<string>> AnalyzeEarningsStability(IEnumerable<FinancialMetrics> financialMetricsItems, IEnumerable<FinancialLineItem> financialLineItems)
        {
            //Graham wants at least several years of consistently positive earnings(ideally 5 +).
            //We'll check:
            //1.Number of years with positive EPS.
            //2.Growth in EPS from first to last period.

            var details = new List<string>();
            int score = 0;

            var epsValues = financialLineItems
                .Where(item => item.Extras.ContainsKey("EarningsPerShare"))
                .Select(item => item.Extras["EarningsPerShare"])
                .ToList();

            if (epsValues.Count < 2)
            {
                details.Add("Not enough multi-year EPS data.");
                return new Dictionary<string, IEnumerable<string>> { { "Score", new[] { score.ToString() } }, { "Details", details } };
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

            return new Dictionary<string, IEnumerable<string>> { { "Score", new[] { score.ToString() } }, { "Details", details } };
        }

        public IDictionary<string, IEnumerable<string>> AnalyzeFinancialStrength(FinancialMetrics financialMetrics, IEnumerable<FinancialLineItem> financialLineItems)
        {
            var details = new List<string>();
            int score = 0;

            var latestItem = financialLineItems.LastOrDefault();
            if (latestItem == null)
            {
                details.Add("No data for financial strength analysis");
                return new Dictionary<string, IEnumerable<string>> { { "Score", new[] { score.ToString() } }, { "Details", details } };
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

            return new Dictionary<string, IEnumerable<string>> { { "Score", new[] { score.ToString() } }, { "Details", details } };
        }

        public IDictionary<string, IEnumerable<string>> AnalyzeValuation(FinancialMetrics financialMetrics, IEnumerable<FinancialLineItem> financialLineItems, decimal marketCap)
        {
            var details = new List<string>();
            int score = 0;
            var latest = financialLineItems.LastOrDefault();
            if (latest == null || marketCap <= 0)
            {
                details.Add("Insufficient data to perform valuation.");
                return new Dictionary<string, IEnumerable<string>> { { "Score", new[] { score.ToString() } }, { "Details", details } };
            }

            decimal netCurrentAssetValue = (latest.Extras["CurrentAssets"] ?? 0) - (latest.Extras["TotalLiabilities"] ?? 0);
            if (netCurrentAssetValue > marketCap)
            {
                score += 4;
                details.Add("Net-Net: NCAV > Market Cap (classic Graham deep value).");
            }
            else if (netCurrentAssetValue >= marketCap * 0.67m)
            {
                score += 2;
                details.Add("NCAV Per Share >= 2/3 of Price Per Share (moderate net-net discount).");
            }

            return new Dictionary<string, IEnumerable<string>> { { "Score", new[] { score.ToString() } }, { "Details", details } };
        }

        public TradeSignal GenerateOutput()
        {
            // Placeholder for AI-driven output generation
            return new TradeSignal
            {
                Signal = "neutral",
                Confidence = 50,
                Reasoning = "Default analysis; implementation required."
            };
        }
    }
}

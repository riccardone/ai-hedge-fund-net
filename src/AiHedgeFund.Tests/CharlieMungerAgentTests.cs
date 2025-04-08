using AiHedgeFund.Agents;
using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using AiHedgeFund.Tests.Fakes;

namespace AiHedgeFund.Tests
{
    public class CharlieMungerAgentTests
    {
        private IEnumerable<FinancialLineItem> _lineItems;
        private TradingWorkflowState _state;

        [SetUp]
        public void Setup()
        {
            _lineItems = new List<FinancialLineItem>
            {
                new FinancialLineItem("AAPL", "2024", "Annual", "USD", new Dictionary<string, dynamic>
                {
                    ["return_on_invested_capital"] = 0.18m,
                    ["gross_margin"] = 0.42m,
                    ["capital_expenditure"] = -5000m,
                    ["revenue"] = 100000m,
                    ["research_and_development"] = 2000m,
                    ["goodwill_and_intangible_assets"] = 3000m,
                }),
                new FinancialLineItem("AAPL", "2023", "Annual", "USD", new Dictionary<string, dynamic>
                {
                    ["return_on_invested_capital"] = 0.16m,
                    ["gross_margin"] = 0.40m,
                    ["capital_expenditure"] = -4500m,
                    ["revenue"] = 95000m,
                    ["research_and_development"] = 1800m,
                    ["goodwill_and_intangible_assets"] = 2800m,
                }),
                new FinancialLineItem("AAPL", "2022", "Annual", "USD", new Dictionary<string, dynamic>
                {
                    ["return_on_invested_capital"] = 0.14m,
                    ["gross_margin"] = 0.38m,
                    ["capital_expenditure"] = -4800m,
                    ["revenue"] = 90000m,
                    ["research_and_development"] = 1500m,
                    ["goodwill_and_intangible_assets"] = 2600m,
                })
            };
            _state = new TradingWorkflowState
            {
                AnalystSignals = new Dictionary<string, IDictionary<string, object>>(),
                Portfolio = new Portfolio(),
                Tickers = new List<string> { "AAPL" },
                FinancialLineItems = new Dictionary<string, IEnumerable<FinancialLineItem>> { { "AAPL", _lineItems } },
                FinancialMetrics = new Dictionary<string, IEnumerable<FinancialMetrics>>(),
                InitialCash = 100000,
                TradeDecisions = new Dictionary<string, TradeDecision?>()
            };
        }

        [Test]
        public void RunPositiveInput()
        {
            // Assign
            var sut = new CharlieMungerAgent(new FakeHttpLib());

            // Act
            var signals = sut.Run(_state);

            // Assert
            Assert.That(signals.First().Confidence.Equals(70));
        }
    }
}

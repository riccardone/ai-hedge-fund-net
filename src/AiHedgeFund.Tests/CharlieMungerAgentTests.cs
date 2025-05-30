﻿using AiHedgeFund.Agents;
using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using AiHedgeFund.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

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
                new("AAPL", new DateTime(2024, 12, 31), "Annual", "USD", new Dictionary<string, dynamic>
                {
                    ["return_on_invested_capital"] = 0.18m,
                    ["gross_margin"] = 0.42m,
                    ["capital_expenditure"] = -5000m,
                    ["revenue"] = 100000m,
                    ["research_and_development"] = 2000m,
                    ["goodwill_and_intangible_assets"] = 3000m,
                }),
                new("AAPL", new DateTime(2023, 12, 31), "Annual", "USD", new Dictionary<string, dynamic>
                {
                    ["return_on_invested_capital"] = 0.16m,
                    ["gross_margin"] = 0.40m,
                    ["capital_expenditure"] = -4500m,
                    ["revenue"] = 95000m,
                    ["research_and_development"] = 1800m,
                    ["goodwill_and_intangible_assets"] = 2800m,
                }),
                new("AAPL", new DateTime(2022, 12, 31), "Annual", "USD", new Dictionary<string, dynamic>
                {
                    ["return_on_invested_capital"] = 0.14m,
                    ["gross_margin"] = 0.38m,
                    ["capital_expenditure"] = -4800m,
                    ["revenue"] = 90000m,
                    ["research_and_development"] = 1500m,
                    ["goodwill_and_intangible_assets"] = 2600m,
                })
            };
            var metrics = new List<FinancialMetrics>
            {
                FinancialMetricsFactory.CreateTestMetrics("AAPL", 2022, 80000, 90000, 4800, 1000, 1_500_000_000),
                FinancialMetricsFactory.CreateTestMetrics("AAPL", 2023, 82000, 92000, 4500, 1100, 1_600_000_000),
                FinancialMetricsFactory.CreateTestMetrics("AAPL", 2024, 84000, 94000, 5000, 1150, 1_700_000_000)
            };

            _state = new TradingWorkflowState
            {
                FinancialMetrics = new Dictionary<string, IEnumerable<FinancialMetrics>> { { "AAPL", metrics } },
                Portfolio = new Portfolio(),
                Tickers = new List<string> { "AAPL" },
                FinancialLineItems = new Dictionary<string, IEnumerable<FinancialLineItem>> { { "AAPL", _lineItems } }
            };
        }

        [Test]
        public void RunPositiveInput()
        {
            // Assign
            var sut = new CharlieMungerAgent(new FakeHttpLib(), new NullLogger<CharlieMungerAgent>());

            // Act
           sut.Run(_state);

            // Assert
            Assert.That(_state.AnalystSignals.First().Value.Values.First().Confidence.Equals(85M));
        }
    }
}

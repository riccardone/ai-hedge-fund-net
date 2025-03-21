﻿using ai_hedge_fund_net.Agents;
using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;
using NLog;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace ai_hedge_fund_net.ConsoleApp.WorkflowSteps;

public class InitializeTradingStateStep : StepBody
{
    private readonly IDataReader _alphaVantageService;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public InitializeTradingStateStep(IDataReader alphaVantageService)
    {
        _alphaVantageService = alphaVantageService;
    }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        var state = context.Workflow.Data as TradingWorkflowState;
        if (state == null) return ExecutionResult.Next();

        // Default tickers if none provided
        if (state.Tickers == null || !state.Tickers.Any())
        {
            state.Tickers = new List<string> { "MSFT", "AAPL" };
        }

        // Default end date to today
        if (string.IsNullOrEmpty(state.EndDate))
        {
            state.EndDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        // Default start date to 3 months before end date
        if (string.IsNullOrEmpty(state.StartDate))
        {
            DateTime endDateParsed = DateTime.Parse(state.EndDate);
            state.StartDate = endDateParsed.AddMonths(-3).ToString("yyyy-MM-dd");
        }

        // Initialize portfolio
        state.Portfolio = new Portfolio
        {
            Cash = state.InitialCash,
            MarginRequirement = state.MarginRequirement,
            Positions = state.Tickers.ToDictionary(
                ticker => ticker,
                ticker => new Position()
            ),
            RealizedGains = state.Tickers.ToDictionary(
                ticker => ticker,
                ticker => new RealizedGains()
            )
        };

        // Initialize empty analyst signals
        state.AnalystSignals = new Dictionary<string, object>();

        // Initialize empty trade decisions
        state.TradeDecisions = new Dictionary<string, TradeDecision>();

        foreach (var ticker in state.Tickers)
        {
            state.FinancialMetrics.Add(ticker, _alphaVantageService.GetFinancialMetricsAsync(state.Tickers.First()).Result);
        }

        Logger.Info("Trading workflow state initialized:");
        Logger.Info($"Start Date: {state.StartDate}, End Date: {state.EndDate}");
        Logger.Info($"Tickers: {string.Join(", ", state.Tickers)}");
        Logger.Info($"Initial Cash: {state.InitialCash}, Margin Requirement: {state.MarginRequirement}");

        return ExecutionResult.Next();
    }
}
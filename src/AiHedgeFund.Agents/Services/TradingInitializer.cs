using AiHedgeFund.Contracts;
using Microsoft.Extensions.Logging;

namespace AiHedgeFund.Agents.Services;

public class TradingInitializer
{
    private readonly ILogger<TradingInitializer> _logger;
    private readonly AppArguments _args;
    private readonly IDataReader _dataReader;

    public TradingInitializer(AppArguments args, IDataReader dataReader, ILogger<TradingInitializer> logger)
    {
        _args = args;
        _dataReader = dataReader;
        _logger = logger;
    }

    public async Task<TradingWorkflowState> InitializeAsync()
    {
        var state = new TradingWorkflowState
        {
            //InitialCash = _args.InitialCash,
            Tickers = _args.Tickers,
            SelectedAnalysts = _args.AgentNames,
            RiskLevel = _args.RiskLevel,
            StartDate = _args.StartDate,
            EndDate = _args.EndDate
        };

        foreach (var ticker in state.Tickers)
        {
            if (!_dataReader.TryGetFinancialMetrics(ticker, DateTime.Today, "ttm", 10, out var metrics))
            {
                _logger.LogError($"I can't retrieve metrics for {ticker}");
                continue;
            }
            state.FinancialMetrics.Add(ticker, metrics);
            if (!_dataReader.TryGetFinancialLineItems(ticker, DateTime.Today, "ttm", 10, out var financialLineItems))
            {
                _logger.LogError($"I can't retrieve financial data for {ticker}");
                continue;
            }
            state.FinancialLineItems.Add(ticker, financialLineItems);
            if (!_dataReader.TryGetPrices(ticker, state.StartDate, state.EndDate, out var prices))
            {
                _logger.LogError($"I can't retrieve prices for {ticker}");
                continue;
            }
            if (!_dataReader.TryGetCompanyNews(ticker, out var companyNews))
            {
                _logger.LogError($"I can't retrieve company news data for {ticker}");
                continue;
            }
            state.CompanyNews.Add(ticker, companyNews);
            state.Prices.Add(ticker, prices);
        }

        await Task.CompletedTask;

        //Logger.Info($"Initial Cash: {state.InitialCash}");
        //Logger.Info($"Margin Rate: {state.MarginRequirement}");
        //Logger.Info($"Selected Tickers: {string.Join(", ", state.Tickers)}");
        //Logger.Info($"Start Date: {state.StartDate}");
        //Logger.Info($"End Date: {state.EndDate}");
        //Logger.Info($"Show Reasoning: {state.ShowReasoning}");
        //Logger.Info($"Model Name: {state.ModelName}");
        //Logger.Info($"Risk Level: {state.RiskLevel}");

        return state;
    }
}
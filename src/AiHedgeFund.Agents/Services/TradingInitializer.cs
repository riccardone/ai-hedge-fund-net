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
            ModelName = _args.ModelName,
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
                _logger.LogWarning($"Financial line items unavailable for {ticker} — trend analysis will be limited");
            else
                state.FinancialLineItems.Add(ticker, financialLineItems!);

            if (!_dataReader.TryGetPrices(ticker, state.StartDate, state.EndDate, out var prices))
            {
                _logger.LogError($"I can't retrieve prices for {ticker}");
                continue;
            }
            state.Prices.Add(ticker, prices);

            if (!_dataReader.TryGetCompanyNews(ticker, out var companyNews))
                _logger.LogWarning($"Company news unavailable for {ticker} — sentiment analysis will be skipped");
            else
                state.CompanyNews.Add(ticker, companyNews!);
        }

        await Task.CompletedTask;

        _logger.LogInformation("Model:      {Model}", state.ModelName);
        _logger.LogInformation("Agents:     {Agents}", string.Join(", ", state.SelectedAnalysts));
        _logger.LogInformation("Tickers:    {Tickers}", string.Join(", ", state.Tickers));
        _logger.LogInformation("Risk level: {RiskLevel}", state.RiskLevel);
        _logger.LogInformation("Period:     {Start:yyyy-MM-dd} → {End:yyyy-MM-dd}", state.StartDate, state.EndDate);

        return state;
    }
}
using AiHedgeFund.Agents.Services;
using AiHedgeFund.Agents;
using AiHedgeFund.Api.Services;
using AiHedgeFund.Contracts.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AiHedgeFund.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/v{version:apiVersion}/agents")]
[AuthorizeTenant]
[Authorize(Roles = "Trader,Admin")]
public class AgentsController : Controller
{
    private readonly TradingInitializer _initializer;
    private readonly PortfolioManager _portfolio;
    private readonly RiskManagerAgent _riskAgent;

    public AgentsController(TradingInitializer initializer, PortfolioManager portfolio, RiskManagerAgent riskAgent)
    {
        _initializer = initializer;
        _portfolio = portfolio;
        _riskAgent = riskAgent;
    }

    [HttpGet("{agentId}/tickers/{ticker}")]
    [MapToApiVersion("1.0")]
    [SwaggerOperation(Summary = "Get output signal", Description = "Fetch signal from agent")]
    public IActionResult Get(string agentId, string ticker)
    {
        var tenantId = HttpContext.Items["TenantId"]?.ToString();

        var state = _initializer.InitializeAsync(new List<string> {agentId}, new List<string> {ticker}, RiskLevel.Medium, null, null).Result;

        foreach (var agent in state.SelectedAnalysts)
            _portfolio.Evaluate(agent, state);

        // TODO
        //_portfolio.RunRiskAssessments("risk_management_agent", state, _riskAgent);

        var result = new AgentOutput(agentId, ticker, DateTime.UtcNow, state.AnalystSignals[agentId][ticker],
            state.TradeDecisions[agentId], null);

        return Ok(result);
    }
}
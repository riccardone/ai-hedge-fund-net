using AiHedgeFund.Api.Services;
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
public class TradingController : Controller
{
    [HttpGet("{agentId}/tickers/{ticker}")]
    [MapToApiVersion("1.0")]
    [SwaggerOperation(Summary = "Get output signal", Description = "Fetch signal from agent")]
    public IActionResult Get(string agentId, string ticker)
    {
        var tenantId = HttpContext.Items["TenantId"]?.ToString();
        return Ok(new { agentId, ticker, tenantId });
    }
}
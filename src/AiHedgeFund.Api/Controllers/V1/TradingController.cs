using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using AiHedgeFund.Api.Services;

namespace AiHedgeFund.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/{tenantId}")]
[AuthorizeTenant]
public class TradingController : Controller
{
    [HttpGet("{agentId}")]
    [SwaggerOperation(
        Summary = "Get the output signal from one of the agents",
        Description = "Fetches a signal from an agent related to a given ticker",
        OperationId = "GetOutputFromAgent",
        Tags = ["reads"]
    )]
    [SwaggerResponse((int)HttpStatusCode.OK, "OK response", typeof(string), ContentTypes = ["application/json"])]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "NOT FOUND response", typeof(string), ContentTypes = ["application/json"])]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Unauthorized access - API key is missing or invalid.", typeof(object), ContentTypes = ["application/json"])]
    public IActionResult GetOutputFromAgent(
        [FromRoute] string tenantId,
        [FromRoute] string agentId,
        [FromRoute] string ticker)
    {
        return new OkObjectResult("test");
    }
}
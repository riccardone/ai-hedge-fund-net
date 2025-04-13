using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using NLog;

namespace AiHedgeFund.Api.Services;

public class NoOpAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public NoOpAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If middleware has already set the user, let it through
        if (Context.User?.Identity?.IsAuthenticated == true)
        {
            var ticket = new AuthenticationTicket(Context.User, Scheme.Name);
            Logger.Info($"Current User: {Context.User?.Identity?.Name ?? "null"}");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        // Otherwise, indicate no authentication
        return Task.FromResult(AuthenticateResult.NoResult());
    }
}
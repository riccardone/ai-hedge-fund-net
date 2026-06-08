using AiHedgeFund.Contracts.Model;

namespace AiHedgeFund.Contracts;

/// <summary>
/// Supplies optional historical base-rate grounding for a (symbol, date). Implementations
/// must be degrade-safe: a failed lookup returns false and a null base rate, never throws,
/// and never blocks the trading flow. When <see cref="Enabled"/> is false the provider is a no-op.
/// </summary>
public interface IBaseRateProvider
{
    bool Enabled { get; }

    bool TryGetBaseRate(string symbol, DateTime asOf, out BaseRate? baseRate);
}

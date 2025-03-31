using ai_hedge_fund_net.Contracts.Model;

namespace ai_hedge_fund_net.Contracts.Extensions;

public static class FinancialLineItemExtensions
{
    public static decimal? GetDecimal(this FinancialLineItem item, string key)
    {
        return item.Extras.TryGetValue(key, out var value) && value is decimal d ? d : null;
    }
}
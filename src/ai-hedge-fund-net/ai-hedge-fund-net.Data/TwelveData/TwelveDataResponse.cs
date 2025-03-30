namespace ai_hedge_fund_net.Data.TwelveData;

public class TwelveDataResponse
{
    public string? Status { get; set; }
    public List<TwelveDataValue>? Values { get; set; }
}

public class TwelveDataValue
{
    public string? Datetime { get; set; }
    public string? Volume { get; set; }
}
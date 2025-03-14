namespace ai_hedge_fund_net.Contracts.Model;

public class Price
{
    public decimal Open { get; set; }
    public decimal Close { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public int Volume { get; set; }
    public string Time { get; set; } = string.Empty; // ISO 8601 format (UTC)
}
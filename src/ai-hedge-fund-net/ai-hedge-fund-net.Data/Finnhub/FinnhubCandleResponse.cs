namespace ai_hedge_fund_net.Data.Finnhub;

public class FinnhubCandleResponse
{
    public long[] T { get; set; } = Array.Empty<long>(); // Time
    public decimal[] O { get; set; } = Array.Empty<decimal>(); // Open
    public decimal[] H { get; set; } = Array.Empty<decimal>(); // High
    public decimal[] L { get; set; } = Array.Empty<decimal>(); // Low
    public decimal[] C { get; set; } = Array.Empty<decimal>(); // Close
    public decimal[] V { get; set; } = Array.Empty<decimal>(); // Volume
    public string S { get; set; } = ""; // Status
}
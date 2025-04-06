using AiHedgeFund.Contracts;

namespace AiHedgeFund.Data.Mock;

public class FakePriceVolumeProvider : IPriceVolumeProvider
{
    private readonly Random _random = new();

    public decimal? GetVolume(string ticker, DateTime date)
    {
        // Return a realistic random volume between 1M and 10M
        return _random.Next(1_000_000, 10_000_000);

        // Or use a fixed value instead
        // return 5_000_000m;
    }
}
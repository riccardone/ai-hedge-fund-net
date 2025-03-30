namespace ai_hedge_fund_net.Contracts;

public interface IPriceVolumeProvider
{
    decimal? GetVolume(string ticker, DateTime date);
}
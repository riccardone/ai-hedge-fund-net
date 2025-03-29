namespace ai_hedge_fund_net.Contracts;

public interface IDataManager
{
    void Save<T>(T data, string key);
    T? Read<T>(string key);
}
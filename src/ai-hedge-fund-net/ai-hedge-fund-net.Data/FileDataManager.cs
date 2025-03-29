using System.Text.Json;
using ai_hedge_fund_net.Contracts;

namespace ai_hedge_fund_net.Data;

public class FileDataManager : IDataManager
{
    private readonly string _storageFolder;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileDataManager(string storageFolder = "AlphaVantageCache")
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(_storageFolder);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public void Save<T>(T data, string key)
    {
        var path = GetFilePath(key);
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        File.WriteAllText(path, json);
    }

    public T? Read<T>(string key)
    {
        var path = GetFilePath(key);
        if (!File.Exists(path))
            return default;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    private string GetFilePath(string key)
    {
        var safeKey = key.Replace(":", "_").Replace("/", "_").Replace("?", "_").Replace("&", "_").Replace("=", "_");
        return Path.Combine(_storageFolder, $"{safeKey}.json");
    }
}
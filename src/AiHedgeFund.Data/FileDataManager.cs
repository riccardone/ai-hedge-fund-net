using NLog;
using System.Reflection;
using System.Text.Json;

namespace AiHedgeFund.Data;

public class FileDataManager
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
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

    public bool TrySave<T>(T data, string key)
    {
        try
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "Can't save null object");

            // check if object has all nulls
            var allPropsNull = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .All(p => p.GetValue(data) == null);
            if (allPropsNull)
                throw new InvalidOperationException("Can't save object with null data");

            var path = GetFilePath(key);
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception e)
        {
            Logger.Error($"Error while {nameof(FileDataManager)}/{nameof(TrySave)}: {e.GetBaseException().Message}");
            return false;
        }
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
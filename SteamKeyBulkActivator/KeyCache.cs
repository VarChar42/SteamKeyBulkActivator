using Newtonsoft.Json;
using SteamKit2;

namespace SteamKeyBulkActivator;

public class KeyCache
{
    private const string CacheFileName = "keys.cache";

    private Dictionary<string, CachedKey> steamKeys;

    private static readonly EPurchaseResultDetail[] NotSkipResult = new[]
        {EPurchaseResultDetail.RateLimited, EPurchaseResultDetail.NoDetail};

    public KeyCache()
    {
        Load();
    }

    public IEnumerable<string> Codes => steamKeys.Keys;

    public void Load()
    {
        steamKeys = new();

        if (!File.Exists(CacheFileName))
        {
            return;
        }
        
        var keys = JsonConvert.DeserializeObject<List<CachedKey>>(File.ReadAllText(CacheFileName));

        if (keys == null)
        {
            return;
        }
        
        foreach (CachedKey key in keys)
        {
            steamKeys[key.Key] = key;
        }
    }

    public void Save()
    {
        var data = JsonConvert.SerializeObject(steamKeys.Values);
        File.WriteAllText(CacheFileName, data);
    }
    
    public bool IsRedeemed(string key, bool skip = true)
    {
        if (steamKeys.TryGetValue(key, out CachedKey cachedKey))
        {
            if (!skip)
            {
                return true;
            }
            
            return !NotSkipResult.Contains(cachedKey.LastResultDetails);
        }
        
        
        steamKeys[key] = new CachedKey
        {
            Key = key,
            LastResultDetails = EPurchaseResultDetail.NoDetail,
            LastResultDateTime = DateTime.Now
                
        };
        return false;
    }

    public void Print()
    {
        var keys = steamKeys.Values;
        foreach (var key in keys)
        {
            Console.WriteLine($"{key.Key} : {key.LastResultDetails} [{key.LastResultDateTime.ToShortDateString()}]");
        }

        Console.WriteLine($"A total of {keys.Count} are loaded!");
    }
    
    public void SetResultDetails(string key, EPurchaseResultDetail result)
    {
        var cachedKey = steamKeys[key];

        cachedKey.LastResultDetails = result;
        cachedKey.LastResultDateTime = DateTime.Now;
    }

    public bool Remove(string key)
    {
        return steamKeys.Remove(key);
    }
}
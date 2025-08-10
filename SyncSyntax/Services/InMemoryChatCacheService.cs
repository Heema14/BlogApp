using Microsoft.Extensions.Caching.Memory;

public class InMemoryChatCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryChatCacheService> _logger;

    public InMemoryChatCacheService(IMemoryCache cache, ILogger<InMemoryChatCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<Message>> GetMessagesAsync(string key)
    {
        if (_cache.TryGetValue(key, out List<Message> messages))
        {
            Console.WriteLine("✅ Retrieved from In-Memory Cache.");
            return messages;
        }

        Console.WriteLine("❌ Not found in cache (will fetch from DB).");
        return null;
    }


    public async Task SetMessagesAsync(string key, List<Message> messages)
    {
        _cache.Set(key, messages, TimeSpan.FromMinutes(10));
        Console.WriteLine($"✅ Cached {messages.Count} messages under key: {key}");
    }

}

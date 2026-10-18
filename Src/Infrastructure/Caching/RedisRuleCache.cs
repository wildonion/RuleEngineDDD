using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using StackExchange.Redis;

namespace Infrastructure.Caching;

public sealed class RedisRuleCache(IConnectionMultiplexer redis) : IRuleCache
{
    private readonly IDatabase _db = redis.GetDatabase();
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);
    private static string Key(string deviceId) => $"rule-engine:device:{deviceId}:active:v1";

    public async Task<IReadOnlyList<RuleSnapshot>?> GetActiveRulesAsync(string deviceId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var value = await _db.StringGetAsync(Key(deviceId));
        if (value.IsNullOrEmpty) return null;
        try { return JsonSerializer.Deserialize<List<RuleSnapshot>>(value!) ?? []; }
        catch (JsonException) { await _db.KeyDeleteAsync(Key(deviceId)); return null; }
    }
    public async Task SetActiveRulesAsync(string deviceId, IReadOnlyList<RuleSnapshot> rules, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await _db.StringSetAsync(Key(deviceId), JsonSerializer.Serialize(rules), Ttl);
    }
    public async Task InvalidateDeviceAsync(string deviceId, CancellationToken ct)
    { ct.ThrowIfCancellationRequested(); await _db.KeyDeleteAsync(Key(deviceId)); }
}

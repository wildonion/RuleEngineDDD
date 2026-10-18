using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces;

public interface IRuleRepository
{
    Task AddAsync(Rule rule, CancellationToken ct);
    Task<Rule?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Rule>> GetByDeviceAsync(string deviceId, bool enabledOnly, CancellationToken ct);
    void Update(Rule rule);
}

public interface IProcessedEventRepository
{
    Task<bool> ExistsAsync(string eventId, CancellationToken ct);
    Task AddAsync(ProcessedTelemetryEvent item, CancellationToken ct);
}

public sealed record OutboxItem(Guid Id, string MessageId, string RoutingKey, string PayloadJson,
    DateTimeOffset CreatedAt, DateTimeOffset? PublishedAt);

public interface IOutboxRepository
{
    Task AddAsync(OutboxItem message, CancellationToken ct);
    Task<IReadOnlyList<OutboxItem>> GetPendingAsync(int take, CancellationToken ct);
    Task MarkPublishedAsync(Guid id, DateTimeOffset publishedAt, CancellationToken ct);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default);
}

public interface IRuleCache
{
    Task<IReadOnlyList<RuleSnapshot>?> GetActiveRulesAsync(string deviceId, CancellationToken ct);
    Task SetActiveRulesAsync(string deviceId, IReadOnlyList<RuleSnapshot> rules, CancellationToken ct);
    Task InvalidateDeviceAsync(string deviceId, CancellationToken ct);
}

public interface IRuleCompiler
{
    string Compile(RuleDefinitionDto definition);
}

public interface IRuleEvaluator
{
    bool Evaluate(string luaScript, IReadOnlyDictionary<string, System.Text.Json.JsonElement> properties);
}

public interface IActionPublisher
{
    Task PublishAsync(string routingKey, string jsonPayload, CancellationToken ct);
}

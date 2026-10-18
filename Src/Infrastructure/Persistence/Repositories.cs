using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class RuleRepository(AppDbContext db) : IRuleRepository
{
    public async Task AddAsync(Rule rule, CancellationToken ct) { await db.Rules.AddAsync(rule, ct); }
    public Task<Rule?> GetAsync(Guid id, CancellationToken ct) => db.Rules.FirstOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<Rule>> GetByDeviceAsync(string deviceId, bool enabledOnly, CancellationToken ct)
    {
        var query = db.Rules.AsNoTracking().Where(x => x.DeviceId == deviceId);
        if (enabledOnly) query = query.Where(x => x.Enabled);
        return await query.OrderBy(x => x.CreatedAt).ToListAsync(ct);
    }
    public void Update(Rule rule) => db.Rules.Update(rule);
}

public sealed class ProcessedEventRepository(AppDbContext db) : IProcessedEventRepository
{
    public Task<bool> ExistsAsync(string eventId, CancellationToken ct) => db.ProcessedEvents.AnyAsync(x => x.EventId == eventId, ct);
    public async Task AddAsync(ProcessedTelemetryEvent item, CancellationToken ct) { await db.ProcessedEvents.AddAsync(item, ct); }
}

public sealed class OutboxRepository(AppDbContext db) : IOutboxRepository
{
    public async Task AddAsync(OutboxItem message, CancellationToken ct)
    {
        await db.OutboxMessages.AddAsync(new OutboxMessage(message.MessageId, message.RoutingKey,
            message.PayloadJson, message.CreatedAt), ct);
    }
    public async Task<IReadOnlyList<OutboxItem>> GetPendingAsync(int take, CancellationToken ct) =>
        await db.OutboxMessages.AsNoTracking().Where(x => x.PublishedAt == null)
            .OrderBy(x => x.CreatedAt).Take(take)
            .Select(x => new OutboxItem(x.Id, x.MessageId, x.RoutingKey, x.PayloadJson, x.CreatedAt, x.PublishedAt))
            .ToListAsync(ct);
    public async Task MarkPublishedAsync(Guid id, DateTimeOffset publishedAt, CancellationToken ct)
    {
        var message = await db.OutboxMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (message is not null) message.MarkPublished(publishedAt);
    }
}

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try { await operation(ct); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); }
        catch { await transaction.RollbackAsync(ct); throw; }
    }
}

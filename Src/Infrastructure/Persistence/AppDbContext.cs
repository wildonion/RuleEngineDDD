using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<ProcessedTelemetryEvent> ProcessedEvents => Set<ProcessedTelemetryEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        var rule = b.Entity<Rule>();
        rule.ToTable("rules"); rule.HasKey(x => x.Id);
        rule.Property(x => x.DeviceId).HasMaxLength(160).IsRequired();
        rule.Property(x => x.Name).HasMaxLength(120).IsRequired();
        rule.Property(x => x.DefinitionJson).HasColumnType("jsonb").IsRequired();
        rule.Property(x => x.ActionsJson).HasColumnType("jsonb").IsRequired();
        rule.Property(x => x.LuaScript).IsRequired();
        rule.HasIndex(x => new { x.DeviceId, x.Enabled });

        var processed = b.Entity<ProcessedTelemetryEvent>();
        processed.ToTable("processed_telemetry_events"); processed.HasKey(x => x.EventId);
        processed.Property(x => x.EventId).HasMaxLength(200);
        processed.Property(x => x.DeviceId).HasMaxLength(160).IsRequired();

        var outbox = b.Entity<OutboxMessage>();
        outbox.ToTable("outbox_messages"); outbox.HasKey(x => x.Id);
        outbox.HasIndex(x => x.MessageId).IsUnique();
        outbox.HasIndex(x => new { x.PublishedAt, x.CreatedAt });
        outbox.Property(x => x.RoutingKey).HasMaxLength(180).IsRequired();
        outbox.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        outbox.Property(x => x.LastError).HasMaxLength(2000);
    }
}

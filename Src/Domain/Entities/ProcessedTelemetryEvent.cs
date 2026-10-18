namespace Domain.Entities;

// Persistence-facing idempotency record; an event ID should be stable across MQTT redelivery.
public sealed class ProcessedTelemetryEvent
{
    private ProcessedTelemetryEvent() { }
    public string EventId { get; private set; } = null!;
    public string DeviceId { get; private set; } = null!;
    public DateTimeOffset ProcessedAt { get; private set; }
    public static ProcessedTelemetryEvent Create(string eventId, string deviceId, DateTimeOffset now)
        => new() { EventId = eventId, DeviceId = deviceId, ProcessedAt = now };
}

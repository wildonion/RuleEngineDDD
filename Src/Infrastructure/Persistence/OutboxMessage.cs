namespace Infrastructure.Persistence;

public sealed class OutboxMessage
{
    private OutboxMessage() { }
    public Guid Id { get; private set; }
    public string MessageId { get; private set; } = null!;
    public string RoutingKey { get; private set; } = null!;
    public string PayloadJson { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public OutboxMessage(string messageId, string routingKey, string payloadJson, DateTimeOffset createdAt)
    { Id = Guid.NewGuid(); MessageId = messageId; RoutingKey = routingKey; PayloadJson = payloadJson; CreatedAt = createdAt; }
    public void MarkPublished(DateTimeOffset at) { PublishedAt = at; LastError = null; }
    public void MarkAttemptFailed(string error) { Attempts++; LastError = error.Length > 2000 ? error[..2000] : error; }
}

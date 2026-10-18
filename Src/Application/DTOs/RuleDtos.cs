using System.Text.Json;

namespace Application.DTOs;

public sealed record CreateRuleRequest(string Name, bool Enabled, RuleDefinitionDto Definition);
public sealed record RuleDefinitionDto(List<ConditionDto> All, List<ActionDto> Actions);
public sealed record ConditionDto(string Field, string Operator, JsonElement Value);
public sealed record ActionDto(string Type, string RoutingKey, JsonElement Payload);
public sealed record RuleResponse(Guid Id, string DeviceId, string Name, bool Enabled, int Version);
public sealed record RuleSnapshot(Guid Id, string DeviceId, string Name, bool Enabled, int Version,
    string LuaScript, string ActionsJson);
public sealed record TelemetryMessage(string EventId, Dictionary<string, JsonElement> Properties, DateTimeOffset? Timestamp);
public sealed record ActionEnvelope(string MessageId, string EventId, string DeviceId, Guid RuleId,
    int RuleVersion, string ActionType, JsonElement Payload, DateTimeOffset OccurredAt);

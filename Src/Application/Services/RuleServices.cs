using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;
using FluentValidation;

namespace Application.Services;

// a service takes dep as interface (easy for mocking) in constructor like 
// passing IMessageBroker or IJobq or IRuleRepository to CreateRuleService
public sealed class CreateRuleService(IRuleRepository rules, IRuleCompiler compiler,
    IRuleCache cache, IUnitOfWork uow)
{
    private readonly CreateRuleValidator _validator = new();

    public async Task<RuleResponse> ExecuteAsync(string deviceId, CreateRuleRequest request, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(request, ct);
        var lua = compiler.Compile(request.Definition);
        var definitionJson = JsonSerializer.Serialize(request.Definition);
        var actionsJson = JsonSerializer.Serialize(request.Definition.Actions);
        var rule = Rule.Create(deviceId, request.Name, definitionJson, lua, actionsJson, DateTimeOffset.UtcNow);
        if (!request.Enabled) rule.SetEnabled(false, DateTimeOffset.UtcNow);
        await uow.ExecuteInTransactionAsync(async token => await rules.AddAsync(rule, token), ct);
        await cache.InvalidateDeviceAsync(deviceId, ct);
        return new RuleResponse(rule.Id, rule.DeviceId, rule.Name, rule.Enabled, rule.Version);
    }
}

public sealed class EvaluateDeviceService(IRuleRepository rules, IProcessedEventRepository processed,
    IOutboxRepository outbox, IRuleCache cache, IRuleEvaluator evaluator, IUnitOfWork uow)
{
    public async Task ExecuteAsync(string deviceId, TelemetryMessage telemetry, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(telemetry.EventId))
            throw new ArgumentException("Telemetry eventId is required.");
        if (await processed.ExistsAsync(telemetry.EventId, ct)) return;

        var snapshots = await cache.GetActiveRulesAsync(deviceId, ct);
        if (snapshots is null)
        {
            var entities = await rules.GetByDeviceAsync(deviceId, true, ct);
            snapshots = entities.Select(r => new RuleSnapshot(r.Id, r.DeviceId, r.Name,
                r.Enabled, r.Version, r.LuaScript, r.ActionsJson)).ToArray();
            await cache.SetActiveRulesAsync(deviceId, snapshots, ct);
        }

        await uow.ExecuteInTransactionAsync(async token =>
        {
            // Unique DB key is the final protection against concurrent duplicate MQTT deliveries.
            if (await processed.ExistsAsync(telemetry.EventId, token)) return;
            await processed.AddAsync(ProcessedTelemetryEvent.Create(telemetry.EventId, deviceId, DateTimeOffset.UtcNow), token);

            foreach (var rule in snapshots.Where(x => x.Enabled))
            {
                bool matched;
                try { matched = evaluator.Evaluate(rule.LuaScript, telemetry.Properties); }
                catch (Exception ex) { throw new InvalidOperationException($"Rule {rule.Id} evaluation failed.", ex); }
                if (!matched) continue;

                var actions = JsonSerializer.Deserialize<List<ActionDto>>(rule.ActionsJson) ?? [];
                foreach (var action in actions)
                {
                    var messageId = Guid.NewGuid().ToString("N");
                    var envelope = new ActionEnvelope(messageId, telemetry.EventId, deviceId,
                        rule.Id, rule.Version, action.Type, action.Payload, DateTimeOffset.UtcNow);
                    await outbox.AddAsync(new OutboxItem(Guid.NewGuid(), messageId, action.RoutingKey,
                        JsonSerializer.Serialize(envelope), DateTimeOffset.UtcNow, null), token);
                }
            }
        }, ct);
    }
}

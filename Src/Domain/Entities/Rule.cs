namespace Domain.Entities;

public sealed class Rule
{
    private Rule() { } // EF Core

    public Guid Id { get; private set; }
    public string DeviceId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool Enabled { get; private set; }
    public int Version { get; private set; }
    public string DefinitionJson { get; private set; } = null!;
    public string LuaScript { get; private set; } = null!;
    public string ActionsJson { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Rule Create(string deviceId, string name, string definitionJson,
        string luaScript, string actionsJson, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) throw new ArgumentException("DeviceId is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Rule name is required.");
        if (string.IsNullOrWhiteSpace(luaScript)) throw new ArgumentException("Compiled script is required.");
        return new Rule { Id = Guid.NewGuid(), DeviceId = deviceId.Trim(), Name = name.Trim(), Enabled = true,
            Version = 1, DefinitionJson = definitionJson, LuaScript = luaScript, ActionsJson = actionsJson,
            CreatedAt = now, UpdatedAt = now };
    }

    public void Update(string name, string definitionJson, string luaScript, string actionsJson, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Rule name is required.");
        Name = name.Trim(); DefinitionJson = definitionJson; LuaScript = luaScript;
        ActionsJson = actionsJson; Version++; UpdatedAt = now;
    }

    public void SetEnabled(bool enabled, DateTimeOffset now)
    { Enabled = enabled; Version++; UpdatedAt = now; }
}

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Application.DTOs;
using Application.Interfaces;

namespace Infrastructure.Scripting;

public sealed class LuaRuleCompiler : IRuleCompiler
{
    private static readonly Regex FieldPattern = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
    public string Compile(RuleDefinitionDto definition)
    {
        if (definition.All.Count == 0) throw new ArgumentException("At least one condition is required.");
        var expressions = definition.All.Select(CompileCondition);
        return "return function(ctx) return (" + string.Join(" and ", expressions) + ") end";
    }
    private static string EscapeLuaString(string value) => value
        .Replace("\\", "\\\\")
        .Replace("\"", "\\\"")
        .Replace("\r", "\\r")
        .Replace("\n", "\\n");

    private static string CompileCondition(ConditionDto condition)
    {
        if (!FieldPattern.IsMatch(condition.Field)) throw new ArgumentException($"Invalid field name '{condition.Field}'.");
        if (!new[] { ">", ">=", "<", "<=", "==", "!=" }.Contains(condition.Operator))
            throw new ArgumentException("Unsupported operator.");
        var value = condition.Value.ValueKind switch
        {
            JsonValueKind.Number => condition.Value.GetDouble().ToString("R", CultureInfo.InvariantCulture),
            JsonValueKind.String => "\"" + EscapeLuaString(condition.Value.GetString() ?? string.Empty) + "\"",
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => throw new ArgumentException("Unsupported condition value type.")
        };
        var op = condition.Operator == "!=" ? "~=" : condition.Operator;
        return $"ctx.{condition.Field} ~= nil and ctx.{condition.Field} {op} {value}";
    }
}

using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public sealed class CreateRuleValidator : AbstractValidator<CreateRuleRequest>
{
    public CreateRuleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Definition).NotNull();
        RuleFor(x => x.Definition.All).NotNull().Must(x => x.Count is > 0 and <= 50)
            .WithMessage("Provide between 1 and 50 conditions.");
        RuleFor(x => x.Definition.Actions).NotNull().Must(x => x.Count is > 0 and <= 10)
            .WithMessage("Provide between 1 and 10 actions.");
        RuleForEach(x => x.Definition.All).ChildRules(c =>
        {
            c.RuleFor(x => x.Field).NotEmpty().MaximumLength(80);
            c.RuleFor(x => x.Operator).Must(x => new[] { ">", ">=", "<", "<=", "==", "!=" }.Contains(x))
                .WithMessage("Unsupported operator.");
            c.RuleFor(x => x.Value).Must(v => v.ValueKind is System.Text.Json.JsonValueKind.Number or System.Text.Json.JsonValueKind.String or System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False)
                .WithMessage("Condition values must be a number, string, or boolean.");
        });
        RuleForEach(x => x.Definition.Actions).ChildRules(a =>
        {
            a.RuleFor(x => x.Type).NotEmpty().MaximumLength(80);
            a.RuleFor(x => x.RoutingKey).NotEmpty().MaximumLength(180)
                .Matches("^[a-zA-Z0-9._-]+$");
            a.RuleFor(x => x.Payload).Must(p => p.ValueKind is System.Text.Json.JsonValueKind.Object or System.Text.Json.JsonValueKind.Array or System.Text.Json.JsonValueKind.String or System.Text.Json.JsonValueKind.Number or System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False or System.Text.Json.JsonValueKind.Null);
        });
    }
}

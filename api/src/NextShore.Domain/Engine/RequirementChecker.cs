using System.Text.Json;
using NextShore.Domain.Enums;
using NextShore.Domain.Rules;

namespace NextShore.Domain.Engine;

public sealed record RequirementOutcome(
    Guid RequirementId,
    string Label,
    bool Met,
    string Current,
    string Target,
    bool IsMandatory,
    Fixability Fixability,
    string FixHint,
    int? FixMonths,
    RuleSource Source);

/// <summary>Evaluates <c>profile[field] &lt;operator&gt; value</c> for each requirement (VISA-RULES-SPEC.md §1).</summary>
public static class RequirementChecker
{
    public const string NotProvided = "Not provided";
    public const string AnswerInProfile = "Add this to your profile";

    public static IReadOnlyList<RequirementOutcome> CheckAll(
        PathwayRule pathway, NormalizedProfile profile, ExchangeRates rates) =>
        pathway.Requirements.Select(rule => Check(rule, profile, rates)).ToList();

    public static RequirementOutcome Check(RequirementRule rule, NormalizedProfile profile, ExchangeRates rates)
    {
        using var document = rule.Value is null ? null : JsonDocument.Parse(rule.Value);
        var expected = document?.RootElement;
        var target = TargetText(rule, expected);

        if (profile.Get(rule.Field) is not { } value)
        {
            // The user can fix a missing answer by filling it in.
            return Outcome(rule, met: false, NotProvided, target, Fixability.Easy, AnswerInProfile, 0);
        }

        var met = Evaluate(rule, value, expected, rates);
        return Outcome(rule, met, ValueText.Describe(value, rule.Unit, rates), target, rule.Fixability, rule.FixHint, rule.FixMonths);
    }

    private static RequirementOutcome Outcome(
        RequirementRule rule, bool met, string current, string target, Fixability fixability, string fixHint, int? fixMonths) =>
        new(rule.Id, rule.Label, met, current, target, rule.IsMandatory, fixability, fixHint, fixMonths, rule.Source);

    private static bool Evaluate(RequirementRule rule, ProfileValue value, JsonElement? expected, ExchangeRates rates)
    {
        switch (rule.Operator)
        {
            case RequirementOperator.Exists:
                return true;
            case RequirementOperator.True:
                return value is ProfileValue.Flag flag
                    ? flag.Value
                    : throw RuleError(rule, $"'true' needs a yes/no field, got {value.GetType().Name}");
        }

        var json = expected ?? throw RuleError(rule, "value is missing");
        return rule.Operator switch
        {
            RequirementOperator.Eq => AreEqual(rule, value, json, rates),
            RequirementOperator.Neq => !AreEqual(rule, value, json, rates),
            RequirementOperator.In => Elements(rule, json).Any(e => AreEqual(rule, value, e, rates)),
            RequirementOperator.NotIn => !Elements(rule, json).Any(e => AreEqual(rule, value, e, rates)),
            RequirementOperator.Gte => Compare(rule, value, json, rates) >= 0,
            RequirementOperator.Gt => Compare(rule, value, json, rates) > 0,
            RequirementOperator.Lte => Compare(rule, value, json, rates) <= 0,
            RequirementOperator.Lt => Compare(rule, value, json, rates) < 0,
            _ => throw RuleError(rule, $"unsupported operator {rule.Operator}"),
        };
    }

    private static bool AreEqual(RequirementRule rule, ProfileValue value, JsonElement json, ExchangeRates rates) =>
        value switch
        {
            ProfileValue.Number n => n.Value == NumberOf(rule, json),
            ProfileValue.Money m => InRuleCurrency(m, rule.Unit, rates) == NumberOf(rule, json),
            ProfileValue.Text t => string.Equals(t.Value, TextOf(rule, json), StringComparison.OrdinalIgnoreCase),
            ProfileValue.Flag f => f.Value == FlagOf(rule, json),
            ProfileValue.Ordinal o => o.Rank == RankOf(rule, o.Scale, json),
            ProfileValue.Below => false,
            _ => throw RuleError(rule, $"cannot compare {value.GetType().Name}"),
        };

    /// <summary>Sign of value − rule value.</summary>
    private static int Compare(RequirementRule rule, ProfileValue value, JsonElement json, ExchangeRates rates) =>
        value switch
        {
            ProfileValue.Number n => n.Value.CompareTo(NumberOf(rule, json)),
            ProfileValue.Money m => InRuleCurrency(m, rule.Unit, rates).CompareTo(NumberOf(rule, json)),
            ProfileValue.Ordinal o => o.Rank.CompareTo(RankOf(rule, o.Scale, json)),
            ProfileValue.Below => -1,
            _ => throw RuleError(rule, $"{rule.Operator} cannot order a {value.GetType().Name.ToLowerInvariant()} value"),
        };

    private static decimal InRuleCurrency(ProfileValue.Money money, string? unit, ExchangeRates rates) =>
        rates.Convert(money.Amount, money.Currency, unit ?? money.Currency);

    private static IEnumerable<JsonElement> Elements(RequirementRule rule, JsonElement json) =>
        json.ValueKind == JsonValueKind.Array
            ? json.EnumerateArray()
            : throw RuleError(rule, $"{rule.Operator} needs an array value");

    private static decimal NumberOf(RequirementRule rule, JsonElement json) =>
        json.ValueKind == JsonValueKind.Number ? json.GetDecimal() : throw RuleError(rule, $"expected a number, got {json.GetRawText()}");

    private static string TextOf(RequirementRule rule, JsonElement json) =>
        json.ValueKind == JsonValueKind.String ? json.GetString()! : throw RuleError(rule, $"expected text, got {json.GetRawText()}");

    private static bool FlagOf(RequirementRule rule, JsonElement json) => json.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => throw RuleError(rule, $"expected true or false, got {json.GetRawText()}"),
    };

    private static int RankOf(RequirementRule rule, OrdinalScale scale, JsonElement json) =>
        scale.RankOf(TextOf(rule, json)) ?? throw RuleError(rule, $"'{json.GetString()}' is not a {scale.Name} level");

    private static InvalidOperationException RuleError(RequirementRule rule, string reason) =>
        new($"Requirement '{rule.Label}' ({rule.Field} {rule.Operator}) cannot be evaluated: {reason}.");

    // ---- Display ----

    private static string TargetText(RequirementRule rule, JsonElement? expected)
    {
        switch (rule.Operator)
        {
            case RequirementOperator.Exists:
                return "Provided";
            case RequirementOperator.True:
                return "Yes";
            case RequirementOperator.In or RequirementOperator.NotIn when expected is { ValueKind: JsonValueKind.Array } list:
                var items = string.Join(", ", list.EnumerateArray().Select(e => FormatJson(e, rule.Unit)));
                return rule.Operator == RequirementOperator.In ? $"one of {items}" : $"not {items}";
        }

        var value = expected is { } json ? FormatJson(json, rule.Unit) : "?";
        return rule.Operator switch
        {
            RequirementOperator.Neq => $"not {value}",
            RequirementOperator.Gt => $"more than {value}",
            RequirementOperator.Lte => $"{value} or less",
            RequirementOperator.Lt => $"under {value}",
            _ => value,
        };
    }

    private static string FormatJson(JsonElement json, string? unit) => json.ValueKind switch
    {
        JsonValueKind.Number => ValueText.WithUnit(ValueText.Number(json.GetDecimal()), unit),
        JsonValueKind.String => ValueText.WithUnit(json.GetString()!, unit),
        JsonValueKind.True => "Yes",
        JsonValueKind.False => "No",
        _ => json.GetRawText(),
    };
}

using System.Text.Json;
using FluentValidation;
using NextShore.Application.Common;
using NextShore.Domain.Enums;

namespace NextShore.Application.Admin;

/// <summary>Columns that may be listed as unverified (<c>TODO_VERIFY</c>), per rule table. Matches the seed parser.</summary>
public static class VerifiableFields
{
    public static readonly IReadOnlySet<string> Pathway = new HashSet<string> { "pass_mark", "cutoff", "cutoff_as_of", "typical_duration_months" };
    public static readonly IReadOnlySet<string> Requirement = new HashSet<string> { "value" };
    public static readonly IReadOnlySet<string> PointsFactor = new HashSet<string> { "min", "max", "match_value", "points", "max_points_for_factor" };
}

internal static class AdminRules
{
    public static bool IsHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    public static void Sourced<T>(
        AbstractValidator<T> v,
        Func<T, string> sourceUrl,
        Func<T, DateOnly?> verifiedAt,
        Func<T, IReadOnlyList<string>> unverified,
        IReadOnlySet<string> allowed,
        Func<T, string, bool> hasValue,
        TimeProvider clock)
    {
        v.RuleFor(x => sourceUrl(x)).Must(IsHttpUrl).OverridePropertyName("sourceUrl").WithMessage("Must be an absolute http(s) URL.");
        v.RuleFor(x => verifiedAt(x))
            .Must(d => d is null || d <= DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
            .OverridePropertyName("verifiedAt").WithMessage("Can't be in the future.");
        v.RuleFor(x => unverified(x)).NotNull().OverridePropertyName("unverifiedFields");
        v.RuleForEach(x => unverified(x))
            .Must(allowed.Contains).OverridePropertyName("unverifiedFields")
            .WithMessage(f => $"Allowed: {string.Join(", ", allowed.Order())}.");
        v.RuleFor(x => x)
            .Must(x => unverified(x) is null || unverified(x).All(f => !hasValue(x, f)))
            .OverridePropertyName("unverifiedFields")
            .WithMessage("An unverified field must be empty: clear the value or remove it from unverified fields.");
    }
}

public sealed class PathwayUpdateValidator : AbstractValidator<PathwayUpdateRequest>
{
    public PathwayUpdateValidator(TimeProvider clock)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Type).Must(t => EnumText.TryParse<PathwayType>(t, out _))
            .WithMessage($"One of: {string.Join(", ", EnumText.All<PathwayType>())}.");
        RuleFor(x => x.Audience).NotEmpty();
        RuleForEach(x => x.Audience).Must(a => EnumText.TryParse<Audience>(a, out _))
            .WithMessage($"One of: {string.Join(", ", EnumText.All<Audience>())}.");
        RuleFor(x => x.PassMark).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Cutoff).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TypicalDurationMonths).GreaterThanOrEqualTo(0);
        AdminRules.Sourced(this, x => x.SourceUrl, x => x.VerifiedAt, x => x.UnverifiedFields, VerifiableFields.Pathway,
            (x, f) => f switch
            {
                "pass_mark" => x.PassMark is not null,
                "cutoff" => x.Cutoff is not null,
                "cutoff_as_of" => x.CutoffAsOf is not null,
                "typical_duration_months" => x.TypicalDurationMonths is not null,
                _ => false,
            }, clock);
    }
}

public sealed class RequirementValidator : AbstractValidator<RequirementRequest>
{
    private static readonly RequirementOperator[] NoValue = [RequirementOperator.Exists, RequirementOperator.True];
    private static readonly RequirementOperator[] ListValue = [RequirementOperator.In, RequirementOperator.NotIn];

    public RequirementValidator(TimeProvider clock)
    {
        RuleFor(x => x.Field).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FixHint).NotNull().MaximumLength(300);
        RuleFor(x => x.FixMonths).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Operator).Must(o => EnumText.TryParse<RequirementOperator>(o, out _))
            .WithMessage($"One of: {string.Join(", ", EnumText.All<RequirementOperator>())}.");
        RuleFor(x => x.Fixability).Must(f => EnumText.TryParse<Fixability>(f, out _))
            .WithMessage($"One of: {string.Join(", ", EnumText.All<Fixability>())}.");
        RuleFor(x => x.Value).Must((x, value) => ValueFits(x, value)).WithMessage(x => ValueMessage(x));
        AdminRules.Sourced(this, x => x.SourceUrl, x => x.VerifiedAt, x => x.UnverifiedFields, VerifiableFields.Requirement,
            (x, f) => f == "value" && HasValue(x.Value), clock);
    }

    private static bool HasValue(JsonElement? value) => value is { ValueKind: not (JsonValueKind.Null or JsonValueKind.Undefined) };

    private static bool ValueFits(RequirementRequest x, JsonElement? value)
    {
        if (!EnumText.TryParse<RequirementOperator>(x.Operator, out var op))
        {
            return true; // reported on Operator
        }

        if (!HasValue(value))
        {
            return NoValue.Contains(op) || (x.UnverifiedFields?.Contains("value") ?? false);
        }

        var kind = value!.Value.ValueKind;
        return ListValue.Contains(op)
            ? kind == JsonValueKind.Array && value.Value.EnumerateArray().All(IsScalar)
            : IsScalar(value.Value);
    }

    private static bool IsScalar(JsonElement e) =>
        e.ValueKind is JsonValueKind.Number or JsonValueKind.String or JsonValueKind.True or JsonValueKind.False;

    private static string ValueMessage(RequirementRequest x) =>
        EnumText.TryParse<RequirementOperator>(x.Operator, out var op) && ListValue.Contains(op)
            ? "in / not_in need an array of numbers or strings."
            : "Needs a number, string or boolean (or list `value` as unverified).";
}

public sealed class PointsFactorValidator : AbstractValidator<PointsFactorRequest>
{
    public PointsFactorValidator(TimeProvider clock)
    {
        RuleFor(x => x.Factor).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Variant).Must(v => EnumText.TryParse<PointsVariant>(v, out _))
            .WithMessage($"One of: {string.Join(", ", EnumText.All<PointsVariant>())}.");
        RuleFor(x => x.Max).GreaterThanOrEqualTo(x => x.Min).When(x => x.Min is not null && x.Max is not null);
        RuleFor(x => x.Points).NotNull().When(x => !(x.UnverifiedFields?.Contains("points") ?? false))
            .WithMessage("Set the points, or list `points` as unverified.");
        RuleFor(x => x.MaxPointsForFactor).GreaterThanOrEqualTo(0);
        AdminRules.Sourced(this, x => x.SourceUrl, x => x.VerifiedAt, x => x.UnverifiedFields, VerifiableFields.PointsFactor,
            (x, f) => f switch
            {
                "min" => x.Min is not null,
                "max" => x.Max is not null,
                "match_value" => x.MatchValue is not null,
                "points" => x.Points is not null,
                "max_points_for_factor" => x.MaxPointsForFactor is not null,
                _ => false,
            }, clock);
    }
}

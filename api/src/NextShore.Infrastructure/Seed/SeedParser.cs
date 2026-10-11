using System.Text.Json;
using System.Text.Json.Serialization;
using NextShore.Domain.Entities;
using NextShore.Domain.Enums;
using NextShore.Domain.Rules;

namespace NextShore.Infrastructure.Seed;

public sealed record SeededPathway(Pathway Pathway, string? NextPathwaySlug);

public sealed record ParsedCountryFile(Country Country, IReadOnlyList<SeededPathway> Pathways);

/// <summary>Turns seed JSON into draft entities. <c>TODO_VERIFY</c> values become null + an entry in UnverifiedFields.</summary>
public static class SeedParser
{
    public const string LanguageMappingsFileName = "language-mappings.json";

    private static readonly string[] Skills = ["listening", "reading", "writing", "speaking"];

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ParsedCountryFile ParseCountryFile(string json, string source)
    {
        var file = Deserialize<CountrySeedFile>(json, source);
        var country = new Country
        {
            Code = file.Country.Code.ToUpperInvariant(),
            Name = file.Country.Name,
            OccupationSystem = file.Country.OccupationSystem,
            LanguageScale = file.Country.LanguageScale,
        };

        var pathways = file.Pathways
            .Select((seed, i) => ToPathway(seed, $"{source}: pathways[{i}] ({seed.Slug})"))
            .ToList();

        var duplicate = pathways.GroupBy(p => p.Pathway.Slug).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new SeedException($"{source}: duplicate pathway slug '{duplicate.Key}'.");
        }

        return new ParsedCountryFile(country, pathways);
    }

    public static IReadOnlyList<LanguageScoreMapping> ParseLanguageMappings(string json, string source)
    {
        var file = Deserialize<LanguageMappingsSeedFile>(json, source);
        var mappings = new List<LanguageScoreMapping>();

        foreach (var (group, g) in file.Groups.Select((group, g) => (group, g)))
        {
            foreach (var (level, l) in group.Levels.Select((level, l) => (level, l)))
            {
                var test = group.TestVariant is null ? group.Test : $"{group.Test} ({group.TestVariant})";
                var path = $"{source}: groups[{g}].levels[{l}] ({test} → {group.TargetScale} {level.TargetLevel})";
                var unknown = level.MinScores.Keys.Except(Skills).ToList();
                var missing = Skills.Except(level.MinScores.Keys).ToList();
                if (unknown.Count > 0 || missing.Count > 0)
                {
                    throw new SeedException(
                        $"{path}: minScores needs exactly {string.Join(", ", Skills)} " +
                        $"(unknown: [{string.Join(", ", unknown)}], missing: [{string.Join(", ", missing)}]).");
                }

                foreach (var skill in Skills)
                {
                    var mapping = new LanguageScoreMapping
                    {
                        Test = group.Test,
                        TestVariant = group.TestVariant,
                        Skill = skill,
                        TargetScale = group.TargetScale,
                        TargetLevel = level.TargetLevel,
                        SourceUrl = group.SourceUrl,
                        VerifiedAt = group.VerifiedAt,
                    };
                    var values = new VerifiableReader($"{path}.minScores", mapping.UnverifiedFields);
                    mapping.MinScore = values.Decimal(level.MinScores[skill], "min_score");
                    mappings.Add(mapping);
                }
            }
        }

        return mappings;
    }

    private static SeededPathway ToPathway(PathwaySeed seed, string path)
    {
        var pathway = new Pathway
        {
            Slug = seed.Slug,
            Name = seed.Name,
            Type = ParseEnum<PathwayType>(seed.Type, $"{path}.type"),
            Audience = seed.Audience.Select((a, i) => ParseEnum<Audience>(a, $"{path}.audience[{i}]")).ToList(),
            IsPointsBased = seed.IsPointsBased,
            Summary = seed.Summary,
            SourceUrl = seed.SourceUrl,
            VerifiedAt = seed.VerifiedAt,
        };

        var values = new VerifiableReader(path, pathway.UnverifiedFields);
        pathway.PassMark = values.Int(seed.PassMark, "pass_mark");
        pathway.Cutoff = values.Int(seed.Cutoff, "cutoff");
        pathway.CutoffAsOf = values.Date(seed.CutoffAsOf, "cutoff_as_of");
        pathway.TypicalDurationMonths = values.Int(seed.TypicalDurationMonths, "typical_duration_months");

        pathway.Requirements = (seed.Requirements ?? [])
            .Select((r, i) => ToRequirement(r, seed, $"{path}.requirements[{i}]"))
            .ToList();
        pathway.PointsFactors = (seed.PointsFactors ?? [])
            .Select((f, i) => ToPointsFactor(f, seed, $"{path}.pointsFactors[{i}]"))
            .ToList();
        pathway.Occupations = (seed.Occupations ?? [])
            .Select((o, i) => ToOccupation(o, seed, $"{path}.occupations[{i}]"))
            .ToList();

        if (pathway.IsPointsBased && pathway.PointsFactors.Count == 0)
        {
            throw new SeedException($"{path}: isPointsBased is true but there are no pointsFactors.");
        }

        return new SeededPathway(pathway, seed.NextPathwaySlug);
    }

    private static Requirement ToRequirement(RequirementSeed seed, PathwaySeed pathway, string path)
    {
        var requirement = new Requirement
        {
            Field = seed.Field,
            Operator = ParseEnum<RequirementOperator>(seed.Operator, $"{path}.operator"),
            Unit = seed.Unit,
            IsMandatory = seed.IsMandatory,
            Fixability = ParseEnum<Fixability>(seed.Fixability, $"{path}.fixability"),
            FixHint = seed.FixHint,
            FixMonths = seed.FixMonths,
            Label = seed.Label,
            SourceUrl = seed.SourceUrl ?? pathway.SourceUrl,
            VerifiedAt = seed.VerifiedAt,
        };
        requirement.Value = new VerifiableReader(path, requirement.UnverifiedFields).Json(seed.Value, "value");
        return requirement;
    }

    private static PointsFactor ToPointsFactor(PointsFactorSeed seed, PathwaySeed pathway, string path)
    {
        var factor = new PointsFactor
        {
            Factor = seed.Factor,
            Variant = ParseEnum<PointsVariant>(seed.Variant, $"{path}.variant"),
            SourceUrl = seed.SourceUrl ?? pathway.SourceUrl,
            VerifiedAt = seed.VerifiedAt,
        };

        var values = new VerifiableReader(path, factor.UnverifiedFields);
        factor.Min = values.Decimal(seed.Min, "min");
        factor.Max = values.Decimal(seed.Max, "max");
        factor.MatchValue = values.Text(seed.MatchValue, "match_value");
        factor.Points = values.Int(seed.Points, "points");
        factor.MaxPointsForFactor = values.Int(seed.MaxPointsForFactor, "max_points_for_factor");
        return factor;
    }

    private static OccupationListEntry ToOccupation(OccupationSeed seed, PathwaySeed pathway, string path)
    {
        var occupation = new OccupationListEntry
        {
            Code = seed.Code,
            Title = seed.Title,
            SourceUrl = seed.SourceUrl ?? pathway.SourceUrl,
            VerifiedAt = seed.VerifiedAt,
        };
        occupation.IsEligible =
            new VerifiableReader(path, occupation.UnverifiedFields).Bool(seed.IsEligible, "is_eligible") ?? false;
        return occupation;
    }

    private static T Deserialize<T>(string json, string source)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, Options)
                ?? throw new SeedException($"{source}: file is empty.");
        }
        catch (JsonException ex)
        {
            throw new SeedException($"{source}: {ex.Message}", ex);
        }
    }

    private static TEnum ParseEnum<TEnum>(string value, string path)
        where TEnum : struct, Enum
    {
        var names = SnakeCase.EnumNames<TEnum>();
        foreach (var (enumValue, name) in names)
        {
            if (name == value)
            {
                return enumValue;
            }
        }

        throw new SeedException($"{path}: '{value}' is not one of {string.Join(", ", names.Values)}.");
    }

    /// <summary>Reads values that may be "TODO_VERIFY"; records those fields (snake_case) as unverified.</summary>
    private sealed class VerifiableReader(string path, List<string> unverified)
    {
        public int? Int(JsonElement? element, string field) =>
            Read<int>(element, field, "number",
                e => e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var v) ? v : null);

        public decimal? Decimal(JsonElement? element, string field) =>
            Read<decimal>(element, field, "number",
                e => e.ValueKind == JsonValueKind.Number && e.TryGetDecimal(out var v) ? v : null);

        public DateOnly? Date(JsonElement? element, string field) =>
            Read<DateOnly>(element, field, "date (yyyy-MM-dd)",
                e => e.ValueKind == JsonValueKind.String && DateOnly.TryParse(e.GetString(), out var v) ? v : null);

        public bool? Bool(JsonElement? element, string field) =>
            Read<bool>(element, field, "boolean",
                e => e.ValueKind is JsonValueKind.True or JsonValueKind.False ? e.GetBoolean() : null);

        public string? Text(JsonElement? element, string field)
        {
            if (!TryGetVerified(element, field, out var e))
            {
                return null;
            }

            return e.ValueKind == JsonValueKind.String ? e.GetString() : throw Invalid(field, "string", e);
        }

        /// <summary>Any JSON value, kept raw. Unverified if TODO_VERIFY appears anywhere inside it.</summary>
        public string? Json(JsonElement? element, string field)
        {
            if (element is not { ValueKind: not JsonValueKind.Null } e)
            {
                return null;
            }

            if (ContainsTodo(e))
            {
                unverified.Add(field);
                return null;
            }

            return e.GetRawText();
        }

        private T? Read<T>(JsonElement? element, string field, string expected, Func<JsonElement, T?> parse)
            where T : struct
        {
            if (!TryGetVerified(element, field, out var e))
            {
                return null;
            }

            return parse(e) ?? throw Invalid(field, expected, e);
        }

        /// <summary>False when the value is absent, null or TODO_VERIFY (which is recorded as unverified).</summary>
        private bool TryGetVerified(JsonElement? element, string field, out JsonElement value)
        {
            value = element.GetValueOrDefault();
            if (element is not { ValueKind: not JsonValueKind.Null })
            {
                return false;
            }

            if (IsTodo(value))
            {
                unverified.Add(field);
                return false;
            }

            return true;
        }

        private SeedException Invalid(string field, string expected, JsonElement e) =>
            new($"{path}.{field}: expected a {expected} or \"{RuleValues.TodoVerify}\", got {e.GetRawText()}.");

        private static bool IsTodo(JsonElement e) =>
            e.ValueKind == JsonValueKind.String && e.GetString() == RuleValues.TodoVerify;

        private static bool ContainsTodo(JsonElement e) => e.ValueKind switch
        {
            JsonValueKind.String => IsTodo(e),
            JsonValueKind.Array => e.EnumerateArray().Any(ContainsTodo),
            JsonValueKind.Object => e.EnumerateObject().Any(p => ContainsTodo(p.Value)),
            _ => false,
        };
    }
}

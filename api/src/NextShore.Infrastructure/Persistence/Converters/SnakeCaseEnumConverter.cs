using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NextShore.Infrastructure.Persistence.Converters;

/// <summary>Stores enums as snake_case text, e.g. <c>PostStudyWork</c> ↔ <c>post_study_work</c>.</summary>
public sealed class SnakeCaseEnumConverter<TEnum>() : ValueConverter<TEnum, string>(
    value => ToDb[value],
    value => FromDb[value])
    where TEnum : struct, Enum
{
    private static readonly IReadOnlyDictionary<TEnum, string> ToDb = SnakeCase.EnumNames<TEnum>();

    private static readonly Dictionary<string, TEnum> FromDb =
        ToDb.ToDictionary(pair => pair.Value, pair => pair.Key);
}

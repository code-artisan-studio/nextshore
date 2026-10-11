using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NextShore.Infrastructure.Persistence.Converters;

/// <summary>Stores enums as snake_case text, e.g. <c>PostStudyWork</c> ↔ <c>post_study_work</c>.</summary>
public sealed class SnakeCaseEnumConverter<TEnum>() : ValueConverter<TEnum, string>(
    value => ToDb[value],
    value => FromDb[value])
    where TEnum : struct, Enum
{
    private static readonly Dictionary<TEnum, string> ToDb =
        Enum.GetValues<TEnum>().ToDictionary(value => value, value => ToSnakeCase(value.ToString()));

    private static readonly Dictionary<string, TEnum> FromDb =
        ToDb.ToDictionary(pair => pair.Value, pair => pair.Key);

    internal static string ToSnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(name[i]));
        }

        return builder.ToString();
    }
}

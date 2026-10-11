using System.Text;

namespace NextShore.Infrastructure;

internal static class SnakeCase
{
    public static string From(string name)
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

    public static IReadOnlyDictionary<TEnum, string> EnumNames<TEnum>()
        where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().ToDictionary(value => value, value => From(value.ToString()));
}

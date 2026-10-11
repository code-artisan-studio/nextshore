using System.Text;

namespace NextShore.Application.Common;

/// <summary>Enum values as they appear in the API: snake_case, e.g. <c>NearEligible</c> → <c>near_eligible</c>.</summary>
public static class EnumText
{
    public static string Of<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var name = value.ToString();
        var builder = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsDigit(name[i - 1]))
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(name[i]));
        }

        return builder.ToString();
    }
}

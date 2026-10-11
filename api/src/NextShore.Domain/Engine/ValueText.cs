using System.Globalization;

namespace NextShore.Domain.Engine;

/// <summary>Display text for profile values, e.g. "CLB 7", "5 years", "USD 20,000 (≈ CAD 27,400)".</summary>
public static class ValueText
{
    public static string Describe(ProfileValue value, string? unit = null, ExchangeRates? rates = null) => value switch
    {
        ProfileValue.Number n => WithUnit(Number(n.Value), unit),
        ProfileValue.Text t => t.Value,
        ProfileValue.Flag f => f.Value ? "Yes" : "No",
        ProfileValue.Ordinal o => WithUnit(o.Label, unit),
        ProfileValue.Below b => $"Below {b.Scale} {b.LowestLabel}",
        ProfileValue.Money m when unit is null || unit == m.Currency || rates is null => $"{m.Currency} {Number(m.Amount)}",
        ProfileValue.Money m =>
            $"{m.Currency} {Number(m.Amount)} (≈ {unit} {Number(Math.Round(rates.Convert(m.Amount, m.Currency, unit)))})",
        _ => value.ToString() ?? string.Empty,
    };

    public static string WithUnit(string value, string? unit) => unit switch
    {
        null => value,
        "years" => value == "1" ? "1 year" : $"{value} years",
        _ => $"{unit} {value}",
    };

    public static string Number(decimal value) =>
        value.ToString(Math.Abs(value) >= 1000 ? "#,0.##" : "0.##", CultureInfo.InvariantCulture);
}

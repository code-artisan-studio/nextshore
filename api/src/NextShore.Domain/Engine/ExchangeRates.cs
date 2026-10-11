namespace NextShore.Domain.Engine;

/// <summary>Units of each currency per 1 USD, e.g. <c>CAD → 1.37</c>. Supplied from configuration.</summary>
public sealed record ExchangeRates(IReadOnlyDictionary<string, decimal> PerUsd)
{
    public const string Usd = "USD";

    public decimal Convert(decimal amount, string from, string to) =>
        from == to ? amount : amount / Rate(from) * Rate(to);

    private decimal Rate(string currency) =>
        currency == Usd ? 1m
        : PerUsd.TryGetValue(currency, out var rate) ? rate
        : throw new InvalidOperationException($"No exchange rate configured for {currency}.");
}

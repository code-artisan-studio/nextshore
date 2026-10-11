using NextShore.Domain.Engine;

namespace NextShore.Application.Engine;

/// <summary>Config section <c>Engine</c>.</summary>
public sealed class EngineSettings
{
    public const string Section = "Engine";

    public int NearEligibleMaxGaps { get; set; } = 2;
    public decimal NearEligiblePointsRatio { get; set; } = 0.85m;
    public int StaleAfterDays { get; set; } = 90;

    /// <summary>Units of each currency per 1 USD, e.g. <c>CAD: 1.38</c>. Needed for every currency rules use.</summary>
    public Dictionary<string, decimal> ExchangeRates { get; set; } = [];

    public EngineOptions ToEngineOptions() =>
        new(new GraderOptions(NearEligibleMaxGaps, NearEligiblePointsRatio), StaleAfterDays);

    public ExchangeRates ToExchangeRates() => new(ExchangeRates);
}

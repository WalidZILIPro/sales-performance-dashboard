using SalesDashboard.Application.Contracts.Responses;
using SalesDashboard.Application.Periods;
using SalesDashboard.Domain.Metrics;

namespace SalesDashboard.Application.Mapping;

/// <summary>
/// Calculations run on full-precision decimals; values are rounded once, here, at the API boundary.
/// Money: 2 decimals. Ratios (margin, shares, change): 4 decimals as fractions (0.1234 = 12.34%).
/// </summary>
internal static class Rounding
{
    public static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static decimal? Money(decimal? value) => value is null ? null : Money(value.Value);

    public static decimal Ratio(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    public static decimal? Ratio(decimal? value) => value is null ? null : Ratio(value.Value);

    public static MetricComparisonDto Compare(decimal? current, decimal? previous, bool isRatio = false)
    {
        Func<decimal?, decimal?> round = isRatio ? Ratio : Money;
        return new MetricComparisonDto(
            round(current),
            round(previous),
            round(Change.Absolute(current, previous)),
            Ratio(Change.Relative(current, previous)));
    }

    public static PeriodDto ToDto(this ResolvedPeriod period) => new(
        period.Preset,
        period.Current.From,
        period.Current.To,
        period.Previous.From,
        period.Previous.To,
        period.Current.Days);
}

using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Domain.Common;

namespace SalesDashboard.Application.Periods;

/// <summary>
/// Turns a preset (or custom range) into concrete UTC day ranges plus the previous comparable range.
///
/// Previous-period rule:
/// <list type="bullet">
///   <item>Today / 7 days / 30 days / custom: the immediately preceding window of the same length.</item>
///   <item>This month (month-to-date): the same number of days from the start of the previous month
///     (clamped to that month's end), so 1-24 Sep is compared with 1-24 Aug.</item>
///   <item>Last month: the full calendar month before it.</item>
/// </list>
/// </summary>
public sealed class PeriodResolver(TimeProvider clock) : IPeriodResolver
{
    public ResolvedPeriod Resolve(PeriodQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        return query.Preset switch
        {
            PeriodPreset.Today => Sliding(query.Preset, today, 1),
            PeriodPreset.Last7Days => Sliding(query.Preset, today, 7),
            PeriodPreset.Last30Days => Sliding(query.Preset, today, 30),
            PeriodPreset.ThisMonth => MonthToDate(today),
            PeriodPreset.LastMonth => PreviousCalendarMonth(today),
            PeriodPreset.Custom => Custom(query),
            _ => throw new ArgumentOutOfRangeException(nameof(query), query.Preset, "Unknown period preset."),
        };
    }

    private static ResolvedPeriod Sliding(PeriodPreset preset, DateOnly today, int days) =>
        WithEqualLengthPrevious(preset, new DateRange(today.AddDays(1 - days), today));

    private static ResolvedPeriod MonthToDate(DateOnly today)
    {
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var current = new DateRange(monthStart, today);

        var previousMonthStart = monthStart.AddMonths(-1);
        var previousMonthEnd = monthStart.AddDays(-1);
        var previousEnd = previousMonthStart.AddDays(current.Days - 1);
        if (previousEnd > previousMonthEnd)
        {
            previousEnd = previousMonthEnd;
        }

        return new ResolvedPeriod(PeriodPreset.ThisMonth, current, new DateRange(previousMonthStart, previousEnd));
    }

    private static ResolvedPeriod PreviousCalendarMonth(DateOnly today)
    {
        var thisMonthStart = new DateOnly(today.Year, today.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var monthBeforeStart = lastMonthStart.AddMonths(-1);

        return new ResolvedPeriod(
            PeriodPreset.LastMonth,
            new DateRange(lastMonthStart, thisMonthStart.AddDays(-1)),
            new DateRange(monthBeforeStart, lastMonthStart.AddDays(-1)));
    }

    private static ResolvedPeriod Custom(PeriodQuery query)
    {
        if (query.From is not { } from || query.To is not { } to)
        {
            throw new ArgumentException("A custom period requires both 'from' and 'to'.", nameof(query));
        }

        return WithEqualLengthPrevious(PeriodPreset.Custom, new DateRange(from, to));
    }

    private static ResolvedPeriod WithEqualLengthPrevious(PeriodPreset preset, DateRange current) =>
        new(preset, current, new DateRange(current.From.AddDays(-current.Days), current.From.AddDays(-1)));
}

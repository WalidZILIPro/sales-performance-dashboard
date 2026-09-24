using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Periods;
using SalesDashboard.Domain.Common;
using SalesDashboard.UnitTests.TestSupport;

namespace SalesDashboard.UnitTests.Application;

public class PeriodResolverTests
{
    private static PeriodResolver At(int year, int month, int day) =>
        new(new FixedTimeProvider(new DateTimeOffset(year, month, day, 15, 30, 0, TimeSpan.Zero)));

    private static PeriodResolver Now() => new(new FixedTimeProvider(Build.Now));

    private static DateRange Range(string from, string to) => new(DateOnly.Parse(from), DateOnly.Parse(to));

    [Fact]
    public void Today_is_compared_with_yesterday()
    {
        var period = Now().Resolve(new PeriodQuery { Preset = PeriodPreset.Today });

        period.Current.ShouldBe(Range("2026-09-24", "2026-09-24"));
        period.Previous.ShouldBe(Range("2026-09-23", "2026-09-23"));
    }

    [Fact]
    public void Last_7_days_includes_today_and_is_compared_with_the_7_days_before()
    {
        var period = Now().Resolve(new PeriodQuery { Preset = PeriodPreset.Last7Days });

        period.Current.ShouldBe(Range("2026-09-18", "2026-09-24"));
        period.Previous.ShouldBe(Range("2026-09-11", "2026-09-17"));
    }

    [Fact]
    public void Last_30_days_includes_today_and_is_compared_with_the_30_days_before()
    {
        var period = Now().Resolve(new PeriodQuery { Preset = PeriodPreset.Last30Days });

        period.Current.ShouldBe(Range("2026-08-26", "2026-09-24"));
        period.Current.Days.ShouldBe(30);
        period.Previous.ShouldBe(Range("2026-07-27", "2026-08-25"));
    }

    [Fact]
    public void This_month_is_month_to_date_compared_with_the_same_days_of_the_previous_month()
    {
        var period = Now().Resolve(new PeriodQuery { Preset = PeriodPreset.ThisMonth });

        period.Current.ShouldBe(Range("2026-09-01", "2026-09-24"));
        period.Previous.ShouldBe(Range("2026-08-01", "2026-08-24"));
    }

    [Fact]
    public void This_month_comparison_is_clamped_when_the_previous_month_is_shorter()
    {
        // 1-31 March vs February, which only has 28 days in 2026.
        var period = At(2026, 3, 31).Resolve(new PeriodQuery { Preset = PeriodPreset.ThisMonth });

        period.Current.ShouldBe(Range("2026-03-01", "2026-03-31"));
        period.Previous.ShouldBe(Range("2026-02-01", "2026-02-28"));
    }

    [Fact]
    public void This_month_on_the_first_day_compares_one_day_with_one_day()
    {
        var period = At(2026, 9, 1).Resolve(new PeriodQuery { Preset = PeriodPreset.ThisMonth });

        period.Current.ShouldBe(Range("2026-09-01", "2026-09-01"));
        period.Previous.ShouldBe(Range("2026-08-01", "2026-08-01"));
    }

    [Fact]
    public void Last_month_is_the_full_previous_calendar_month_compared_with_the_one_before()
    {
        var period = Now().Resolve(new PeriodQuery { Preset = PeriodPreset.LastMonth });

        period.Current.ShouldBe(Range("2026-08-01", "2026-08-31"));
        period.Previous.ShouldBe(Range("2026-07-01", "2026-07-31"));
    }

    [Fact]
    public void Last_month_crosses_the_year_boundary()
    {
        var period = At(2026, 1, 10).Resolve(new PeriodQuery { Preset = PeriodPreset.LastMonth });

        period.Current.ShouldBe(Range("2025-12-01", "2025-12-31"));
        period.Previous.ShouldBe(Range("2025-11-01", "2025-11-30"));
    }

    [Fact]
    public void Custom_range_is_compared_with_the_equally_long_window_right_before_it()
    {
        var period = Now().Resolve(new PeriodQuery
        {
            Preset = PeriodPreset.Custom,
            From = new DateOnly(2026, 1, 10),
            To = new DateOnly(2026, 1, 20),
        });

        period.Current.ShouldBe(Range("2026-01-10", "2026-01-20"));
        period.Previous.ShouldBe(Range("2025-12-30", "2026-01-09"));
        period.Previous.Days.ShouldBe(period.Current.Days);
    }

    [Fact]
    public void Custom_single_day_range_is_valid()
    {
        var day = new DateOnly(2026, 5, 5);
        var period = Now().Resolve(new PeriodQuery { Preset = PeriodPreset.Custom, From = day, To = day });

        period.Current.Days.ShouldBe(1);
        period.Previous.ShouldBe(new DateRange(day.AddDays(-1), day.AddDays(-1)));
    }

    [Fact]
    public void Custom_without_both_dates_is_rejected() =>
        Should.Throw<ArgumentException>(() =>
            Now().Resolve(new PeriodQuery { Preset = PeriodPreset.Custom, From = new DateOnly(2026, 1, 1) }));

    [Fact]
    public void The_current_period_never_overlaps_the_previous_one()
    {
        foreach (var preset in Enum.GetValues<PeriodPreset>().Where(p => p != PeriodPreset.Custom))
        {
            var period = Now().Resolve(new PeriodQuery { Preset = preset });
            period.Previous.To.ShouldBeLessThan(period.Current.From, preset.ToString());
        }
    }
}

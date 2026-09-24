using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Periods;
using SalesDashboard.Application.Persistence.Models;
using SalesDashboard.Application.Trends;
using SalesDashboard.Application.Validation;
using SalesDashboard.Domain.Common;
using SalesDashboard.Domain.Metrics;

namespace SalesDashboard.UnitTests.Application;

public class TrendBucketingTests
{
    [Theory]
    [InlineData(1, BucketSize.Day)]
    [InlineData(45, BucketSize.Day)]
    [InlineData(46, BucketSize.Week)]
    [InlineData(200, BucketSize.Week)]
    [InlineData(201, BucketSize.Month)]
    [InlineData(365, BucketSize.Month)]
    public void Picks_a_bucket_size_that_keeps_the_chart_readable(int days, BucketSize expected) =>
        TrendBucketing.Choose(days).ShouldBe(expected);

    [Fact]
    public void Weeks_start_on_monday()
    {
        // Thursday 24 Sep 2026 -> Monday 21 Sep 2026; a Sunday belongs to the week that started six days earlier.
        TrendBucketing.BucketStart(new DateOnly(2026, 9, 24), BucketSize.Week).ShouldBe(new DateOnly(2026, 9, 21));
        TrendBucketing.BucketStart(new DateOnly(2026, 9, 27), BucketSize.Week).ShouldBe(new DateOnly(2026, 9, 21));
        TrendBucketing.BucketStart(new DateOnly(2026, 9, 21), BucketSize.Week).ShouldBe(new DateOnly(2026, 9, 21));
    }

    [Fact]
    public void Fills_days_without_sales_with_zeros()
    {
        var range = new DateRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 4));
        TrendBucket[] buckets = [new(new DateOnly(2026, 9, 2), new SalesMetrics(500m, 300m, 2))];

        var points = TrendBucketing.FillGaps(range, BucketSize.Day, buckets);

        points.Select(p => p.BucketStart.Day).ShouldBe([1, 2, 3, 4]);
        points.Select(p => p.SalesCount).ShouldBe([0, 2, 0, 0]);
        points[1].Revenue.ShouldBe(500m);
        points[1].GrossProfit.ShouldBe(200m);
        points[0].Revenue.ShouldBe(0m);
    }

    [Fact]
    public void Weekly_buckets_start_at_the_monday_on_or_before_the_range_start()
    {
        var range = new DateRange(new DateOnly(2026, 9, 16), new DateOnly(2026, 9, 29)); // Wed .. Tue

        var points = TrendBucketing.FillGaps(range, BucketSize.Week, []);

        points.Select(p => p.BucketStart).ShouldBe(
        [
            new DateOnly(2026, 9, 14),
            new DateOnly(2026, 9, 21),
            new DateOnly(2026, 9, 28),
        ]);
    }

    [Fact]
    public void Monthly_buckets_cover_partial_first_and_last_months()
    {
        var range = new DateRange(new DateOnly(2026, 1, 15), new DateOnly(2026, 3, 10));

        var points = TrendBucketing.FillGaps(range, BucketSize.Month, []);

        points.Select(p => p.BucketStart).ShouldBe(
        [
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 3, 1),
        ]);
    }

    [Fact]
    public void A_one_day_range_gives_exactly_one_point()
    {
        var day = new DateOnly(2026, 9, 24);

        TrendBucketing.FillGaps(new DateRange(day, day), BucketSize.Day, []).Count.ShouldBe(1);
    }
}

public class QueryValidatorTests
{
    private static readonly DateOnly Jan1 = new(2026, 1, 1);

    [Fact]
    public void Defaults_are_valid() =>
        new PeriodQueryValidator().Validate(new PeriodQuery()).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData(PeriodPreset.Today)]
    [InlineData(PeriodPreset.Last7Days)]
    [InlineData(PeriodPreset.Last30Days)]
    [InlineData(PeriodPreset.ThisMonth)]
    [InlineData(PeriodPreset.LastMonth)]
    public void Presets_without_dates_are_valid(PeriodPreset preset) =>
        new PeriodQueryValidator().Validate(new PeriodQuery { Preset = preset }).IsValid.ShouldBeTrue();

    [Fact]
    public void Undefined_preset_is_rejected() =>
        new PeriodQueryValidator().Validate(new PeriodQuery { Preset = (PeriodPreset)99 }).IsValid.ShouldBeFalse();

    [Fact]
    public void Custom_needs_both_dates()
    {
        var validator = new PeriodQueryValidator();

        validator.Validate(new PeriodQuery { Preset = PeriodPreset.Custom }).Errors.Count.ShouldBe(2);
        validator.Validate(new PeriodQuery { Preset = PeriodPreset.Custom, From = Jan1 }).IsValid.ShouldBeFalse();
        validator.Validate(new PeriodQuery { Preset = PeriodPreset.Custom, To = Jan1 }).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Custom_accepts_a_single_day_and_a_normal_range()
    {
        var validator = new PeriodQueryValidator();

        validator.Validate(new PeriodQuery { Preset = PeriodPreset.Custom, From = Jan1, To = Jan1 }).IsValid.ShouldBeTrue();
        validator.Validate(new PeriodQuery { Preset = PeriodPreset.Custom, From = Jan1, To = Jan1.AddDays(90) }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Custom_rejects_an_inverted_range()
    {
        var result = new PeriodQueryValidator().Validate(
            new PeriodQuery { Preset = PeriodPreset.Custom, From = Jan1.AddDays(5), To = Jan1 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("earlier"));
    }

    [Fact]
    public void Custom_rejects_absurdly_long_ranges_and_dates_before_2000()
    {
        var validator = new PeriodQueryValidator();

        validator.Validate(new PeriodQuery
        {
            Preset = PeriodPreset.Custom,
            From = Jan1,
            To = Jan1.AddDays(PeriodQueryValidatorBase<PeriodQuery>.MaxCustomRangeDays),
        }).IsValid.ShouldBeFalse();

        validator.Validate(new PeriodQuery
        {
            Preset = PeriodPreset.Custom,
            From = new DateOnly(1999, 12, 31),
            To = Jan1,
        }).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Dates_are_not_allowed_with_a_preset()
    {
        var result = new PeriodQueryValidator().Validate(
            new PeriodQuery { Preset = PeriodPreset.Last7Days, From = Jan1, To = Jan1 });

        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(50, true)]
    [InlineData(51, false)]
    public void Top_products_limit_is_bounded(int limit, bool valid) =>
        new TopProductsQueryValidator().Validate(new TopProductsQuery { Limit = limit }).IsValid.ShouldBe(valid);

    [Theory]
    [InlineData(0, 20, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 100, true)]
    [InlineData(1, 101, false)]
    [InlineData(7, 20, true)]
    public void Recent_sales_paging_is_bounded(int page, int pageSize, bool valid) =>
        new RecentSalesQueryValidator().Validate(new RecentSalesQuery { Page = page, PageSize = pageSize }).IsValid.ShouldBe(valid);

    [Fact]
    public void Undefined_ranking_metric_is_rejected() =>
        new RankingQueryValidator().Validate(new RankingQuery { RankBy = (RankingMetric)42 }).IsValid.ShouldBeFalse();
}

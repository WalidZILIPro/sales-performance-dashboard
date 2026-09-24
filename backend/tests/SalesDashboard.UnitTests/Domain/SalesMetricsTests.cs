using SalesDashboard.Domain.Common;
using SalesDashboard.Domain.Entities;
using SalesDashboard.Domain.Enums;
using SalesDashboard.Domain.Metrics;
using SalesDashboard.Domain.Rules;

namespace SalesDashboard.UnitTests.Domain;

public class SalesMetricsTests
{
    [Fact]
    public void Derives_gross_profit_margin_and_average_check()
    {
        var metrics = new SalesMetrics(Revenue: 3000m, Cost: 2000m, SalesCount: 3);

        metrics.GrossProfit.ShouldBe(1000m);
        metrics.Margin.ShouldBe(1000m / 3000m);
        metrics.AverageCheck.ShouldBe(1000m);
    }

    [Fact]
    public void Ratios_are_null_not_zero_or_exception_when_undefined()
    {
        SalesMetrics.Empty.Margin.ShouldBeNull();
        SalesMetrics.Empty.AverageCheck.ShouldBeNull();
        SalesMetrics.Empty.HasSales.ShouldBeFalse();
    }

    [Fact]
    public void Margin_can_be_negative_for_loss_making_sales()
    {
        new SalesMetrics(100m, 130m, 1).Margin.ShouldBe(-0.3m);
    }

    [Fact]
    public void Adding_sums_the_building_blocks_so_team_margin_is_not_an_average_of_margins()
    {
        // A: 10% margin on 1000. B: 50% margin on 100. Team margin must be weighted by revenue.
        var team = new SalesMetrics(1000m, 900m, 1) + new SalesMetrics(100m, 50m, 1);

        team.Revenue.ShouldBe(1100m);
        team.GrossProfit.ShouldBe(150m);
        team.Margin.ShouldBe(150m / 1100m);
        team.SalesCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(SaleStatus.Paid, true)]
    [InlineData(SaleStatus.Cancelled, false)]
    [InlineData(SaleStatus.Refunded, false)]
    public void Only_paid_sales_count_towards_revenue(SaleStatus status, bool expected) =>
        SalesRules.CountsTowardRevenue(status).ShouldBe(expected);
}

public class ChangeTests
{
    [Theory]
    [InlineData(120, 100, 0.2)]
    [InlineData(80, 100, -0.2)]
    [InlineData(100, 100, 0)]
    public void Relative_change_is_a_fraction_of_the_previous_value(double current, double previous, double expected) =>
        Change.Relative((decimal)current, (decimal)previous).ShouldBe((decimal)expected);

    [Fact]
    public void Relative_change_is_undefined_when_previous_is_zero_or_missing()
    {
        Change.Relative(50m, 0m).ShouldBeNull();
        Change.Relative(50m, null).ShouldBeNull();
        Change.Relative(null, 50m).ShouldBeNull();
    }

    [Fact]
    public void Relative_change_from_a_negative_base_keeps_the_sign_meaningful()
    {
        // Profit went from -100 to -50: an improvement, so the change must be positive.
        Change.Relative(-50m, -100m).ShouldBe(0.5m);
    }
}

public class DateRangeTests
{
    private static readonly DateRange Range = new(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

    [Fact]
    public void Counts_days_inclusively() => Range.Days.ShouldBe(30);

    [Fact]
    public void Includes_the_first_instant_of_the_first_day() =>
        Range.Contains(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero)).ShouldBeTrue();

    [Fact]
    public void Includes_the_last_instant_of_the_last_day() =>
        Range.Contains(new DateTimeOffset(2026, 9, 30, 23, 59, 59, 999, TimeSpan.Zero)).ShouldBeTrue();

    [Fact]
    public void Excludes_the_instant_just_before_it_starts() =>
        Range.Contains(new DateTimeOffset(2026, 8, 31, 23, 59, 59, 999, TimeSpan.Zero)).ShouldBeFalse();

    [Fact]
    public void Excludes_exactly_midnight_of_the_day_after_it_ends() =>
        Range.Contains(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero)).ShouldBeFalse();

    [Fact]
    public void Rejects_an_inverted_range() =>
        Should.Throw<ArgumentException>(() => new DateRange(new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 1)));
}

public class SaleTests
{
    private static readonly Category Cat = new("Drones");
    private static readonly Product Drone = new("SKU-1", "Drone", Cat, 100m, 60m);
    private static readonly Product Case = new("SKU-2", "Case", Cat, 10m, 4m);
    private static readonly Manager Mgr = new("Иван", "Петров", "SMB", "Sales Manager", "#000000");
    private static readonly Customer Cust = new("Анна Смирнова", "ООО «Тест»", CustomerSegment.Smb);

    [Fact]
    public void Totals_are_computed_from_the_items()
    {
        var sale = Sale.Create(
            Mgr,
            Cust,
            DateTimeOffset.UtcNow,
            SaleStatus.Paid,
            [new SaleLine(Drone, 2, 95m, 60m), new SaleLine(Case, 3, 10m, 4m)]);

        sale.TotalAmount.ShouldBe((2 * 95m) + (3 * 10m));
        sale.TotalCost.ShouldBe((2 * 60m) + (3 * 4m));
        sale.GrossProfit.ShouldBe(sale.TotalAmount - sale.TotalCost);
        sale.Items.Count.ShouldBe(2);
    }

    [Fact]
    public void Normalises_the_sale_time_to_utc()
    {
        var local = new DateTimeOffset(2026, 9, 24, 10, 0, 0, TimeSpan.FromHours(3));

        var sale = Sale.Create(Mgr, Cust, local, SaleStatus.Paid, [new SaleLine(Drone, 1, 100m, 60m)]);

        sale.SoldAt.Offset.ShouldBe(TimeSpan.Zero);
        sale.SoldAt.ShouldBe(local);
    }

    [Fact]
    public void A_sale_needs_at_least_one_item() =>
        Should.Throw<ArgumentException>(() => Sale.Create(Mgr, Cust, DateTimeOffset.UtcNow, SaleStatus.Paid, []));

    [Theory]
    [InlineData(0, 100, 60)]
    [InlineData(-1, 100, 60)]
    [InlineData(1, -1, 60)]
    [InlineData(1, 100, -1)]
    public void Rejects_invalid_lines(int quantity, int price, int cost) =>
        Should.Throw<ArgumentOutOfRangeException>(() => Sale.Create(
            Mgr,
            Cust,
            DateTimeOffset.UtcNow,
            SaleStatus.Paid,
            [new SaleLine(Drone, quantity, price, cost)]));

    [Fact]
    public void Rejects_an_unknown_status() =>
        Should.Throw<ArgumentOutOfRangeException>(() => Sale.Create(
            Mgr,
            Cust,
            DateTimeOffset.UtcNow,
            (SaleStatus)99,
            [new SaleLine(Drone, 1, 100m, 60m)]));

    [Fact]
    public void Manager_initials_and_full_name_are_derived()
    {
        Mgr.FullName.ShouldBe("Иван Петров");
        Mgr.Initials.ShouldBe("ИП");
    }
}

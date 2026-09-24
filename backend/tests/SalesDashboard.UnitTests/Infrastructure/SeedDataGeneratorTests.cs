using SalesDashboard.Domain.Enums;
using SalesDashboard.Infrastructure.Seeding;
using SalesDashboard.UnitTests.TestSupport;

namespace SalesDashboard.UnitTests.Infrastructure;

public class SeedDataGeneratorTests
{
    private static readonly SeedDataSet Data = new SeedDataGenerator().Generate(Build.Now);

    [Fact]
    public void Volumes_match_the_brief()
    {
        Data.Managers.Count.ShouldBeInRange(15, 25);
        Data.Customers.Count.ShouldBeInRange(50, 100);
        Data.Categories.Count.ShouldBeGreaterThanOrEqualTo(5);
        Data.Products.Count.ShouldBeGreaterThanOrEqualTo(24);
        Data.Sales.Count.ShouldBeInRange(2000, 5000);
    }

    [Fact]
    public void Is_reproducible_for_the_same_seed_and_time()
    {
        var again = new SeedDataGenerator().Generate(Build.Now);

        again.Sales.Count.ShouldBe(Data.Sales.Count);
        again.Sales
            .Select(s => (s.SoldAt, s.Status, s.TotalAmount, s.TotalCost, s.Manager.FullName, s.Customer.Company, s.Items.Count))
            .ShouldBe(Data.Sales.Select(s => (s.SoldAt, s.Status, s.TotalAmount, s.TotalCost, s.Manager.FullName, s.Customer.Company, s.Items.Count)));
    }

    [Fact]
    public void A_different_seed_gives_different_data()
    {
        var other = new SeedDataGenerator(seed: 7).Generate(Build.Now);

        other.Sales.Sum(s => s.TotalAmount).ShouldNotBe(Data.Sales.Sum(s => s.TotalAmount));
    }

    [Fact]
    public void Covers_twelve_months_and_never_sells_in_the_future()
    {
        Data.Sales.Min(s => s.SoldAt).ShouldBeGreaterThanOrEqualTo(Build.Now.AddDays(-365));
        Data.Sales.Min(s => s.SoldAt).ShouldBeLessThan(Build.Now.AddDays(-330));
        Data.Sales.Max(s => s.SoldAt).ShouldBeLessThanOrEqualTo(Build.Now);
        Data.Sales.ShouldAllBe(s => s.SoldAt.Offset == TimeSpan.Zero);
    }

    [Fact]
    public void Sales_are_in_chronological_order()
    {
        Data.Sales.Select(s => s.SoldAt).ShouldBe(Data.Sales.Select(s => s.SoldAt).Order());
    }

    [Fact]
    public void Includes_paid_cancelled_and_refunded_sales_in_realistic_proportions()
    {
        var byStatus = Data.Sales.GroupBy(s => s.Status).ToDictionary(g => g.Key, g => g.Count());

        byStatus.Keys.ShouldBe([SaleStatus.Paid, SaleStatus.Cancelled, SaleStatus.Refunded], ignoreOrder: true);
        (byStatus[SaleStatus.Paid] / (double)Data.Sales.Count).ShouldBeInRange(0.80, 0.95);
        byStatus[SaleStatus.Cancelled].ShouldBeGreaterThan(50);
        byStatus[SaleStatus.Refunded].ShouldBeGreaterThan(30);
    }

    [Fact]
    public void Managers_perform_very_differently()
    {
        var perManager = Data.Sales
            .Where(s => s.Status == SaleStatus.Paid)
            .GroupBy(s => s.Manager)
            .ToDictionary(g => g.Key, g => (Count: g.Count(), Revenue: g.Sum(s => s.TotalAmount), Avg: g.Average(s => s.TotalAmount)));

        perManager.Values.Max(v => v.Count).ShouldBeGreaterThan(perManager.Values.Min(v => v.Count) * 2);
        perManager.Values.Max(v => v.Avg).ShouldBeGreaterThan(perManager.Values.Min(v => v.Avg) * 2);
    }

    [Fact]
    public void Margins_differ_between_managers()
    {
        var margins = Data.Sales
            .Where(s => s.Status == SaleStatus.Paid)
            .GroupBy(s => s.Manager)
            .Select(g => g.Sum(s => s.GrossProfit) / g.Sum(s => s.TotalAmount))
            .ToList();

        (margins.Max() - margins.Min()).ShouldBeGreaterThan(0.03m);
    }

    [Fact]
    public void Has_seasonality_with_a_stronger_end_of_year_than_summer()
    {
        var countByMonth = Data.Sales.GroupBy(s => s.SoldAt.Month).ToDictionary(g => g.Key, g => g.Count());

        // Whichever months the trailing year covers, November/December must beat July/August.
        (countByMonth[11] + countByMonth[12]).ShouldBeGreaterThan(countByMonth[7] + countByMonth[8]);
    }

    [Fact]
    public void Some_managers_have_periods_without_any_sales()
    {
        // Maria Ivanova is on leave for 21 days starting 120 days ago (see SeedCatalog).
        var today = DateOnly.FromDateTime(Build.Now.UtcDateTime);
        var from = today.AddDays(-120);
        var to = today.AddDays(-100);

        var duringLeave = Data.Sales.Count(s =>
            s.Manager.LastName == "Иванова"
            && DateOnly.FromDateTime(s.SoldAt.UtcDateTime) >= from
            && DateOnly.FromDateTime(s.SoldAt.UtcDateTime) <= to);

        duringLeave.ShouldBe(0);
        Data.Sales.Count(s => s.Manager.LastName == "Иванова").ShouldBeGreaterThan(100);
    }

    [Fact]
    public void Has_a_recent_hire_and_a_leaver()
    {
        var newHire = Data.Sales.Where(s => s.Manager.LastName == "Богданов").ToList();
        newHire.Min(s => s.SoldAt).ShouldBeGreaterThan(Build.Now.AddDays(-101));

        Data.Managers.Single(m => m.LastName == "Воробьёва").IsActive.ShouldBeFalse();
        Data.Sales.Where(s => s.Manager.LastName == "Воробьёва").Max(s => s.SoldAt)
            .ShouldBeLessThan(Build.Now.AddDays(-149));
    }

    [Fact]
    public void Has_both_very_large_and_small_deals()
    {
        Data.Sales.Max(s => s.TotalAmount).ShouldBeGreaterThan(1_000_000m);
        Data.Sales.Count(s => s.TotalAmount < 20_000m).ShouldBeGreaterThan(500);
    }

    [Fact]
    public void Every_sale_is_internally_consistent()
    {
        foreach (var sale in Data.Sales)
        {
            sale.Items.Count.ShouldBeGreaterThan(0);
            sale.TotalAmount.ShouldBe(sale.Items.Sum(i => i.Quantity * i.UnitPrice));
            sale.TotalCost.ShouldBe(sale.Items.Sum(i => i.Quantity * i.UnitCost));
            sale.Items.Select(i => i.Product).Distinct().Count().ShouldBe(sale.Items.Count);
        }
    }

    [Fact]
    public void Skus_are_unique() =>
        Data.Products.Select(p => p.Sku).Distinct().Count().ShouldBe(Data.Products.Count);

    [Fact]
    public void Early_in_the_day_there_are_no_sales_after_now()
    {
        var earlyMorning = new DateTimeOffset(2026, 9, 24, 6, 0, 0, TimeSpan.Zero);

        var data = new SeedDataGenerator().Generate(earlyMorning);

        data.Sales.Max(s => s.SoldAt).ShouldBeLessThanOrEqualTo(earlyMorning);
    }
}

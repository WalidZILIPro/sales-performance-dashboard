using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Periods;
using SalesDashboard.Application.Persistence.Models;
using SalesDashboard.Application.Services;
using SalesDashboard.Domain.Enums;
using SalesDashboard.Domain.Metrics;
using SalesDashboard.UnitTests.TestSupport;
using static SalesDashboard.UnitTests.TestSupport.Build;

namespace SalesDashboard.UnitTests.Application;

public class DashboardSummaryServiceTests
{
    private readonly FakeAnalyticsRepository _repository = new();
    private readonly DashboardSummaryService _service;

    public DashboardSummaryServiceTests() =>
        _service = new DashboardSummaryService(new PeriodResolver(new FixedTimeProvider(Now)), _repository);

    private Task<SalesDashboard.Application.Contracts.Responses.DashboardSummaryDto> Summary() =>
        _service.GetSummaryAsync(new PeriodQuery { Preset = PeriodPreset.Last30Days }, CancellationToken.None);

    [Fact]
    public async Task Team_kpis_are_the_sum_of_the_managers()
    {
        _repository.Performance.Add(Perf(Manager(1, "Ann"), Metrics(2000, 1500, 2), Metrics(1000, 800, 1)));
        _repository.Performance.Add(Perf(Manager(2, "Ben"), Metrics(1000, 500, 1), Metrics(500, 200, 1)));

        var kpis = (await Summary()).Kpis;

        kpis.Revenue.Current.ShouldBe(3000m);
        kpis.Revenue.Previous.ShouldBe(1500m);
        kpis.GrossProfit.Current.ShouldBe(1000m);   // (2000-1500) + (1000-500)
        kpis.GrossProfit.Previous.ShouldBe(500m);
        kpis.SalesCount.Current.ShouldBe(3m);
        kpis.AverageCheck.Current.ShouldBe(1000m);  // 3000 / 3
        kpis.AverageCheck.Previous.ShouldBe(750m);  // 1500 / 2
    }

    [Fact]
    public async Task Margin_is_total_profit_over_total_revenue_not_an_average_of_manager_margins()
    {
        _repository.Performance.Add(Perf(Manager(1, "Ann"), Metrics(1000, 900, 1))); // 10%
        _repository.Performance.Add(Perf(Manager(2, "Ben"), Metrics(100, 50, 1)));   // 50%

        var margin = (await Summary()).Kpis.Margin;

        margin.Current.ShouldBe(0.1364m); // 150 / 1100, not (10% + 50%) / 2 = 30%
    }

    [Fact]
    public async Task Change_is_a_fraction_and_undefined_against_a_zero_previous_period()
    {
        _repository.Performance.Add(Perf(Manager(1, "Ann"), Metrics(3000, 2000, 3), Metrics(1500, 1000, 1)));
        var withHistory = await Summary();

        withHistory.Kpis.Revenue.ChangePct.ShouldBe(1.0m);       // +100%
        withHistory.Kpis.Revenue.Change.ShouldBe(1500m);
        withHistory.Kpis.AverageCheck.ChangePct.ShouldBe(-0.3333m);

        _repository.Performance.Clear();
        _repository.Performance.Add(Perf(Manager(1, "Ann"), Metrics(3000, 2000, 3)));
        var withoutHistory = await Summary();

        withoutHistory.Kpis.Revenue.Previous.ShouldBe(0m);
        withoutHistory.Kpis.Revenue.ChangePct.ShouldBeNull();
        withoutHistory.Kpis.AverageCheck.Previous.ShouldBeNull();
        withoutHistory.Kpis.AverageCheck.ChangePct.ShouldBeNull();
    }

    [Fact]
    public async Task Best_manager_is_the_one_with_the_highest_gross_profit()
    {
        _repository.Performance.Add(Perf(Manager(1, "Big Revenue"), Metrics(9000, 8500, 10)));   // GP 500
        _repository.Performance.Add(Perf(Manager(2, "Best Profit"), Metrics(2000, 1000, 2), Metrics(1000, 800, 1)));

        var best = (await Summary()).BestManager!;

        best.Manager.Name.ShouldBe("Best Profit");
        best.GrossProfit.ShouldBe(1000m);
        best.GrossProfitChangePct.ShouldBe(4.0m); // 200 -> 1000
    }

    [Fact]
    public async Task Best_manager_tie_is_broken_by_revenue_then_name()
    {
        _repository.Performance.Add(Perf(Manager(1, "Zed"), Metrics(1000, 500, 1)));
        _repository.Performance.Add(Perf(Manager(2, "Amy"), Metrics(1000, 500, 1)));

        (await Summary()).BestManager!.Manager.Name.ShouldBe("Amy");
    }

    [Fact]
    public async Task Empty_period_gives_zeros_and_no_best_manager_instead_of_failing()
    {
        _repository.Performance.Add(Perf(Manager(1, "Idle"), SalesMetrics.Empty));

        var summary = await Summary();

        summary.BestManager.ShouldBeNull();
        summary.Kpis.Revenue.Current.ShouldBe(0m);
        summary.Kpis.SalesCount.Current.ShouldBe(0m);
        summary.Kpis.Margin.Current.ShouldBeNull();
        summary.Kpis.AverageCheck.Current.ShouldBeNull();
        summary.Statuses.CancellationRate.ShouldBeNull();
        summary.Statuses.RefundRate.ShouldBeNull();
    }

    [Fact]
    public async Task No_managers_at_all_is_also_an_empty_state()
    {
        var summary = await Summary();

        summary.BestManager.ShouldBeNull();
        summary.Kpis.Revenue.Current.ShouldBe(0m);
    }

    [Fact]
    public async Task Status_breakdown_reports_cancelled_and_refunded_separately_from_paid()
    {
        _repository.Statuses.Add(new StatusStatistic(SaleStatus.Paid, 80, 800_000m));
        _repository.Statuses.Add(new StatusStatistic(SaleStatus.Cancelled, 12, 90_000m));
        _repository.Statuses.Add(new StatusStatistic(SaleStatus.Refunded, 8, 60_000m));

        var statuses = (await Summary()).Statuses;

        statuses.PaidCount.ShouldBe(80);
        statuses.CancelledCount.ShouldBe(12);
        statuses.RefundedCount.ShouldBe(8);
        statuses.CancelledAmount.ShouldBe(90_000m);
        statuses.RefundedAmount.ShouldBe(60_000m);
        statuses.CancellationRate.ShouldBe(0.12m); // 12 of 100 sales
        statuses.RefundRate.ShouldBe(0.08m);
    }

    [Fact]
    public async Task Response_echoes_the_resolved_period()
    {
        var period = (await Summary()).Period;

        period.Preset.ShouldBe(PeriodPreset.Last30Days);
        period.From.ShouldBe(new DateOnly(2026, 8, 26));
        period.To.ShouldBe(new DateOnly(2026, 9, 24));
        period.PreviousFrom.ShouldBe(new DateOnly(2026, 7, 27));
        period.PreviousTo.ShouldBe(new DateOnly(2026, 8, 25));
        period.Days.ShouldBe(30);
    }

    [Fact]
    public async Task One_very_large_sale_does_not_break_the_numbers()
    {
        _repository.Performance.Add(Perf(Manager(1, "Whale"), Metrics(12_000_000, 10_800_000, 1)));
        _repository.Performance.Add(Perf(Manager(2, "Many small"), Metrics(3_000, 2_000, 300)));

        var summary = await Summary();

        summary.Kpis.Revenue.Current.ShouldBe(12_003_000m);
        summary.Kpis.SalesCount.Current.ShouldBe(301m);
        summary.BestManager!.Manager.Name.ShouldBe("Whale");
    }
}

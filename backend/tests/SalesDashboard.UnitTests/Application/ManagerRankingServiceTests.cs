using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Periods;
using SalesDashboard.Application.Ranking;
using SalesDashboard.Application.Services;
using SalesDashboard.UnitTests.TestSupport;
using static SalesDashboard.UnitTests.TestSupport.Build;

namespace SalesDashboard.UnitTests.Application;

public class ManagerRankingServiceTests
{
    private readonly FakeAnalyticsRepository _repository = new();
    private readonly ManagerRankingService _service;

    public ManagerRankingServiceTests()
    {
        IRankingStrategy[] strategies =
        [
            new GrossProfitRankingStrategy(),
            new AverageCheckRankingStrategy(),
            new RevenueRankingStrategy(),
            new MarginRankingStrategy(),
        ];

        _service = new ManagerRankingService(new PeriodResolver(new FixedTimeProvider(Now)), _repository, strategies);
    }

    private Task<SalesDashboard.Application.Contracts.Responses.ManagerRankingDto> Rank(RankingMetric metric) =>
        _service.GetRankingAsync(new RankingQuery { Preset = PeriodPreset.Last30Days, RankBy = metric }, CancellationToken.None);

    [Fact]
    public async Task Ranks_by_gross_profit_descending()
    {
        _repository.Performance.Add(Perf(Manager(1, "Anna Low"), Metrics(1000, 900, 5)));   // GP 100
        _repository.Performance.Add(Perf(Manager(2, "Boris High"), Metrics(1000, 500, 5))); // GP 500
        _repository.Performance.Add(Perf(Manager(3, "Clara Mid"), Metrics(1000, 700, 5)));  // GP 300

        var result = await Rank(RankingMetric.GrossProfit);

        result.Managers.Select(m => m.Manager.Name).ShouldBe(["Boris High", "Clara Mid", "Anna Low"]);
        result.Managers.Select(m => m.Rank).ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task Average_check_ranks_differently_from_gross_profit()
    {
        // Big is #1 by gross profit (many small sales); Premium is #1 by average check (few big sales).
        _repository.Performance.Add(Perf(Manager(1, "Big Volume"), Metrics(10000, 8000, 100)));  // GP 2000, avg 100
        _repository.Performance.Add(Perf(Manager(2, "Premium"), Metrics(3000, 2500, 2)));        // GP 500, avg 1500

        var byProfit = await Rank(RankingMetric.GrossProfit);
        var byCheck = await Rank(RankingMetric.AverageCheck);

        byProfit.Managers[0].Manager.Name.ShouldBe("Big Volume");
        byCheck.Managers[0].Manager.Name.ShouldBe("Premium");
        byCheck.Managers[0].MetricValue.ShouldBe(1500m);
    }

    [Fact]
    public async Task Tied_managers_share_a_rank_and_the_next_rank_is_skipped()
    {
        _repository.Performance.Add(Perf(Manager(1, "Zed"), Metrics(1000, 500, 2)));
        _repository.Performance.Add(Perf(Manager(2, "Amy"), Metrics(1000, 500, 4))); // same GP as Zed
        _repository.Performance.Add(Perf(Manager(3, "Kim"), Metrics(600, 500, 1)));

        var result = await Rank(RankingMetric.GrossProfit);

        result.Managers.Select(m => m.Rank).ShouldBe([1, 1, 3]);
    }

    [Fact]
    public async Task Ties_are_ordered_deterministically_by_revenue_then_name()
    {
        _repository.Performance.Add(Perf(Manager(1, "Zed"), Metrics(1000, 500, 2)));
        _repository.Performance.Add(Perf(Manager(2, "Amy"), Metrics(1000, 500, 2)));
        _repository.Performance.Add(Perf(Manager(3, "Bob"), Metrics(1200, 700, 2))); // same GP, more revenue

        var result = await Rank(RankingMetric.GrossProfit);

        result.Managers.Select(m => m.Manager.Name).ShouldBe(["Bob", "Amy", "Zed"]);
    }

    [Fact]
    public async Task Managers_without_sales_are_listed_last_and_are_not_ranked()
    {
        _repository.Performance.Add(Perf(Manager(1, "Idle"), SalesDashboard.Domain.Metrics.SalesMetrics.Empty));
        _repository.Performance.Add(Perf(Manager(2, "Active"), Metrics(1000, 900, 1)));

        var gross = await Rank(RankingMetric.GrossProfit);
        var check = await Rank(RankingMetric.AverageCheck);

        foreach (var result in new[] { gross, check })
        {
            result.Managers[0].Manager.Name.ShouldBe("Active");
            result.Managers[0].Rank.ShouldBe(1);

            var idle = result.Managers[1];
            idle.Manager.Name.ShouldBe("Idle");
            idle.Rank.ShouldBeNull();
            idle.HasSales.ShouldBeFalse();
            idle.MetricValue.ShouldBeNull();
            idle.AverageCheck.ShouldBeNull();
            idle.Margin.ShouldBeNull();
        }
    }

    [Fact]
    public async Task Everyone_without_sales_yields_an_unranked_list_not_an_error()
    {
        _repository.Performance.Add(Perf(Manager(1, "A"), SalesDashboard.Domain.Metrics.SalesMetrics.Empty));
        _repository.Performance.Add(Perf(Manager(2, "B"), SalesDashboard.Domain.Metrics.SalesMetrics.Empty));

        var result = await Rank(RankingMetric.GrossProfit);

        result.Managers.Count.ShouldBe(2);
        result.Managers.ShouldAllBe(m => m.Rank == null);
    }

    [Fact]
    public async Task Reports_rank_movement_against_the_previous_period()
    {
        // Previous GP: Ann 900 (#1), Ben 100 (#2). Current GP: Ann 100, Ben 900 -> they swapped places.
        _repository.Performance.Add(Perf(Manager(1, "Ann"), Metrics(1000, 900, 1), Metrics(1000, 100, 1)));
        _repository.Performance.Add(Perf(Manager(2, "Ben"), Metrics(1000, 100, 1), Metrics(1000, 900, 1)));

        var result = await Rank(RankingMetric.GrossProfit);

        var ben = result.Managers[0];
        ben.Manager.Name.ShouldBe("Ben");
        (ben.Rank, ben.PreviousRank, ben.RankChange).ShouldBe((1, 2, 1));   // moved up one place

        var ann = result.Managers[1];
        (ann.Rank, ann.PreviousRank, ann.RankChange).ShouldBe((2, 1, -1)); // moved down one place
    }

    [Fact]
    public async Task A_manager_new_in_the_period_has_no_previous_rank_or_change()
    {
        _repository.Performance.Add(Perf(Manager(1, "Newcomer"), Metrics(1000, 500, 2), SalesDashboard.Domain.Metrics.SalesMetrics.Empty));

        var row = (await Rank(RankingMetric.GrossProfit)).Managers.Single();

        row.PreviousRank.ShouldBeNull();
        row.RankChange.ShouldBeNull();
        row.GrossProfitChangePct.ShouldBeNull(); // undefined against zero, not "infinity"
    }

    [Fact]
    public async Task Calculates_row_metrics_and_change_against_the_previous_period()
    {
        _repository.Performance.Add(Perf(
            Manager(1, "Ann"),
            Metrics(revenue: 3000, cost: 2000, count: 3),
            Metrics(revenue: 1500, cost: 1000, count: 1)));

        var row = (await Rank(RankingMetric.GrossProfit)).Managers.Single();

        row.SalesCount.ShouldBe(3);
        row.Revenue.ShouldBe(3000m);
        row.GrossProfit.ShouldBe(1000m);
        row.AverageCheck.ShouldBe(1000m);
        row.Margin.ShouldBe(0.3333m);
        row.RevenueChangePct.ShouldBe(1.0m);      // 1500 -> 3000
        row.GrossProfitChangePct.ShouldBe(1.0m);  // 500 -> 1000
        row.SalesCountChangePct.ShouldBe(2.0m);   // 1 -> 3
        row.AverageCheckChangePct.ShouldBe(-0.3333m); // 1500 -> 1000
        row.MetricChangePct.ShouldBe(1.0m);
    }

    [Fact]
    public async Task An_unknown_ranking_mode_is_a_programming_error_not_a_silent_default()
    {
        var service = new ManagerRankingService(
            new PeriodResolver(new FixedTimeProvider(Now)),
            _repository,
            [new GrossProfitRankingStrategy()]);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            service.GetRankingAsync(new RankingQuery { RankBy = RankingMetric.Margin }, CancellationToken.None));
    }

    [Fact]
    public async Task Asks_the_repository_for_the_resolved_current_and_previous_periods()
    {
        await Rank(RankingMetric.GrossProfit);

        _repository.LastCurrent!.Value.From.ShouldBe(new DateOnly(2026, 8, 26));
        _repository.LastCurrent!.Value.To.ShouldBe(new DateOnly(2026, 9, 24));
        _repository.LastPrevious!.Value.From.ShouldBe(new DateOnly(2026, 7, 27));
        _repository.LastPrevious!.Value.To.ShouldBe(new DateOnly(2026, 8, 25));
    }
}

public class RankAssignerTests
{
    [Fact]
    public void Uses_competition_ranking()
    {
        var ranks = RankAssigner.Assign(
            new (int Id, decimal? Value)[] { (1, 50m), (2, 100m), (3, 100m), (4, 10m), (5, null) },
            x => x.Id,
            x => x.Value);

        ranks[2].ShouldBe(1);
        ranks[3].ShouldBe(1);
        ranks[1].ShouldBe(3);
        ranks[4].ShouldBe(4);
        ranks.ContainsKey(5).ShouldBeFalse();
    }
}

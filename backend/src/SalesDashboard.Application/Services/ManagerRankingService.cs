using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Contracts.Responses;
using SalesDashboard.Application.Mapping;
using SalesDashboard.Application.Periods;
using SalesDashboard.Application.Persistence;
using SalesDashboard.Application.Persistence.Models;
using SalesDashboard.Application.Ranking;
using SalesDashboard.Domain.Metrics;

namespace SalesDashboard.Application.Services;

public sealed class ManagerRankingService : IManagerRankingService
{
    private readonly IPeriodResolver _periods;
    private readonly ISalesAnalyticsRepository _repository;
    private readonly Dictionary<RankingMetric, IRankingStrategy> _strategies;

    public ManagerRankingService(
        IPeriodResolver periods,
        ISalesAnalyticsRepository repository,
        IEnumerable<IRankingStrategy> strategies)
    {
        _periods = periods;
        _repository = repository;
        _strategies = strategies.ToDictionary(s => s.Metric);
    }

    public async Task<ManagerRankingDto> GetRankingAsync(RankingQuery query, CancellationToken cancellationToken)
    {
        if (!_strategies.TryGetValue(query.RankBy, out var strategy))
        {
            throw new InvalidOperationException($"No ranking strategy is registered for '{query.RankBy}'.");
        }

        var period = _periods.Resolve(query);
        var performance = await _repository.GetManagerPerformanceAsync(period.Current, period.Previous, cancellationToken);

        // A manager with no sales in the period has no meaningful value, so is left unranked.
        decimal? Value(SalesMetrics metrics) => metrics.HasSales ? strategy.GetValue(metrics) : null;

        var currentRanks = RankAssigner.Assign(performance, r => r.Manager.Id, r => Value(r.Current));
        var previousRanks = RankAssigner.Assign(performance, r => r.Manager.Id, r => Value(r.Previous));

        // Ranked managers by rank, then a deterministic tie-break so equal managers never flicker
        // between requests; unranked managers last, alphabetically.
        var rows = performance
            .OrderBy(r => currentRanks.ContainsKey(r.Manager.Id) ? 0 : 1)
            .ThenByDescending(r => Value(r.Current))
            .ThenByDescending(r => r.Current.GrossProfit)
            .ThenByDescending(r => r.Current.Revenue)
            .ThenBy(r => r.Manager.FullName, StringComparer.Ordinal)
            .Select(r => ToRow(r, currentRanks, previousRanks, Value))
            .ToList();

        return new ManagerRankingDto(period.ToDto(), query.RankBy, rows);
    }

    private static ManagerRankingRowDto ToRow(
        ManagerPerformance row,
        Dictionary<int, int> currentRanks,
        Dictionary<int, int> previousRanks,
        Func<SalesMetrics, decimal?> value)
    {
        int? rank = currentRanks.TryGetValue(row.Manager.Id, out var r) ? r : null;
        int? previousRank = previousRanks.TryGetValue(row.Manager.Id, out var p) ? p : null;
        var current = row.Current;
        var previous = row.Previous;

        return new ManagerRankingRowDto(
            rank,
            previousRank,
            previousRank - rank,
            row.Manager.ToRef(),
            current.HasSales,
            RoundedMetric(value(current)),
            Rounding.Ratio(Change.Relative(value(current), value(previous))),
            current.SalesCount,
            Rounding.Money(current.Revenue),
            Rounding.Money(current.GrossProfit),
            Rounding.Money(current.AverageCheck),
            Rounding.Ratio(current.Margin),
            Rounding.Ratio(Change.Relative(current.SalesCount, previous.SalesCount)),
            Rounding.Ratio(Change.Relative(current.Revenue, previous.Revenue)),
            Rounding.Ratio(Change.Relative(current.GrossProfit, previous.GrossProfit)),
            Rounding.Ratio(Change.Relative(current.AverageCheck, previous.AverageCheck)));
    }

    // Margin is a ratio (4 decimals), the other metrics are money (2 decimals); 4 decimals is a safe
    // superset for the ranked value, and the client formats it per mode.
    private static decimal? RoundedMetric(decimal? value) => value is null ? null : Math.Round(value.Value, 4);
}

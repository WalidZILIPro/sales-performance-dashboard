using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Persistence;
using SalesDashboard.Application.Persistence.Models;
using SalesDashboard.Domain.Common;
using SalesDashboard.Domain.Metrics;

namespace SalesDashboard.UnitTests.TestSupport;

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal sealed class FakeAnalyticsRepository : ISalesAnalyticsRepository
{
    public List<ManagerPerformance> Performance { get; } = [];

    public List<StatusStatistic> Statuses { get; } = [];

    public List<TrendBucket> Trend { get; } = [];

    public DateRange? LastCurrent { get; private set; }

    public DateRange? LastPrevious { get; private set; }

    public Task<IReadOnlyList<ManagerPerformance>> GetManagerPerformanceAsync(
        DateRange current,
        DateRange previous,
        CancellationToken cancellationToken)
    {
        LastCurrent = current;
        LastPrevious = previous;
        return Task.FromResult<IReadOnlyList<ManagerPerformance>>(Performance);
    }

    public Task<IReadOnlyList<StatusStatistic>> GetStatusStatisticsAsync(DateRange range, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StatusStatistic>>(Statuses);

    public Task<IReadOnlyList<TrendBucket>> GetTrendAsync(DateRange range, BucketSize bucketSize, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<TrendBucket>>(Trend);
}

internal static class Build
{
    /// <summary>Thursday 24 September 2026, noon UTC.</summary>
    public static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    public static ManagerInfo Manager(int id, string name, bool active = true) =>
        new(id, name, name[..Math.Min(2, name.Length)].ToUpperInvariant(), "#4F46E5", "Team", "Sales Manager", active);

    public static SalesMetrics Metrics(decimal revenue, decimal cost, int count) => new(revenue, cost, count);

    public static ManagerPerformance Perf(ManagerInfo manager, SalesMetrics current, SalesMetrics? previous = null) =>
        new(manager, current, previous ?? SalesMetrics.Empty);
}

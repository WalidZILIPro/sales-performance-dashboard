using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Domain.Metrics;

namespace SalesDashboard.Application.Ranking;

/// <summary>
/// Strategy pattern: one implementation per ranking mode. The ranking service never switches on the
/// mode, so adding one (say, ranking by sales count) is one new class plus one DI registration.
/// Higher value ranks higher. Managers without sales are never passed in: they are unranked.
/// </summary>
public interface IRankingStrategy
{
    RankingMetric Metric { get; }

    decimal? GetValue(SalesMetrics metrics);
}

public sealed class GrossProfitRankingStrategy : IRankingStrategy
{
    public RankingMetric Metric => RankingMetric.GrossProfit;

    public decimal? GetValue(SalesMetrics metrics) => metrics.GrossProfit;
}

public sealed class AverageCheckRankingStrategy : IRankingStrategy
{
    public RankingMetric Metric => RankingMetric.AverageCheck;

    public decimal? GetValue(SalesMetrics metrics) => metrics.AverageCheck;
}

public sealed class RevenueRankingStrategy : IRankingStrategy
{
    public RankingMetric Metric => RankingMetric.Revenue;

    public decimal? GetValue(SalesMetrics metrics) => metrics.Revenue;
}

public sealed class MarginRankingStrategy : IRankingStrategy
{
    public RankingMetric Metric => RankingMetric.Margin;

    public decimal? GetValue(SalesMetrics metrics) => metrics.Margin;
}

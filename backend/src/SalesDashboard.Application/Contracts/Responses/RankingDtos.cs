using SalesDashboard.Application.Contracts.Requests;

namespace SalesDashboard.Application.Contracts.Responses;

public sealed record ManagerRankingDto(
    PeriodDto Period,
    RankingMetric RankBy,
    IReadOnlyList<ManagerRankingRowDto> Managers);

/// <param name="Rank">
/// 1-based position; tied managers share a rank (1, 1, 3). <c>null</c> for managers with no sales in
/// the period: they are listed last and are not ranked.
/// </param>
/// <param name="PreviousRank">Rank the manager would have had in the previous period, same metric.</param>
/// <param name="RankChange">PreviousRank - Rank: positive means moved up. <c>null</c> when either rank is missing.</param>
/// <param name="MetricValue">The value the ranking is based on.</param>
public sealed record ManagerRankingRowDto(
    int? Rank,
    int? PreviousRank,
    int? RankChange,
    ManagerRefDto Manager,
    bool HasSales,
    decimal? MetricValue,
    decimal? MetricChangePct,
    int SalesCount,
    decimal Revenue,
    decimal GrossProfit,
    decimal? AverageCheck,
    decimal? Margin,
    decimal? SalesCountChangePct,
    decimal? RevenueChangePct,
    decimal? GrossProfitChangePct,
    decimal? AverageCheckChangePct);

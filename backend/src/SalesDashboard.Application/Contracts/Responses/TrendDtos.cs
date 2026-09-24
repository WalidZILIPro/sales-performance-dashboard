using SalesDashboard.Application.Contracts.Requests;

namespace SalesDashboard.Application.Contracts.Responses;

/// <summary>Points are gap-filled: a bucket with no sales is present with zeros, never omitted.</summary>
public sealed record SalesTrendDto(PeriodDto Period, BucketSize Granularity, IReadOnlyList<TrendPointDto> Points);

/// <param name="BucketStart">First day of the bucket (Monday for weeks, the 1st for months).</param>
public sealed record TrendPointDto(DateOnly BucketStart, decimal Revenue, decimal GrossProfit, int SalesCount);

using SalesDashboard.Application.Periods;
using SalesDashboard.Domain.Enums;

namespace SalesDashboard.Application.Contracts.Requests;

/// <summary>Query-string parameters shared by every dashboard endpoint.</summary>
public class PeriodQuery
{
    public PeriodPreset Preset { get; set; } = PeriodPreset.Last30Days;

    /// <summary>First day (inclusive). Only with <see cref="PeriodPreset.Custom"/>.</summary>
    public DateOnly? From { get; set; }

    /// <summary>Last day (inclusive). Only with <see cref="PeriodPreset.Custom"/>.</summary>
    public DateOnly? To { get; set; }
}

public enum RankingMetric
{
    GrossProfit = 1,
    AverageCheck = 2,
    Revenue = 3,
    Margin = 4,
}

public class RankingQuery : PeriodQuery
{
    public RankingMetric RankBy { get; set; } = RankingMetric.GrossProfit;
}

public enum BucketSize
{
    Day = 1,
    Week = 2,
    Month = 3,
}

public class TrendQuery : PeriodQuery
{
    /// <summary>Omit to let the server pick a bucket size that suits the length of the period.</summary>
    public BucketSize? Granularity { get; set; }
}

public enum ProductSortBy
{
    GrossProfit = 1,
    Revenue = 2,
    Units = 3,
}

public class TopProductsQuery : PeriodQuery
{
    public int Limit { get; set; } = 10;

    public ProductSortBy SortBy { get; set; } = ProductSortBy.GrossProfit;
}

public class RecentSalesQuery : PeriodQuery
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    /// <summary>Optional filter; omit to list sales of every status.</summary>
    public SaleStatus? Status { get; set; }
}

using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Persistence.Models;
using SalesDashboard.Domain.Common;
using SalesDashboard.Domain.Enums;

namespace SalesDashboard.Application.Persistence;

// The application layer owns these abstractions (dependency inversion): services depend on them,
// Infrastructure implements them. They are query-specific on purpose. A generic IRepository<T>
// over the DbContext would leak IQueryable into the services and push aggregation into memory.

/// <summary>Aggregates over sales. Only Paid sales feed money metrics (see SalesRules).</summary>
public interface ISalesAnalyticsRepository
{
    /// <summary>
    /// Metrics per manager for both periods in one round trip. Includes every active manager, and
    /// inactive ones that sold something in either period, with zeros where they have no sales.
    /// </summary>
    Task<IReadOnlyList<ManagerPerformance>> GetManagerPerformanceAsync(
        DateRange current,
        DateRange previous,
        CancellationToken cancellationToken);

    /// <summary>Count and amount per status (all statuses) within the range.</summary>
    Task<IReadOnlyList<StatusStatistic>> GetStatusStatisticsAsync(DateRange range, CancellationToken cancellationToken);

    /// <summary>Paid metrics per time bucket. Buckets with no sales are absent (the service fills them).</summary>
    Task<IReadOnlyList<TrendBucket>> GetTrendAsync(
        DateRange range,
        BucketSize bucketSize,
        CancellationToken cancellationToken);
}

/// <summary>Aggregates over sale items, grouped by product and category (Paid sales only).</summary>
public interface ICatalogAnalyticsRepository
{
    Task<IReadOnlyList<CategoryPerformance>> GetCategoryPerformanceAsync(
        DateRange range,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductPerformance>> GetTopProductsAsync(
        DateRange range,
        ProductSortBy sortBy,
        int limit,
        CancellationToken cancellationToken);
}

public interface ISaleReadRepository
{
    /// <summary>Newest first (ties broken by id), optionally filtered by status; sales of every status by default.</summary>
    Task<PagedRows<RecentSaleRow>> GetPageAsync(
        DateRange range,
        SaleStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

using SalesDashboard.Domain.Enums;
using SalesDashboard.Domain.Metrics;

namespace SalesDashboard.Application.Persistence.Models;

/// <summary>
/// Shapes returned by the repositories. They are read models, not entities: the database already
/// aggregated them, and the services only combine, compare and round them.
/// </summary>
public sealed record ManagerInfo(
    int Id,
    string FullName,
    string Initials,
    string AvatarColor,
    string Team,
    string Title,
    bool IsActive);

public sealed record ManagerPerformance(ManagerInfo Manager, SalesMetrics Current, SalesMetrics Previous);

public sealed record StatusStatistic(SaleStatus Status, int Count, decimal Amount);

public sealed record TrendBucket(DateOnly BucketStart, SalesMetrics Metrics);

public sealed record CategoryPerformance(int CategoryId, string Name, decimal Revenue, decimal Cost, int Units)
{
    public decimal GrossProfit => Revenue - Cost;

    public decimal? Margin => Revenue == 0m ? null : GrossProfit / Revenue;
}

public sealed record ProductPerformance(
    int ProductId,
    string Sku,
    string Name,
    string CategoryName,
    decimal Revenue,
    decimal Cost,
    int Units)
{
    public decimal GrossProfit => Revenue - Cost;

    public decimal? Margin => Revenue == 0m ? null : GrossProfit / Revenue;
}

public sealed class RecentSaleRow
{
    public required int Id { get; init; }

    public required DateTimeOffset SoldAt { get; init; }

    public required SaleStatus Status { get; init; }

    public required int ManagerId { get; init; }

    public required string ManagerFirstName { get; init; }

    public required string ManagerLastName { get; init; }

    public required string ManagerAvatarColor { get; init; }

    public required string ManagerTeam { get; init; }

    public required string ManagerTitle { get; init; }

    public required int CustomerId { get; init; }

    public required string CustomerName { get; init; }

    public required string CustomerCompany { get; init; }

    public required decimal TotalAmount { get; init; }

    public required decimal TotalCost { get; init; }

    public required IReadOnlyList<RecentSaleItemRow> Items { get; init; }
}

public sealed record RecentSaleItemRow(string ProductName, int Quantity);

public sealed record PagedRows<T>(IReadOnlyList<T> Items, int TotalCount);

using SalesDashboard.Application.Contracts.Requests;

namespace SalesDashboard.Application.Contracts.Responses;

public sealed record CategoryBreakdownDto(
    PeriodDto Period,
    decimal TotalRevenue,
    IReadOnlyList<CategoryPerformanceDto> Categories);

/// <param name="RevenueShare">Fraction of total revenue in the period (0.25 = 25%).</param>
public sealed record CategoryPerformanceDto(
    int CategoryId,
    string Name,
    decimal Revenue,
    decimal GrossProfit,
    decimal? Margin,
    int Units,
    decimal? RevenueShare);

public sealed record TopProductsDto(
    PeriodDto Period,
    ProductSortBy SortBy,
    IReadOnlyList<ProductPerformanceDto> Products);

public sealed record ProductPerformanceDto(
    int ProductId,
    string Sku,
    string Name,
    string Category,
    decimal Revenue,
    decimal GrossProfit,
    decimal? Margin,
    int Units);

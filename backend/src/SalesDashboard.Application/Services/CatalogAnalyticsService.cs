using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Contracts.Responses;
using SalesDashboard.Application.Mapping;
using SalesDashboard.Application.Periods;
using SalesDashboard.Application.Persistence;

namespace SalesDashboard.Application.Services;

public sealed class CatalogAnalyticsService(IPeriodResolver periods, ICatalogAnalyticsRepository repository)
    : ICatalogAnalyticsService
{
    public async Task<CategoryBreakdownDto> GetCategoryBreakdownAsync(
        PeriodQuery query,
        CancellationToken cancellationToken)
    {
        var period = periods.Resolve(query);
        var categories = await repository.GetCategoryPerformanceAsync(period.Current, cancellationToken);

        var totalRevenue = categories.Sum(c => c.Revenue);

        var rows = categories
            .OrderByDescending(c => c.Revenue)
            .ThenBy(c => c.Name, StringComparer.Ordinal)
            .Select(c => new CategoryPerformanceDto(
                c.CategoryId,
                c.Name,
                Rounding.Money(c.Revenue),
                Rounding.Money(c.GrossProfit),
                Rounding.Ratio(c.Margin),
                c.Units,
                totalRevenue == 0m ? null : Rounding.Ratio(c.Revenue / totalRevenue)))
            .ToList();

        return new CategoryBreakdownDto(period.ToDto(), Rounding.Money(totalRevenue), rows);
    }

    public async Task<TopProductsDto> GetTopProductsAsync(TopProductsQuery query, CancellationToken cancellationToken)
    {
        var period = periods.Resolve(query);
        var products = await repository.GetTopProductsAsync(period.Current, query.SortBy, query.Limit, cancellationToken);

        var rows = products
            .Select(p => new ProductPerformanceDto(
                p.ProductId,
                p.Sku,
                p.Name,
                p.CategoryName,
                Rounding.Money(p.Revenue),
                Rounding.Money(p.GrossProfit),
                Rounding.Ratio(p.Margin),
                p.Units))
            .ToList();

        return new TopProductsDto(period.ToDto(), query.SortBy, rows);
    }
}

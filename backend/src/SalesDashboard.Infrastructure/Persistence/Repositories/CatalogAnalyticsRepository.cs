using Microsoft.EntityFrameworkCore;
using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Persistence;
using SalesDashboard.Application.Persistence.Models;
using SalesDashboard.Domain.Common;
using SalesDashboard.Domain.Entities;
using SalesDashboard.Domain.Enums;
using SalesDashboard.Domain.Rules;

namespace SalesDashboard.Infrastructure.Persistence.Repositories;

internal sealed class CatalogAnalyticsRepository(AppDbContext db) : ICatalogAnalyticsRepository
{
    public async Task<IReadOnlyList<CategoryPerformance>> GetCategoryPerformanceAsync(
        DateRange range,
        CancellationToken cancellationToken)
    {
        var rows = await PaidItems(range)
            .GroupBy(i => new { i.Product.CategoryId, CategoryName = i.Product.Category.Name })
            .Select(g => new
            {
                g.Key.CategoryId,
                g.Key.CategoryName,
                Revenue = g.Sum(i => i.Quantity * i.UnitPrice),
                Cost = g.Sum(i => i.Quantity * i.UnitCost),
                Units = g.Sum(i => i.Quantity),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new CategoryPerformance(r.CategoryId, r.CategoryName, r.Revenue, r.Cost, r.Units))
            .ToList();
    }

    public async Task<IReadOnlyList<ProductPerformance>> GetTopProductsAsync(
        DateRange range,
        ProductSortBy sortBy,
        int limit,
        CancellationToken cancellationToken)
    {
        var grouped = PaidItems(range)
            .GroupBy(i => new
            {
                i.ProductId,
                i.Product.Sku,
                i.Product.Name,
                CategoryName = i.Product.Category.Name,
            })
            .Select(g => new ProductAggregate
            {
                ProductId = g.Key.ProductId,
                Sku = g.Key.Sku,
                Name = g.Key.Name,
                CategoryName = g.Key.CategoryName,
                Revenue = g.Sum(i => i.Quantity * i.UnitPrice),
                Cost = g.Sum(i => i.Quantity * i.UnitCost),
                Units = g.Sum(i => i.Quantity),
            });

        // Ordering and LIMIT happen in the database; ProductId is the final tie-break so the
        // result is deterministic.
        var ordered = sortBy switch
        {
            ProductSortBy.Revenue => grouped.OrderByDescending(p => p.Revenue),
            ProductSortBy.Units => grouped.OrderByDescending(p => p.Units),
            ProductSortBy.GrossProfit => grouped.OrderByDescending(p => p.Revenue - p.Cost),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null),
        };

        var rows = await ordered
            .ThenBy(p => p.ProductId)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(p => new ProductPerformance(p.ProductId, p.Sku, p.Name, p.CategoryName, p.Revenue, p.Cost, p.Units))
            .ToList();
    }

    private IQueryable<SaleItem> PaidItems(DateRange range)
    {
        var start = range.StartUtc;
        var end = range.EndExclusiveUtc;
        const SaleStatus paid = SalesRules.RevenueStatus;

        return db.Sales
            .Where(s => s.Status == paid && s.SoldAt >= start && s.SoldAt < end)
            .SelectMany(s => s.Items);
    }

    private sealed class ProductAggregate
    {
        public int ProductId { get; init; }

        public string Sku { get; init; } = null!;

        public string Name { get; init; } = null!;

        public string CategoryName { get; init; } = null!;

        public decimal Revenue { get; init; }

        public decimal Cost { get; init; }

        public int Units { get; init; }
    }
}

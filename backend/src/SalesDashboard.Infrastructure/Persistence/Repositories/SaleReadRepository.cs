using Microsoft.EntityFrameworkCore;
using SalesDashboard.Application.Persistence;
using SalesDashboard.Application.Persistence.Models;
using SalesDashboard.Domain.Common;
using SalesDashboard.Domain.Enums;

namespace SalesDashboard.Infrastructure.Persistence.Repositories;

internal sealed class SaleReadRepository(AppDbContext db) : ISaleReadRepository
{
    public async Task<PagedRows<RecentSaleRow>> GetPageAsync(
        DateRange range,
        SaleStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var start = range.StartUtc;
        var end = range.EndExclusiveUtc;

        var query = db.Sales.Where(s => s.SoldAt >= start && s.SoldAt < end);
        if (status is { } wanted)
        {
            query = query.Where(s => s.Status == wanted);
        }

        var total = await query.CountAsync(cancellationToken);

        // Projection, not Include(): only the columns the table shows are read, and the item names
        // come back in the same statement (no N+1 query per sale).
        var rows = await query
            .OrderByDescending(s => s.SoldAt)
            .ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.SoldAt,
                s.Status,
                s.ManagerId,
                ManagerFirstName = s.Manager.FirstName,
                ManagerLastName = s.Manager.LastName,
                ManagerAvatarColor = s.Manager.AvatarColor,
                ManagerTeam = s.Manager.Team,
                ManagerTitle = s.Manager.Title,
                s.CustomerId,
                CustomerName = s.Customer.Name,
                CustomerCompany = s.Customer.Company,
                s.TotalAmount,
                s.TotalCost,
                Items = s.Items
                    .OrderBy(i => i.Id)
                    .Select(i => new { ProductName = i.Product.Name, i.Quantity })
                    .ToList(),
            })
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new RecentSaleRow
            {
                Id = r.Id,
                SoldAt = r.SoldAt,
                Status = r.Status,
                ManagerId = r.ManagerId,
                ManagerFirstName = r.ManagerFirstName,
                ManagerLastName = r.ManagerLastName,
                ManagerAvatarColor = r.ManagerAvatarColor,
                ManagerTeam = r.ManagerTeam,
                ManagerTitle = r.ManagerTitle,
                CustomerId = r.CustomerId,
                CustomerName = r.CustomerName,
                CustomerCompany = r.CustomerCompany,
                TotalAmount = r.TotalAmount,
                TotalCost = r.TotalCost,
                Items = r.Items.Select(i => new RecentSaleItemRow(i.ProductName, i.Quantity)).ToList(),
            })
            .ToList();

        return new PagedRows<RecentSaleRow>(items, total);
    }
}

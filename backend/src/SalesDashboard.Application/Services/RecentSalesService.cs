using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Contracts.Responses;
using SalesDashboard.Application.Mapping;
using SalesDashboard.Application.Periods;
using SalesDashboard.Application.Persistence;
using SalesDashboard.Application.Persistence.Models;
using SalesDashboard.Domain.Entities;

namespace SalesDashboard.Application.Services;

public sealed class RecentSalesService(IPeriodResolver periods, ISaleReadRepository repository) : IRecentSalesService
{
    public async Task<RecentSalesDto> GetRecentSalesAsync(RecentSalesQuery query, CancellationToken cancellationToken)
    {
        var period = periods.Resolve(query);
        var page = await repository.GetPageAsync(period.Current, query.Status, query.Page, query.PageSize, cancellationToken);

        var totalPages = (int)Math.Ceiling(page.TotalCount / (double)query.PageSize);
        var items = page.Items.Select(ToDto).ToList();

        return new RecentSalesDto(
            period.ToDto(),
            new PagedResultDto<RecentSaleDto>(items, query.Page, query.PageSize, page.TotalCount, totalPages));
    }

    private static RecentSaleDto ToDto(RecentSaleRow row)
    {
        var manager = new ManagerRefDto(
            row.ManagerId,
            $"{row.ManagerFirstName} {row.ManagerLastName}",
            Manager.BuildInitials(row.ManagerFirstName, row.ManagerLastName),
            row.ManagerAvatarColor,
            row.ManagerTeam,
            row.ManagerTitle);

        return new RecentSaleDto(
            row.Id,
            $"S-{row.Id:D6}",
            row.SoldAt,
            row.Status,
            manager,
            new CustomerRefDto(row.CustomerId, row.CustomerName, row.CustomerCompany),
            row.Items.Select(i => new SaleItemSummaryDto(i.ProductName, i.Quantity)).ToList(),
            Rounding.Money(row.TotalAmount),
            Rounding.Money(row.TotalAmount - row.TotalCost));
    }
}

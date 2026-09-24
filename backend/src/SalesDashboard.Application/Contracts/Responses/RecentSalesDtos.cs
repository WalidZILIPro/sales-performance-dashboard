using SalesDashboard.Domain.Enums;

namespace SalesDashboard.Application.Contracts.Responses;

public sealed record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record RecentSalesDto(PeriodDto Period, PagedResultDto<RecentSaleDto> Sales);

/// <param name="Amount">Sale total. For Cancelled/Refunded sales it is shown but not counted in the KPIs.</param>
public sealed record RecentSaleDto(
    int Id,
    string Number,
    DateTimeOffset SoldAt,
    SaleStatus Status,
    ManagerRefDto Manager,
    CustomerRefDto Customer,
    IReadOnlyList<SaleItemSummaryDto> Items,
    decimal Amount,
    decimal GrossProfit);

public sealed record CustomerRefDto(int Id, string Name, string Company);

public sealed record SaleItemSummaryDto(string ProductName, int Quantity);

namespace SalesDashboard.Application.Contracts.Responses;

public sealed record DashboardSummaryDto(
    PeriodDto Period,
    KpisDto Kpis,
    BestManagerDto? BestManager,
    SaleStatusBreakdownDto Statuses);

public sealed record KpisDto(
    MetricComparisonDto Revenue,
    MetricComparisonDto GrossProfit,
    MetricComparisonDto Margin,
    MetricComparisonDto SalesCount,
    MetricComparisonDto AverageCheck);

public sealed record BestManagerDto(
    ManagerRefDto Manager,
    decimal GrossProfit,
    decimal Revenue,
    int SalesCount,
    decimal? GrossProfitChangePct);

/// <summary>
/// Sales of every status in the period. Only Paid ones feed the KPIs; the rest is shown so a manager
/// can see how much was lost to cancellations and refunds. Rates are fractions of all sales.
/// </summary>
public sealed record SaleStatusBreakdownDto(
    int PaidCount,
    int CancelledCount,
    int RefundedCount,
    decimal CancelledAmount,
    decimal RefundedAmount,
    decimal? CancellationRate,
    decimal? RefundRate);

using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Contracts.Responses;
using SalesDashboard.Application.Mapping;
using SalesDashboard.Application.Periods;
using SalesDashboard.Application.Persistence;
using SalesDashboard.Domain.Enums;
using SalesDashboard.Domain.Metrics;

namespace SalesDashboard.Application.Services;

public sealed class DashboardSummaryService(IPeriodResolver periods, ISalesAnalyticsRepository repository)
    : IDashboardSummaryService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(PeriodQuery query, CancellationToken cancellationToken)
    {
        var period = periods.Resolve(query);

        // Two cheap queries, run one after the other (a DbContext is not thread-safe):
        // per-manager metrics for both periods, and per-status counts for the current one.
        var performance = await repository.GetManagerPerformanceAsync(period.Current, period.Previous, cancellationToken);
        var statuses = await repository.GetStatusStatisticsAsync(period.Current, cancellationToken);

        // Team totals are the sum of the (additive) manager metrics, so the KPI cards and the
        // ranking can never disagree with each other.
        var current = performance.Aggregate(SalesMetrics.Empty, (sum, row) => sum + row.Current);
        var previous = performance.Aggregate(SalesMetrics.Empty, (sum, row) => sum + row.Previous);

        var kpis = new KpisDto(
            Rounding.Compare(current.Revenue, previous.Revenue),
            Rounding.Compare(current.GrossProfit, previous.GrossProfit),
            Rounding.Compare(current.Margin, previous.Margin, isRatio: true),
            Rounding.Compare(current.SalesCount, previous.SalesCount),
            Rounding.Compare(current.AverageCheck, previous.AverageCheck));

        return new DashboardSummaryDto(period.ToDto(), kpis, FindBestManager(performance), BuildStatuses(statuses));
    }

    private static BestManagerDto? FindBestManager(IReadOnlyList<Persistence.Models.ManagerPerformance> performance)
    {
        // Best = highest Gross Profit; ties broken by revenue, then name, so the result is stable.
        var best = performance
            .Where(row => row.Current.HasSales)
            .OrderByDescending(row => row.Current.GrossProfit)
            .ThenByDescending(row => row.Current.Revenue)
            .ThenBy(row => row.Manager.FullName, StringComparer.Ordinal)
            .FirstOrDefault();

        return best is null
            ? null
            : new BestManagerDto(
                best.Manager.ToRef(),
                Rounding.Money(best.Current.GrossProfit),
                Rounding.Money(best.Current.Revenue),
                best.Current.SalesCount,
                Rounding.Ratio(Change.Relative(best.Current.GrossProfit, best.Previous.GrossProfit)));
    }

    private static SaleStatusBreakdownDto BuildStatuses(IReadOnlyList<Persistence.Models.StatusStatistic> statuses)
    {
        var paid = statuses.FirstOrDefault(s => s.Status == SaleStatus.Paid);
        var cancelled = statuses.FirstOrDefault(s => s.Status == SaleStatus.Cancelled);
        var refunded = statuses.FirstOrDefault(s => s.Status == SaleStatus.Refunded);

        var paidCount = paid?.Count ?? 0;
        var cancelledCount = cancelled?.Count ?? 0;
        var refundedCount = refunded?.Count ?? 0;
        var total = paidCount + cancelledCount + refundedCount;

        return new SaleStatusBreakdownDto(
            paidCount,
            cancelledCount,
            refundedCount,
            Rounding.Money(cancelled?.Amount ?? 0m),
            Rounding.Money(refunded?.Amount ?? 0m),
            total == 0 ? null : Rounding.Ratio((decimal)cancelledCount / total),
            total == 0 ? null : Rounding.Ratio((decimal)refundedCount / total));
    }
}

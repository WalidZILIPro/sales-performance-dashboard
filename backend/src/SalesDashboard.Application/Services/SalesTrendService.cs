using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Contracts.Responses;
using SalesDashboard.Application.Mapping;
using SalesDashboard.Application.Periods;
using SalesDashboard.Application.Persistence;
using SalesDashboard.Application.Trends;

namespace SalesDashboard.Application.Services;

public sealed class SalesTrendService(IPeriodResolver periods, ISalesAnalyticsRepository repository) : ISalesTrendService
{
    public async Task<SalesTrendDto> GetTrendAsync(TrendQuery query, CancellationToken cancellationToken)
    {
        var period = periods.Resolve(query);
        var size = query.Granularity ?? TrendBucketing.Choose(period.Current.Days);

        var buckets = await repository.GetTrendAsync(period.Current, size, cancellationToken);

        return new SalesTrendDto(period.ToDto(), size, TrendBucketing.FillGaps(period.Current, size, buckets));
    }
}

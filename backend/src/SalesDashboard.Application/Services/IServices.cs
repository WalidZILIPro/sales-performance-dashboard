using SalesDashboard.Application.Contracts.Requests;
using SalesDashboard.Application.Contracts.Responses;

namespace SalesDashboard.Application.Services;

public interface IDashboardSummaryService
{
    Task<DashboardSummaryDto> GetSummaryAsync(PeriodQuery query, CancellationToken cancellationToken);
}

public interface IManagerRankingService
{
    Task<ManagerRankingDto> GetRankingAsync(RankingQuery query, CancellationToken cancellationToken);
}

public interface ISalesTrendService
{
    Task<SalesTrendDto> GetTrendAsync(TrendQuery query, CancellationToken cancellationToken);
}

public interface ICatalogAnalyticsService
{
    Task<CategoryBreakdownDto> GetCategoryBreakdownAsync(PeriodQuery query, CancellationToken cancellationToken);

    Task<TopProductsDto> GetTopProductsAsync(TopProductsQuery query, CancellationToken cancellationToken);
}

public interface IRecentSalesService
{
    Task<RecentSalesDto> GetRecentSalesAsync(RecentSalesQuery query, CancellationToken cancellationToken);
}

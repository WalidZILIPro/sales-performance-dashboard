using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SalesDashboard.Application.Periods;
using SalesDashboard.Application.Ranking;
using SalesDashboard.Application.Services;
using SalesDashboard.Application.Validation;

namespace SalesDashboard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // The clock is injected so "today" is testable and never read from DateTime.UtcNow directly.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IPeriodResolver, PeriodResolver>();

        // Strategy pattern: every ranking mode is registered here and picked at runtime by its Metric.
        services.AddSingleton<IRankingStrategy, GrossProfitRankingStrategy>();
        services.AddSingleton<IRankingStrategy, AverageCheckRankingStrategy>();
        services.AddSingleton<IRankingStrategy, RevenueRankingStrategy>();
        services.AddSingleton<IRankingStrategy, MarginRankingStrategy>();

        services.AddScoped<IDashboardSummaryService, DashboardSummaryService>();
        services.AddScoped<IManagerRankingService, ManagerRankingService>();
        services.AddScoped<ISalesTrendService, SalesTrendService>();
        services.AddScoped<ICatalogAnalyticsService, CatalogAnalyticsService>();
        services.AddScoped<IRecentSalesService, RecentSalesService>();

        services.AddValidatorsFromAssemblyContaining<PeriodQueryValidator>(ServiceLifetime.Singleton);

        return services;
    }
}

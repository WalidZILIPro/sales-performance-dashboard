using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SalesDashboard.Application.Persistence;
using SalesDashboard.Infrastructure.Persistence;
using SalesDashboard.Infrastructure.Persistence.Repositories;
using SalesDashboard.Infrastructure.Seeding;

namespace SalesDashboard.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Default";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure())
            .UseSnakeCaseNamingConvention()
            // The API only reads; nothing needs change tracking (the seeder adds new entities, which
            // works the same either way).
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<ISalesAnalyticsRepository, SalesAnalyticsRepository>();
        services.AddScoped<ICatalogAnalyticsRepository, CatalogAnalyticsRepository>();
        services.AddScoped<ISaleReadRepository, SaleReadRepository>();

        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.AddSingleton<SeedDataGenerator>();
        services.AddScoped<DatabaseSeeder>();
        services.AddHostedService<DatabaseInitializer>();

        return services;
    }
}

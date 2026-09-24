using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SalesDashboard.Infrastructure.Persistence;

namespace SalesDashboard.Infrastructure.Seeding;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool Migrate { get; set; } = true;

    public bool Seed { get; set; } = true;

    /// <summary>The database container may still be starting when the API does.</summary>
    public int MaxConnectionAttempts { get; set; } = 20;

    public int RetryDelaySeconds { get; set; } = 3;
}

/// <summary>
/// Applies migrations and seeds an empty database before the API starts accepting requests
/// (ASP.NET Core awaits hosted services first). This is what makes `docker compose up` produce a
/// ready dashboard with no manual steps.
/// </summary>
public sealed class DatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    IOptions<DatabaseOptions> options,
    ILogger<DatabaseInitializer> logger) : IHostedService
{
    // Arbitrary constant: instances racing at startup take this lock, so only one migrates and seeds.
    private const long StartupLockKey = 727_001;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.Migrate && !settings.Seed)
        {
            return;
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await InitializeAsync(settings, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < settings.MaxConnectionAttempts && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    ex,
                    "Database is not ready (attempt {Attempt}/{Max}); retrying in {Delay}s.",
                    attempt,
                    settings.MaxConnectionAttempts,
                    settings.RetryDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(settings.RetryDelaySeconds), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task InitializeAsync(DatabaseOptions settings, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Advisory locks belong to a connection, so keep one open for the whole initialisation.
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_lock({StartupLockKey})", cancellationToken);

            if (settings.Migrate)
            {
                logger.LogInformation("Applying database migrations...");
                await db.Database.MigrateAsync(cancellationToken);
            }

            if (settings.Seed)
            {
                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                var seeded = await seeder.SeedIfEmptyAsync(cancellationToken);
                if (seeded)
                {
                    logger.LogInformation("Database seeded.");
                }
                else
                {
                    logger.LogInformation("Database already contains data; seeding skipped.");
                }
            }
        }
        finally
        {
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({StartupLockKey})", CancellationToken.None);
            await db.Database.CloseConnectionAsync();
        }
    }
}

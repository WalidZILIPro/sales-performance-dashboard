using Microsoft.EntityFrameworkCore;
using SalesDashboard.Infrastructure.Persistence;

namespace SalesDashboard.Infrastructure.Seeding;

public sealed class DatabaseSeeder(AppDbContext db, TimeProvider clock, SeedDataGenerator generator)
{
    /// <summary>Seeds an empty database. Returns false (and does nothing) if data already exists.</summary>
    public async Task<bool> SeedIfEmptyAsync(CancellationToken cancellationToken)
    {
        if (await db.Managers.AnyAsync(cancellationToken))
        {
            return false;
        }

        var data = generator.Generate(clock.GetUtcNow());

        // Adding one connected graph and saving once means a single transaction: a failed seed
        // leaves the database empty, so the next start simply tries again.
        db.Categories.AddRange(data.Categories);
        db.Products.AddRange(data.Products);
        db.Managers.AddRange(data.Managers);
        db.Customers.AddRange(data.Customers);
        db.Sales.AddRange(data.Sales);

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

using Microsoft.EntityFrameworkCore;
using SalesDashboard.Domain.Entities;

namespace SalesDashboard.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Manager> Managers => Set<Manager>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}

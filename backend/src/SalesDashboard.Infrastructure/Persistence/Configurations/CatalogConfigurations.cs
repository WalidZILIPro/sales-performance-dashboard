using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesDashboard.Domain.Entities;

namespace SalesDashboard.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(c => c.Name).IsUnique().HasDatabaseName("ux_categories_name");
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", t =>
        {
            t.HasCheckConstraint("ck_products_list_price", "list_price >= 0");
            t.HasCheckConstraint("ck_products_unit_cost", "unit_cost >= 0");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Sku).HasMaxLength(32).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.ListPrice).HasPrecision(18, 2);
        builder.Property(p => p.UnitCost).HasPrecision(18, 2);

        builder.HasIndex(p => p.Sku).IsUnique().HasDatabaseName("ux_products_sku");
        builder.HasIndex(p => p.CategoryId).HasDatabaseName("ix_products_category_id");

        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

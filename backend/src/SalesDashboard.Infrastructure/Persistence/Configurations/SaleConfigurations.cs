using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesDashboard.Domain.Entities;

namespace SalesDashboard.Infrastructure.Persistence.Configurations;

internal sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales", t =>
        {
            t.HasCheckConstraint("ck_sales_status", "status IN ('Paid','Cancelled','Refunded')");
            t.HasCheckConstraint("ck_sales_totals", "total_amount >= 0 AND total_cost >= 0");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.SoldAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(s => s.TotalAmount).HasPrecision(18, 2);
        builder.Property(s => s.TotalCost).HasPrecision(18, 2);

        builder.HasOne(s => s.Manager)
            .WithMany()
            .HasForeignKey(s => s.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Customer)
            .WithMany()
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Every dashboard aggregate filters "status = Paid AND sold_at in range" and reads manager_id,
        // total_amount, total_cost. Equality column first, range column second, and the rest as
        // INCLUDE columns so those queries can be answered by an index-only scan (no table visits).
        builder.HasIndex(s => new { s.Status, s.SoldAt })
            .IncludeProperties(s => new { s.ManagerId, s.TotalAmount, s.TotalCost })
            .HasDatabaseName("ix_sales_status_sold_at");

        // "Recent sales" pages through all statuses newest-first: ORDER BY sold_at DESC, id DESC.
        builder.HasIndex(s => new { s.SoldAt, s.Id })
            .HasDatabaseName("ix_sales_sold_at_id");

        // Per-manager drill-down, and supports the manager_id foreign key.
        builder.HasIndex(s => new { s.ManagerId, s.SoldAt })
            .HasDatabaseName("ix_sales_manager_id_sold_at");
    }
}

internal sealed class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("sale_items", t =>
        {
            t.HasCheckConstraint("ck_sale_items_quantity", "quantity > 0");
            t.HasCheckConstraint("ck_sale_items_amounts", "unit_price >= 0 AND unit_cost >= 0");
        });

        builder.HasKey(i => i.Id);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.UnitCost).HasPrecision(18, 2);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Product / category analytics reach items through their sale. Covering the columns they read
        // lets that join stay index-only; it also serves as the sale_id foreign-key index.
        builder.HasIndex(i => i.SaleId)
            .IncludeProperties(i => new { i.ProductId, i.Quantity, i.UnitPrice, i.UnitCost })
            .HasDatabaseName("ix_sale_items_sale_id");

        builder.HasIndex(i => i.ProductId).HasDatabaseName("ix_sale_items_product_id");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesDashboard.Domain.Entities;

namespace SalesDashboard.Infrastructure.Persistence.Configurations;

internal sealed class ManagerConfiguration : IEntityTypeConfiguration<Manager>
{
    public void Configure(EntityTypeBuilder<Manager> builder)
    {
        builder.ToTable("managers");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.LastName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Team).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Title).HasMaxLength(100).IsRequired();
        builder.Property(m => m.AvatarColor).HasMaxLength(7).IsRequired();
        builder.Property(m => m.IsActive).IsRequired();
    }
}

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers", t =>
            t.HasCheckConstraint("ck_customers_segment", "segment IN ('Enterprise','MidMarket','Smb','Reseller')"));

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Company).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Segment).HasConversion<string>().HasMaxLength(16).IsRequired();
    }
}

namespace SalesDashboard.Domain.Entities;

public class Product
{
    private Product()
    {
    }

    public Product(string sku, string name, Category category, decimal listPrice, decimal unitCost)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(category);
        ArgumentOutOfRangeException.ThrowIfNegative(listPrice);
        ArgumentOutOfRangeException.ThrowIfNegative(unitCost);

        Sku = sku.Trim();
        Name = name.Trim();
        Category = category;
        CategoryId = category.Id;
        ListPrice = listPrice;
        UnitCost = unitCost;
    }

    public int Id { get; private set; }

    public string Sku { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public int CategoryId { get; private set; }

    public Category Category { get; private set; } = null!;

    /// <summary>Catalogue price; the price actually charged is stored on each <see cref="SaleItem"/>.</summary>
    public decimal ListPrice { get; private set; }

    /// <summary>Current catalogue cost; the cost actually booked is stored on each <see cref="SaleItem"/>.</summary>
    public decimal UnitCost { get; private set; }
}

namespace SalesDashboard.Domain.Entities;

/// <summary>A line of a <see cref="Sale"/>. Price and cost are snapshots taken at the time of sale.</summary>
public class SaleItem
{
    private SaleItem()
    {
    }

    internal SaleItem(Product product, int quantity, decimal unitPrice, decimal unitCost)
    {
        Product = product;
        ProductId = product.Id;
        Quantity = quantity;
        UnitPrice = unitPrice;
        UnitCost = unitCost;
    }

    public int Id { get; private set; }

    public int SaleId { get; private set; }

    public int ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal UnitCost { get; private set; }

    public decimal LineAmount => Quantity * UnitPrice;

    public decimal LineCost => Quantity * UnitCost;
}

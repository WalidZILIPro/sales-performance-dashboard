using SalesDashboard.Domain.Enums;

namespace SalesDashboard.Domain.Entities;

/// <summary>One line to be sold: which product, how many, at what price and cost.</summary>
public readonly record struct SaleLine(Product Product, int Quantity, decimal UnitPrice, decimal UnitCost);

/// <summary>
/// Aggregate root. Items are created together with the sale, and <see cref="TotalAmount"/> /
/// <see cref="TotalCost"/> are computed once from them and stored, so analytics can aggregate the
/// <c>sales</c> table alone without joining items. The factory is the only way to build a sale,
/// which keeps the totals consistent with the items.
/// </summary>
public class Sale
{
    private readonly List<SaleItem> _items = [];

    private Sale()
    {
    }

    public int Id { get; private set; }

    public int ManagerId { get; private set; }

    public Manager Manager { get; private set; } = null!;

    public int CustomerId { get; private set; }

    public Customer Customer { get; private set; } = null!;

    /// <summary>Always UTC.</summary>
    public DateTimeOffset SoldAt { get; private set; }

    public SaleStatus Status { get; private set; }

    public decimal TotalAmount { get; private set; }

    public decimal TotalCost { get; private set; }

    public decimal GrossProfit => TotalAmount - TotalCost;

    public IReadOnlyCollection<SaleItem> Items => _items;

    public static Sale Create(
        Manager manager,
        Customer customer,
        DateTimeOffset soldAt,
        SaleStatus status,
        IEnumerable<SaleLine> lines)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(lines);

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown sale status.");
        }

        var sale = new Sale
        {
            Manager = manager,
            ManagerId = manager.Id,
            Customer = customer,
            CustomerId = customer.Id,
            SoldAt = soldAt.ToUniversalTime(),
            Status = status,
        };

        foreach (var line in lines)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(line.Quantity, nameof(lines));
            ArgumentOutOfRangeException.ThrowIfNegative(line.UnitPrice, nameof(lines));
            ArgumentOutOfRangeException.ThrowIfNegative(line.UnitCost, nameof(lines));

            sale._items.Add(new SaleItem(line.Product, line.Quantity, line.UnitPrice, line.UnitCost));
        }

        if (sale._items.Count == 0)
        {
            throw new ArgumentException("A sale must contain at least one item.", nameof(lines));
        }

        sale.TotalAmount = sale._items.Sum(i => i.LineAmount);
        sale.TotalCost = sale._items.Sum(i => i.LineCost);
        return sale;
    }
}

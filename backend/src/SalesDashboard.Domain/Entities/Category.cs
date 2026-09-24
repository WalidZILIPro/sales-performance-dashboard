namespace SalesDashboard.Domain.Entities;

public class Category
{
    private Category()
    {
    }

    public Category(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = null!;
}

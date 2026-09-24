using SalesDashboard.Domain.Enums;

namespace SalesDashboard.Domain.Entities;

public class Customer
{
    private Customer()
    {
    }

    public Customer(string name, string company, CustomerSegment segment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(company);

        Name = name.Trim();
        Company = company.Trim();
        Segment = segment;
    }

    public int Id { get; private set; }

    /// <summary>Contact person.</summary>
    public string Name { get; private set; } = null!;

    public string Company { get; private set; } = null!;

    public CustomerSegment Segment { get; private set; }
}

namespace SalesDashboard.Domain.Metrics;

/// <summary>
/// The additive building blocks (revenue, cost, number of sales) of every dashboard number.
/// Everything else is derived, so metrics of different groups can be summed and re-derived
/// correctly (e.g. team margin = total gross profit / total revenue, not an average of margins).
/// Ratios are returned as fractions (0.25 = 25%) and are <c>null</c> when undefined (division by zero).
/// </summary>
public readonly record struct SalesMetrics(decimal Revenue, decimal Cost, int SalesCount)
{
    public static SalesMetrics Empty => default;

    public decimal GrossProfit => Revenue - Cost;

    /// <summary>Gross Profit / Revenue.</summary>
    public decimal? Margin => Revenue == 0m ? null : GrossProfit / Revenue;

    /// <summary>Revenue / number of sales.</summary>
    public decimal? AverageCheck => SalesCount == 0 ? null : Revenue / SalesCount;

    public bool HasSales => SalesCount > 0;

    public static SalesMetrics operator +(SalesMetrics left, SalesMetrics right) =>
        new(left.Revenue + right.Revenue, left.Cost + right.Cost, left.SalesCount + right.SalesCount);
}
